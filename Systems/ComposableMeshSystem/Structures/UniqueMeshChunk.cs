using Unity.Collections;

/// <summary>
/// Unmanaged struct representing shared mesh chunk data. Holds vertex and index buffers for reuse across instances.
/// Implements IDisposable for safe cleanup of native resources.
/// </summary>
public struct UniqueMeshChunk : System.IDisposable
{
    public UniqueMeshChunkID id;
    public NativeList<VertexData> vertices;
    public NativeList<ushort> indices;

    /// <summary> Creates a new chunk from input arrays, transferring ownership. </summary>
    public static UniqueMeshChunk Create(UniqueMeshChunkID id, NativeArray<VertexData> vertices, NativeArray<ushort> indices, Allocator allocator)
    {
        var chunk = new UniqueMeshChunk
        {
            id = id,
            vertices = new NativeList<VertexData>(vertices.Length, allocator),
            indices = new NativeList<ushort>(indices.Length, allocator)
        };
        
        // Copy data from input arrays
        chunk.vertices.AddRange(vertices);
        chunk.indices.AddRange(indices);
        
        return chunk;
    }

    /// <summary>
    /// Disposes native buffers. Call explicitly when removing a chunk.
    /// </summary>
    public void Dispose()
    {
        if (vertices.IsCreated) 
            vertices.Dispose();
        if (indices.IsCreated) 
            indices.Dispose();
    }
}