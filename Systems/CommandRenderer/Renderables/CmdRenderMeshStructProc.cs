using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace VLib
{
    public interface ICmdRenderInstancedProcBase : ICmdRenderable, IDisposable, ICmdRefreshable, ICmdRemovalScheduler
    {
        void ClearInstances();
    }
        
    public interface ICmdRenderInstancedProc : ICmdRenderInstancedProcBase
    {
        void ScheduleRemoveByMatrix(in float4x4 matrix);
    }

    public abstract class CmdRenderMeshStructProcBase<TPropStruct> : ICmdRenderInstancedProcBase
        where TPropStruct : unmanaged, ICmdPropStruct, IEquatable<TPropStruct>
    {
        static readonly int PropBuffer = Shader.PropertyToID("_PropBuffer");
        protected bool needsRefresh = true;
        protected Mesh mesh;
        protected Material material;
        protected MaterialPropertyBlock propBlock;
        protected Bounds renderBounds;
        protected NativeList<TPropStruct> properties;
        protected ComputeBuffer propertiesBuffer;
        protected NativeList<float4x4> propertiesToRemove;

        public CmdRenderMeshStructProcBase(Mesh mesh, Material materialInstance, int initialCapacity)
        {
            this.mesh = mesh;
            this.material = materialInstance;
            this.material.enableInstancing = true;

            propBlock = new MaterialPropertyBlock();

            propertiesToRemove = new NativeList<float4x4>(math.max(1, initialCapacity / 2), Allocator.Persistent);

            InitBuffers(initialCapacity);
        }

        public bool NeedsRefresh => needsRefresh;
        public int Count => properties.IsCreated ? properties.Length : 0;
        public Mesh Mesh
        {
            get => mesh;
            set => mesh = value;
        }
        public Material Material { get => material; set => material = value; }
        public Bounds DirectRenderBounds { get => renderBounds; set => renderBounds = value; }
        public ComputeBuffer PropertiesBuffer => propertiesBuffer;

        protected virtual void InitBuffers(int capacity)
        {
            //Properties
            properties = new NativeList<TPropStruct>(capacity, Allocator.Persistent);
            CreatePropBuffer(capacity);
        }

        public virtual void RefreshBuffers()
        {
            if (properties.Length == 0)
            {
                needsRefresh = false;
                return;
            }

            //Removal Pass
            ExecuteScheduledRemovals();

            //Release and recreate the buffer if it is too small
            if (propertiesBuffer.count < properties.Length)
            {
                propertiesBuffer.Release();
                CreatePropBuffer(properties.Length);
            }

            propertiesBuffer.SetData(properties.AsArray());

            propBlock.SetBuffer(PropBuffer, propertiesBuffer);
            //material.SetBuffer("_PropBuffer", propertiesBuffer);
            needsRefresh = false;
        }

        public virtual void SetNeedsRefresh(bool refresh)
        {
            needsRefresh = refresh;
        }

        protected virtual void CreatePropBuffer(int bufferLength)
        {
            var dummyProp = new TPropStruct();
            propertiesBuffer = new ComputeBuffer(bufferLength, dummyProp.SizeOf, ComputeBufferType.Structured);
        }

        public bool TryGetPropertiesList(out NativeList<TPropStruct> propertyList)
        { 
            if (properties.IsCreated)
            {
                propertyList = properties;
                return true;
            }
            else
            {
                propertyList = default;
                return false;
            }
        }

        public virtual void ExecuteScheduledRemovals()
        {
            //Could thread removal job and catch/complete it if any updates need to be made...
            if (propertiesToRemove.Length > 0)
            {
                //properties.RemoveElements(propertiesToRemove.AsArray());
                //Part of an effort to reduce garbage, generics and structs don't work very well together :\
                var jahb = new BufferRemoveByMatrixMatchJob<TPropStruct>(ref properties, propertiesToRemove.AsArray());
                jahb.Schedule().Complete();

                propertiesToRemove.Clear();
            }
        }

        /// <summary> Call 'RefreshBuffers()' after you are finished changes! </summary>
        public virtual void ClearInstances()
        {
            properties.Clear();
            needsRefresh = true;
        }

        public virtual void RenderFrom(CommandBuffer buffer)
        {
            if (properties.Length < 1)
                return;

            buffer.DrawMeshInstancedProcedural(mesh, 0, material, -1, properties.Length, propBlock);
        }

        /// <summary>
        /// If there is an issue with stuff not rendering, ensure you have set the DirectRenderBounds
        /// </summary>
        public virtual void RenderDirect()
        {
            if (properties.Length < 1)
                return;

            Graphics.DrawMeshInstancedProcedural(mesh, 0, material, renderBounds, properties.Length);
        }

        public virtual void Dispose()
        {
            if (properties.IsCreated)
                properties.Dispose();
            if (propertiesToRemove.IsCreated)
                propertiesToRemove.Dispose();

            propertiesBuffer?.Release();
            propertiesBuffer = null;
        }

        [BurstCompile]
        protected struct BufferRemoveByMatrixMatchJob<T> : IJob
            where T : unmanaged, IEquatable<T>, ICmdTransform
        {
            NativeList<T> list;
            [ReadOnly] NativeArray<float4x4> toRemove;

            public BufferRemoveByMatrixMatchJob(ref NativeList<T> list, NativeArray<float4x4> toRemove)
            {
                this.list = list;
                this.toRemove = toRemove;
            }

            public void Execute()
            {
                //Populate hashset for efficient checks
                NativeParallelHashMap<float4x4, byte> hashset = new NativeParallelHashMap<float4x4, byte>(toRemove.Length, Allocator.Temp);
                for (int i = 0; i < toRemove.Length; i++)
                    hashset.TryAdd(toRemove[i], 0);

                //Check and Remove From List
                for (int i = 0; i < list.Length; i++)
                {
                    if (hashset.ContainsKey(list[i].Matrix))
                    {
                        list.RemoveAtSwapBack(i);
                        i--;
                    }
                #if DEBUGJAHBS
                    else
                    {
                        UnityEngine.Debug.LogError("List.RemoveElementsSwapbackJob failed!");
                    }
                #endif
                }

                hashset.Dispose();
            }
        }
    }

    public class CmdRenderMeshStructProc<TPropStruct> : CmdRenderMeshStructProcBase<TPropStruct>, ICmdRenderInstancedProc
        where TPropStruct : unmanaged, ICmdPropStruct, IEquatable<TPropStruct>
    {
        public CmdRenderMeshStructProc(Mesh mesh, Material materialInstance, int initialCapacity) : base(mesh, materialInstance, initialCapacity)
        {
        }

        /// <summary> Call 'RefreshBuffers()' after you are finished changes!
        /// Calling the method AFTER 'ScheduleRemoveInstance' or any variants of that, will force the removal to happen immediately. </summary>
        public virtual int AddInstance(in TPropStruct instanceData)
        {
            ExecuteScheduledRemovals();

            int count = properties.Length;
            properties.Add(instanceData);
            needsRefresh = true;
            return count;
        }
        
        /// <summary> 
        /// Doesn't execute scheduled removals first, also doesn't set 'needsRefresh' internally. 
        /// Methods skipped: ExecuteScheduledRemovals() and SetNeedsRefresh(bool refresh)
        /// </summary>
        public virtual void AddInstanceUnsafe(in TPropStruct instanceData)
        {
            properties.Add(instanceData);
        }

        public virtual void ScheduleRemoveInstance(ref TPropStruct propStruct)
        {
            propertiesToRemove.Add(propStruct.Matrix);
            needsRefresh = true;
        }

        public virtual void ScheduleRemoveByMatrix(in float4x4 matrix)
        {
            propertiesToRemove.Add(matrix);
            needsRefresh = true;
        }

        #region Jobs
        //Custom job, verbose to avoid garbagio

        #endregion
    }

    public static class CmdRenderMeshProcExt
    {
        /// <summary> 
        /// Doesn't execute scheduled removals first, also doesn't set 'needsRefresh' internally. 
        /// Methods skipped: ExecuteScheduledRemovals() and SetNeedsRefresh(bool refresh)
        /// </summary>
        public static bool AddInstanceSortedUnsafe<TPropStruct>(this CmdRenderMeshStructProc<TPropStruct> cmdRender, TPropStruct instanceData)
            where TPropStruct : unmanaged, ICmdPropStruct, IEquatable<TPropStruct>, IComparable<TPropStruct>
        {
            if (cmdRender.TryGetPropertiesList(out var properties))
            {
                properties.AddSortedExclusive(instanceData, out _);
                return true;
            }

            return false;
        }
        
        /// <summary> 
        /// Doesn't set 'needsRefresh' internally. 
        /// Method skipped: SetNeedsRefresh(bool refresh)
        /// </summary>
        public static bool RemoveInstanceSortedUnsafe<TPropStruct>(this CmdRenderMeshStructProc<TPropStruct> cmdRender, TPropStruct instanceData)
            where TPropStruct : unmanaged, ICmdPropStruct, IEquatable<TPropStruct>, IComparable<TPropStruct>
        {
            if (cmdRender.TryGetPropertiesList(out var properties))
            {
                properties.RemoveSorted(instanceData);
                return true;
            }

            return false;
        }
    }
}