using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Libraries.KeyedAccessors.Lightweight;
using Unity.Burst;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using VLib;
using VLib.Threading;
using Object = UnityEngine.Object;

/// <summary>
/// A mesh that can be dynamically composed of many instances of multiple unique mesh chunks. <br/>
/// Enqueues updates and uses Burst jobs for efficient mesh rebuilding. <br/>
/// Call UpdateMesh() manually to process updates and generate the combined mesh.
/// </summary>
public class ComposableMesh : IDisposable
{
    Dictionary<Mesh, UniqueMeshChunkID> meshToChunkIDMap = new();
    
    /// <summary> Native/unmanaged data grouped together for efficient job passing and reduced allocations. </summary>
    public struct Native : IDisposable
    {
        public UnsafeKeyedMap<UniqueMeshChunkID, UniqueMeshChunk> chunks;
        public UnsafeKeyedMap<MeshChunkInstanceID, MeshChunkInstance> instances;
        public NativeList<VertexData> combinedVertices;
        public NativeList<uint> combinedIndices;
        public NativeQueue<UpdateCommand> pendingUpdates;
        
        public bool IsDirty => HasPendingUpdates || NeedsRebuild;
        
        bool needsRebuild;
        public bool NeedsRebuild
        {
            get => needsRebuild;
            private set => needsRebuild = value;
        }

        public bool HasPendingUpdates => !pendingUpdates.IsEmpty();

        public void Dispose()
        {
            chunks.Dispose();
            instances.Dispose();
            combinedVertices.Dispose();
            combinedIndices.Dispose();
            pendingUpdates.Dispose();
        }

        public void AddNewChunk(UniqueMeshChunkID chunkId, UniqueMeshChunk chunk)
        {
            chunks.Add(chunkId, chunk, out _);
            NeedsRebuild = true;
        }

        public void EnqueueCommand(in UpdateCommand command)
        {
            pendingUpdates.Enqueue(command);
            NeedsRebuild = true;
        }

        public void MarkRebuilt() => NeedsRebuild = false;
    }

    // All native/unmanaged data wrapped in RefStruct for safe sharing with jobs
    RefStruct<Native> nativeData;
    
    public bool IsCreated => nativeData.IsCreated;

    public bool IsDirty
    {
        get
        {
            ref var nativeRef = ref nativeData.TryGetRef(out var hasNative);
            return hasNative && nativeRef.IsDirty;
        }
    }

    // Vertex layout for MeshData
    static readonly VertexAttributeDescriptor[] VertexLayout = 
    {
        new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
        new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
        new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float16, 4),
        new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
        new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 2)
    };

    Mesh combinedMesh;
    /// <summary> Gets the combined mesh ready for use with MeshFilter or other systems. </summary>
    public Mesh CombinedMesh => combinedMesh;
    
    JobHandle? rebuildHandle;
    
    long nextChunkId;
    long nextInstanceId;
    
    UniqueMeshChunkID GetNewChunkID() => new UniqueMeshChunkID(Interlocked.Increment(ref nextChunkId));
    MeshChunkInstanceID GetNewInstanceID() => new MeshChunkInstanceID(Interlocked.Increment(ref nextInstanceId));

    /// <summary> Initializes the manager with specified capacities for chunks and instances. </summary>
    public ComposableMesh(int chunkCapacity = 32, int instanceCapacity = 256)
    {
        nextChunkId.IncrementToUlong();

        var allocator = Allocator.Persistent;

        var native = new Native
        {
            chunks = new UnsafeKeyedMap<UniqueMeshChunkID, UniqueMeshChunk>(allocator, chunkCapacity),
            instances = new UnsafeKeyedMap<MeshChunkInstanceID, MeshChunkInstance>(allocator, instanceCapacity),
            combinedVertices = new NativeList<VertexData>(512, allocator),
            combinedIndices = new NativeList<uint>(512, allocator),
            pendingUpdates = new NativeQueue<UpdateCommand>(allocator)
        };
        nativeData = RefStruct<Native>.Create(native);

        combinedMesh = new Mesh {name = "Instanced Combined Mesh"};
        combinedMesh.MarkDynamic();

        nextInstanceId = 0;
        nextChunkId = 0;
    }

    /// <summary> Disposes all native resources and the combined mesh. </summary>
    public void Dispose()
    {
        rebuildHandle.CompleteAndClear();

        {
            // Dispose chunks
            ref var native = ref nativeData.ValueRef;
            foreach (var kvp in native.chunks.AsReadOnly())
                kvp.Value.Dispose();
        }

        // Dispose the RefStruct and all its internal native collections
        nativeData.DisposeFullToDefault();

        if (combinedMesh != null)
            Object.DestroyImmediate(combinedMesh);
    }

    /// <summary> Adds a new shared mesh chunk. Returns the chunk ID for instance creation. </summary>
    public UniqueMeshChunkID AddSourceMesh(Mesh mesh)
    {
        MainThread.Assert();

        var chunkId = GetNewChunkID();
        UniqueMeshChunk chunk = default;
        
        using (var meshData = Mesh.AcquireReadOnlyMeshData(mesh))
        {
            var vertices = new NativeArray<Vector3>(meshData[0].vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var normals = new NativeArray<Vector3>(meshData[0].vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var tangents = new NativeArray<Vector4>(meshData[0].vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var uvs = new NativeArray<Vector2>(meshData[0].vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var colors = new NativeArray<Color32>(meshData[0].vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            
            meshData[0].GetVertices(vertices);
            meshData[0].GetNormals(normals);
            meshData[0].GetTangents(tangents);
            meshData[0].GetUVs(0, uvs);
            
            if (meshData[0].HasVertexAttribute(VertexAttribute.Color))
                meshData[0].GetColors(colors);
            else
            {
                for (int i = 0; i < colors.Length; i++)
                    colors[i] = Color.white;
            }
            
            var verticesFloat3 = vertices.Reinterpret<float3>();
            var normalsFloat3 = normals.Reinterpret<float3>();
            var tangentsFloat4 = tangents.Reinterpret<float4>();
            var uvsFloat2 = uvs.Reinterpret<float2>();
         
            // Construct compact vertex data
            var vertexData = new NativeArray<VertexData>(vertices.Length, Allocator.Temp);
            for (int i = 0; i < vertices.Length; i++)
            {
                vertexData[i] = new VertexData
                {
                    position = verticesFloat3[i],
                    normal = normalsFloat3[i],
                    tangent = (half4)tangentsFloat4[i],
                    uv = (half2)uvsFloat2[i],
                    color = colors[i]
                };
            }

            var indexData = meshData[0].GetIndexData<ushort>();
            
            chunk = UniqueMeshChunk.Create(chunkId, vertexData, indexData, Allocator.Persistent);
            
            vertices.Dispose();
            normals.Dispose();
            tangents.Dispose();
            uvs.Dispose();
            colors.Dispose();
        }

        nativeData.ValueRef.AddNewChunk(chunkId, chunk);
        
        return chunkId;
    }

    /// <summary> Adds a new shared mesh chunk. Returns the chunk ID for instance creation. </summary>
    public UniqueMeshChunkID AddSourceMesh(NativeArray<VertexData> vertices, NativeArray<ushort> indices)
    {
        MainThread.Assert();

        var chunkId = GetNewChunkID();
        var chunk = UniqueMeshChunk.Create(chunkId, vertices, indices, Allocator.Persistent);

        nativeData.ValueRef.AddNewChunk(chunkId, chunk);
        
        return chunkId;
    }

    // ADD IF NEEDED
    /*/// <summary> Removes a shared chunk and all its instances. Disposes chunk data. </summary>
    public void RemoveMeshChunk(int chunkId)
    {
        if (!chunks.RemoveSwapback(chunkId, out var removedChunk))
            return;
        
        removedChunk.Dispose();

        // Remove dependent instances
        var tempInstances = new UnsafeList<long>(Allocator.Temp);
        foreach (var kvp in instances.AsReadOnly())
        {
            if (kvp.Key != chunkId && instances.TryGetValueCopy(kvp.Key, out var inst) && inst.id != chunkId)
                tempInstances.Add(kvp.Key);
        }

        instances.Clear();
        for (int i = 0; i < tempInstances.Length; i++)
        {
            if (instances.TryGetValueCopy(tempInstances[i], out var inst))
                instances.Add(tempInstances[i], inst, out _);
        }

        tempInstances.Dispose();

        NeedsRebuild = true;
    }*/

    /// <summary> Generates a new instance of a mesh that becomes part of the final combined mesh. <br/>
    /// Call <see cref="Instance.EnqueueUpdate"/> to move. <br/>
    /// Call <see cref="Instance.Dispose"/> to remove. </summary>
    public Instance CreateInstance(Mesh sourceMesh, in float4x4 transform)
    {
        // Ensure mesh is setup
        if (!meshToChunkIDMap.TryGetValue(sourceMesh, out var meshChunkID))
        {
            meshChunkID = AddSourceMesh(sourceMesh);
            meshToChunkIDMap.Add(sourceMesh, meshChunkID);
        }

        // Get a new instance of this mesh
        var instanceID = EnqueueAddInstance(meshChunkID, transform);
        if (!instanceID.IsValid)
            throw new InvalidOperationException("Failed to create new water object!");

        return new Instance(this, instanceID);
    }

    /// <summary> A handle to a single instance of a mesh, that is part of a large composed mesh. <br/>
    /// To transform the instance, call <see cref="EnqueueUpdate"/>. <br/>
    /// To remove the instance, call <see cref="Dispose"/>. </summary>
    public struct Instance : IDisposable, IEquatable<Instance>
    {
        readonly ComposableMesh composableMesh;
        readonly MeshChunkInstanceID instanceID;

        /// <summary> Assumes the mesh instance has already been created. Not for external use. Get these through the composableMesh. </summary>
        public Instance(ComposableMesh composableMesh, MeshChunkInstanceID instanceID)
        {
            this.composableMesh = composableMesh;
            this.instanceID = instanceID;
        }

        /// <summary> Will remove the instance from the mesh composableMesh. Safe to call on default or invalid instances. </summary>
        [BurstDiscard]
        public void Dispose() => composableMesh?.EnqueueRemoveInstance(instanceID);

        /// <summary> Enqueues an update that will be processed in burst when <see cref="ComposableMesh.UpdateMesh"/> is called. Updates will be computed in order received. </summary>
        public void EnqueueUpdate(in float4x4 transform)
        {
            composableMesh.EnqueueUpdateInstance(instanceID, transform);
        }

        [BurstDiscard]
        public bool Equals(Instance other) => instanceID.Equals(other.instanceID) && composableMesh == other.composableMesh;
    }
    
    /// <summary> Low-level add call. Enqueues an instance addition. Returns an id for the new instance. </summary>
    /// <returns> Valid id if successful, default otherwise. </returns>
    public MeshChunkInstanceID EnqueueAddInstance(UniqueMeshChunkID chunkId, in float4x4 transform)
    {
        MainThread.Assert();
        
        ref var native = ref nativeData.TryGetRef(out var hasNative);
        if (!hasNative)
            return default;
        if (!native.chunks.ContainsKey(chunkId))
            return default;

        var instanceId = GetNewInstanceID();

        var command = UpdateCommand.NewAdd(chunkId, instanceId, transform);

        native.EnqueueCommand(command);

        return instanceId;
    }

    /// <summary> Low-level update call. Enqueues an instance update (transform only). </summary>
    public void EnqueueUpdateInstance(MeshChunkInstanceID id, in float4x4 newTransform)
    {
        MainThread.Assert();

        if (!id.IsValid)
            return;
        ref var native = ref nativeData.TryGetRef(out var hasNative);
        if (!hasNative)
            return;

        var command = UpdateCommand.NewUpdate(id, newTransform);
        native.EnqueueCommand(command);
    }

    /// <summary> Low-level remove call. Enqueues an instance removal. </summary>
    public void EnqueueRemoveInstance(MeshChunkInstanceID id)
    {
        MainThread.Assert();

        if (!id.IsValid)
            return;
        
        ref var native = ref nativeData.TryGetRef(out var hasNative);
        if (!hasNative)
            return;

        var command = UpdateCommand.NewRemove(id);
        native.EnqueueCommand(command);
    }

    /// <summary>
    /// Processes enqueued updates, schedules rebuild job if needed, and applies the combined mesh.
    /// Call this method manually (e.g., in LateUpdate) to update the mesh.
    /// </summary>
    public bool UpdateMesh()
    {
        ref var native = ref nativeData.TryGetRef(out var hasNative);
        if (!hasNative)
            return false;
        
        bool updated = false;
        
        if (native.HasPendingUpdates)
        {
            ProcessUpdates();
            updated = true;
        }

        if (native.NeedsRebuild)
        {
            rebuildHandle = ScheduleRebuildJob();
            rebuildHandle.CompleteAndClear();

            ApplyMeshData();

            native.MarkRebuilt();
            updated = true;
        }
        
        return updated;
    }

    /// <summary> Processes enqueued updates using a Burst-compiled job. </summary>
    void ProcessUpdates()
    {
        var job = new ProcessMeshChangesJob
        {
            NativeData = nativeData
        };

        job.Schedule().Complete();
    }

    [BurstCompile(CompileSynchronously = true)]
    struct ProcessMeshChangesJob : IJob
    {
        public RefStruct<Native> NativeData;

        public void Execute()
        {
            ref var native = ref NativeData.ValueRef;

            while (native.pendingUpdates.TryDequeue(out var cmd))
            {
                switch (cmd.Action)
                {
                    case UpdateAction.AddInstance:
                        var instance = new MeshChunkInstance {chunkId = cmd.ChunkId, id = cmd.InstanceId, transform = cmd.Transform};
                        native.instances.Add(cmd.InstanceId, instance, out _);
                        break;

                    case UpdateAction.UpdateInstance:
                        if (native.instances.TryGetValueCopy(cmd.InstanceId, out var existing))
                        {
                            existing.transform = cmd.Transform;
                            native.instances.RemoveSwapback(cmd.InstanceId, out _, out _);
                            native.instances.Add(cmd.InstanceId, existing, out _);
                        }
                        break;

                    case UpdateAction.RemoveInstance:
                        native.instances.RemoveSwapback(cmd.InstanceId, out _, out _);
                        break;
                }
            }
        }
    }

    /// <summary> Schedules a Burst job to rebuild combined mesh by transforming instance chunks. </summary>
    JobHandle ScheduleRebuildJob()
    {
        ref var native = ref nativeData.ValueRef;

        // Clear previous data
        native.combinedVertices.Clear();
        native.combinedIndices.Clear();

        var rebuildJob = new RebuildCombinedMeshJob
        {
            NativeData = nativeData
        };

        return rebuildJob.Schedule();
    }

    /// <summary>
    /// Applies job output to the Unity mesh using MeshData API.
    /// </summary>
    void ApplyMeshData()
    {
        ref var native = ref nativeData.ValueRef;

        if (native.combinedVertices.Length == 0)
        {
            combinedMesh.Clear();
            return;
        }

        var meshDataArray = Mesh.AllocateWritableMeshData(1);
        var meshData = meshDataArray[0];

        meshData.SetVertexBufferParams(native.combinedVertices.Length, VertexLayout);
        meshData.SetIndexBufferParams(native.combinedIndices.Length, IndexFormat.UInt32);

        // Copy vertices
        var vertexJob = new CopyVerticesJob
        {
            source = native.combinedVertices.AsArray(),
            target = meshData.GetVertexData<VertexData>()
        };
        vertexJob.Schedule(native.combinedVertices.Length, 256).Complete();

        var indexData = meshData.GetIndexData<uint>();
        indexData.CopyFrom(native.combinedIndices.AsArray());

        meshData.subMeshCount = 1;
        meshData.SetSubMesh(0, new SubMeshDescriptor(0, native.combinedIndices.Length, MeshTopology.Triangles)
        {
            firstVertex = 0,
            vertexCount = native.combinedVertices.Length
        }, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices);

        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, combinedMesh, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices);
        combinedMesh.RecalculateBounds();
        combinedMesh.RecalculateNormals(); // Optional; can be job-ified if needed
    }
}