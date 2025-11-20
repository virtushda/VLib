using Unity.Mathematics;

/// <summary>
/// Struct for enqueuing updates to instances. Burst-compatible for potential job extension.
/// </summary>
public struct UpdateCommand
{
    public UpdateAction Action { get; private set; }
    public UniqueMeshChunkID ChunkId { get; private set; } // For add
    public MeshChunkInstanceID InstanceId { get; private set; } // For update/remove
    public float4x4 Transform { get; private set; } // For add/update

    UpdateCommand(UpdateAction action, UniqueMeshChunkID chunkId, MeshChunkInstanceID instanceId, float4x4 transform)
    {
        Action = action;
        ChunkId = chunkId;
        InstanceId = instanceId;
        Transform = transform;
    }
    
    public static UpdateCommand NewAdd(UniqueMeshChunkID chunkId, MeshChunkInstanceID instanceId, float4x4 transform)
    {
        return new UpdateCommand(UpdateAction.AddInstance, chunkId, instanceId, transform);
    }
    
    public static UpdateCommand NewUpdate(MeshChunkInstanceID instanceId, float4x4 transform)
    {
        return new UpdateCommand(UpdateAction.UpdateInstance, default, instanceId, transform);
    }
    
    public static UpdateCommand NewRemove(MeshChunkInstanceID instanceId)
    {
        return new UpdateCommand(UpdateAction.RemoveInstance, default, instanceId, default);
    }
}