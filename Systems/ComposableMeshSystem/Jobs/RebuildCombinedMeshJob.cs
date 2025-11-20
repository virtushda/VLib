using Libraries.KeyedAccessors.Lightweight;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using VLib;

/// <summary> Burst-compiled job to rebuild combined mesh by iterating instances and transforming shared chunk data. </summary>
[BurstCompile(CompileSynchronously = true)]
public struct RebuildCombinedMeshJob : IJob
{
    public RefStruct<ComposableMesh.Native> NativeData;

    public void Execute()
    {
        ref var native = ref NativeData.ValueRef;
        int vertexOffset = 0;

        foreach (var instanceKvp in native.instances.AsReadOnly())
        {
            var instance = instanceKvp.Value;

            // Lookup chunk
            ref readonly var chunkRef = ref native.chunks.AsReadOnly().TryGetValueRefReadOnly(instance.chunkId, out var hasChunkRef);
            if (!hasChunkRef)
                continue;

            var verts = chunkRef.vertices;
            var inds = chunkRef.indices;

            if (verts.Length == 0)
                continue;

            // Transform and copy vertices
            for (int i = 0; i < verts.Length; i++)
            {
                var vertCopy = verts[i];
                vertCopy.position = math.transform(instance.transform, vertCopy.position);
                var invTrans = math.inverse(math.transpose(instance.transform));
                vertCopy.normal = math.normalize(math.mul((float3x3)invTrans, vertCopy.normal)); // Proper normal transform
                native.combinedVertices.Add(vertCopy);
            }

            // Offset and copy indices
            for (int i = 0; i < inds.Length; i++)
                native.combinedIndices.Add((uint)(inds[i] + vertexOffset));

            vertexOffset += verts.Length;
        }
    }
}