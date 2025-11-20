using System;
using Unity.Mathematics;

/// <summary> Represents a transformable instance of a shared mesh chunk. </summary>
public struct MeshChunkInstance : IEquatable<MeshChunkInstance>
{
    /// <summary> Identifies the shared mesh chunk. </summary>
    public UniqueMeshChunkID chunkId;
    /// <summary> Identifies the instance. </summary>
    public MeshChunkInstanceID id;
    public float4x4 transform;

    public bool Equals(MeshChunkInstance other) => id.Equals(other.id);
    public override bool Equals(object obj) => obj is MeshChunkInstance other && Equals(other);

    public override int GetHashCode() => id.GetHashCode();

    public static bool operator ==(MeshChunkInstance left, MeshChunkInstance right) => left.Equals(right);
    public static bool operator !=(MeshChunkInstance left, MeshChunkInstance right) => !left.Equals(right);
}