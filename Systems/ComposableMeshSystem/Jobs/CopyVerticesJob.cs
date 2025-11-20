using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

/// <summary>
/// Simple copy job for vertices to MeshData.
/// </summary>
[BurstCompile(CompileSynchronously = true)]
public struct CopyVerticesJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<VertexData> source;
    public NativeArray<VertexData> target;

    public void Execute(int index)
    {
        target[index] = source[index];
    }
}