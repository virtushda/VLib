using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace VLib
{
    public struct CmdRenderProcInstanceTracker : IInstanceTrack
    {
        Mesh mesh;
        float4x4 transformMatrix;

        public Mesh Mesh { get => mesh; set => mesh = value; }
        public float4x4 Matrix { get => transformMatrix; set => transformMatrix = value; }

        public CmdRenderProcInstanceTracker(Mesh mesh, float4x4 transformMatrix)
        {
            this.mesh = mesh;
            this.transformMatrix = transformMatrix;
        }

        public bool Equals(float4x4 other) { return transformMatrix.Equals(other); }
    }

    public class CommandRenderMeshesWithMatProcedural<TTracker> : CommandRenderMeshesWithMatProcedural, IInstanceTrackingMulti<TTracker, CmdRenderProcInstanceTracker>
    {
        Dictionary<TTracker, List<CmdRenderProcInstanceTracker>> instanceMap;
        public Dictionary<TTracker, List<CmdRenderProcInstanceTracker>> InstanceMap { get => instanceMap; }

        SimpleListPool<CmdRenderProcInstanceTracker> instanceTrackerListPool;

        public CommandRenderMeshesWithMatProcedural() : base()
        {
            this.instanceMap = new Dictionary<TTracker, List<CmdRenderProcInstanceTracker>>(8);
            instanceTrackerListPool = new SimpleListPool<CmdRenderProcInstanceTracker>(16, 8);
        }

        public void AddInstanceTracked<TPropType>(TTracker tracker, Mesh mesh, TPropType propStruct)
            where TPropType : unmanaged, ICmdPropStruct, IEquatable<TPropType>
        {
            if (meshRendererMap.TryGetValue(mesh, out var renderer))
            {
                /*if (renderer is CommandRenderMeshProcedural<TPropType>)
                {*/

                (renderer as CmdRenderMeshStructProc<TPropType>).AddInstance(propStruct);
                needsRefresh = true;

                //instanceMap.GetOrAdd(tracker).Add(new CmdRenderProcInstanceTracker(mesh, propStruct.Matrix));
                //Extension was creating lists of min size, allocating constantly
                //Also pool lists now
                if (!instanceMap.TryGetValue(tracker, out var instanceTrackers))
                {
                    instanceTrackers = instanceTrackerListPool.Fetch();
                    instanceMap.Add(tracker, instanceTrackers);
                }

                instanceTrackers.Add(new CmdRenderProcInstanceTracker(mesh, propStruct.Matrix));

                /*}
                else
                    Debug.LogError($"Indirect renderer of <type> {renderer.GetType().GetGenericTypeDefinition()} does not match input <type> {nameof(TPropType)}!");*/
            }
            else
                Debug.LogError($"Unable to add instance of mesh {mesh}, this mesh has not been added with AddMesh<TPropStruct>()!");
        }

        /// <summary>
        /// Faster, but requires SetNeedsRefresh() and maybe ExecuteScheduledRemovals() to be called after/before respectively.
        /// </summary>
        public void AddInstanceTrackedUnsafe<TPropType>(TTracker tracker, Mesh mesh, TPropType propStruct)
            where TPropType : unmanaged, ICmdPropStruct, IEquatable<TPropType>
        {
            if (meshRendererMap.TryGetValue(mesh, out var renderer))
            {
                (renderer as CmdRenderMeshStructProc<TPropType>).AddInstanceUnsafe(propStruct);

                //instanceMap.GetOrAdd(tracker).Add(new CmdRenderProcInstanceTracker(mesh, propStruct.Matrix));
                //Extension was creating lists of min size, allocating constantly
                //Also pool lists now
                if (!instanceMap.TryGetValue(tracker, out var instanceTrackers))
                {
                    instanceTrackers = instanceTrackerListPool.Fetch();
                    instanceMap.Add(tracker, instanceTrackers);
                }

                instanceTrackers.Add(new CmdRenderProcInstanceTracker(mesh, propStruct.Matrix));
            }
            else
                Debug.LogError($"Unable to add instance of mesh {mesh}, this mesh has not been added with AddMesh<TPropStruct>()!");
        }

        public void ScheduleRemoveTracked<TPropType>(TTracker tracker)
            where TPropType : unmanaged, ICmdPropStruct, IEquatable<TPropType>
        {
            if (!this.TryGetTrackedBy(tracker, out var tracks))
                return;

            int trackCount = tracks.Count;
            for (int i = 0; i < trackCount; i++)
            {
                var track = tracks[i];

                if (meshRendererMap.TryGetValue(track.Mesh, out var renderer))
                    renderer.ScheduleRemoveByMatrix(track.Matrix);
                /*else
                {
                    //Logger.LogError($"Unable to remove instance of mesh {track.Mesh}, this mesh has not been added with AddMesh<TPropStruct>()!");
                    //Handle in this case, track is no longer valid
                    tracks.RemoveAt(i--);
                }*/
            }

            if (this.RemoveTracker(tracker, out var list))
                instanceTrackerListPool.Repool(list);

            needsRefresh = true;
        }

        public override void ClearAll()
        {
            base.ClearAll();
            instanceMap.Clear();
        }
    }

    public interface ICmdRenderInstanceGroup : ICmdRenderable, ICmdRefreshable, ICmdRemovalScheduler, IDisposable { }

    /// <summary>
    /// Convenient class to draw various meshes efficiently, with the restriction that they all share one material
    /// </summary>
    public class CommandRenderMeshesWithMatProcedural : ICmdRenderInstanceGroup
    {
        protected bool needsRefresh = true;
        public bool NeedsRefresh { get => needsRefresh; }

        public Material Material { get; set; }
        public List<ICmdRenderInstancedProc> ProcRenderables { get => procRenderables; set => procRenderables = value; }

        protected List<ICmdRenderInstancedProc> procRenderables;
        protected Dictionary<Mesh, ICmdRenderInstancedProc> meshRendererMap;
        protected Dictionary<Mesh, List<int>> meshInstanceIndexMap;

        public CommandRenderMeshesWithMatProcedural()
        {
            procRenderables = new List<ICmdRenderInstancedProc>();
            meshRendererMap = new Dictionary<Mesh, ICmdRenderInstancedProc>();
        }

        public void Dispose()
        {
            if (procRenderables != null)
            {
                for (int i = 0; i < procRenderables.Count; i++)
                {
                    procRenderables[i].Dispose();
                }
            }
        }

        public void RefreshBuffers()
        {
            for (int i = 0; i < procRenderables.Count; i++)
            {
                if (!procRenderables[i].NeedsRefresh)
                    continue;

                ICmdRenderInstancedProc renderer = procRenderables[i];
                renderer.RefreshBuffers();
            }

            needsRefresh = false;
        }

        public void SetNeedsRefresh(bool refresh)
        {
            needsRefresh = refresh;
        }

        public void RenderFrom(CommandBuffer buffer)
        {
            foreach (var indirRender in procRenderables)
            {
                indirRender.RenderFrom(buffer);
            }
        }

        public bool TryAddMesh<TPropType>(Mesh mesh, Material mat, int instancingCapacity)
            where TPropType : unmanaged, ICmdPropStruct, IEquatable<TPropType>
        {
            //Could expand to type check to render the same mesh with a different propstruct or more than one material
            if (!meshRendererMap.ContainsKey(mesh))
            {
                var newIndirRender = new CmdRenderMeshStructProc<TPropType>(mesh, new Material(mat), instancingCapacity);
                procRenderables.Add(newIndirRender);
                meshRendererMap.Add(mesh, newIndirRender);
                newIndirRender.SetNeedsRefresh(true);

                needsRefresh = true;

                return true;
            }
            return false;
        }

        public bool HasMesh(Mesh mesh)
        {
            return meshRendererMap.ContainsKey(mesh);
        }

        public void RemoveMesh(Mesh mesh)
        {
            if (meshRendererMap.ContainsKey(mesh))
            {
                var indirRenderer = meshRendererMap[mesh];
                meshRendererMap.Remove(mesh);
                procRenderables.Remove(indirRenderer);
                indirRenderer.Dispose();

                needsRefresh = true;
            }
        }

        public virtual void ClearAll()
        {
            foreach (var r in procRenderables)
            {
                r.Dispose();
            }
            procRenderables.Clear();
            meshRendererMap.Clear();

            needsRefresh = true;
        }

        public void AddInstance<TPropType>(Mesh mesh, TPropType propStruct)
            where TPropType : unmanaged, ICmdPropStruct, IEquatable<TPropType>
        {
            if (meshRendererMap.TryGetValue(mesh, out var renderer))
            {
                //Skip this safety check, it should be redundant
                /*if (renderer is CommandRenderMeshProcedural<TPropType>)
                {*/
                    (renderer as CmdRenderMeshStructProc<TPropType>).AddInstance(propStruct);

                    needsRefresh = true;
                /*}
                else
                    Debug.LogError($"Indirect renderer of <type> {renderer.GetType().GetGenericTypeDefinition()} does not match input <type> {nameof(TPropType)}!");*/
            }
            else
                Debug.LogError($"Unable to add instance of mesh {mesh}, this mesh has not been added with AddMesh<TPropStruct>()!");
        }

        public void ScheduleRemoveInstance<TPropType>(Mesh mesh, TPropType propStruct)
            where TPropType : unmanaged, ICmdPropStruct, IEquatable<TPropType>
        {
            if (meshRendererMap.TryGetValue(mesh, out var renderer))
            {
                /*if (renderer is CommandRenderMeshProcedural<TPropType>)
                {*/
                    (renderer as CmdRenderMeshStructProc<TPropType>).ScheduleRemoveInstance(ref propStruct);
                    needsRefresh = true;
                /*}
                else
                    Debug.LogError($"Indirect renderer of <type> {renderer.GetType().GetGenericTypeDefinition()} does not match input <type> {nameof(TPropType)}!");*/
            }
            else
                Debug.LogError($"Unable to remove instance of mesh {mesh}, this mesh has not been added with AddMesh<TPropStruct>()!");
        }

        /*public void ScheduleRemoveInstances<TPropType>(Mesh mesh, NativeArray<TPropType> propStruct)
            where TPropType : unmanaged, ICmdPropStruct, IEquatable<TPropType>
        {
            if (meshRendererMap.TryGetValue(mesh, out var renderer))
            {
                if (renderer is CommandRenderMeshProcedural<TPropType>)
                {
                    (renderer as CommandRenderMeshProcedural<TPropType>).ScheduleRemoveInstances(ref propStruct);
                    needsRefresh = true;
                }
                else
                    Debug.LogError($"Indirect renderer of <type> {renderer.GetType().GetGenericTypeDefinition()} does not match input <type> {nameof(TPropType)}!");
            }
            else
                Debug.LogError($"Unable to remove instance of mesh {mesh}, this mesh has not been added with AddMesh<TPropStruct>()!");
        }*/

        public void ExecuteScheduledRemovals()
        {
            foreach (var procRenderer in procRenderables)
                procRenderer.ExecuteScheduledRemovals();
        }

        public virtual void ClearInstances(Mesh mesh)
        {
            if (meshRendererMap.TryGetValue(mesh, out var r))
            {
                r.ClearInstances();
                needsRefresh = true;
            }
        }
    }
}