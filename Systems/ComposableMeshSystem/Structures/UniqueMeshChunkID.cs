using System;

/// <summary> Refers to a unique mesh chunk, which may be shared across multiple instances. </summary>
public readonly struct UniqueMeshChunkID : IEquatable<UniqueMeshChunkID>
{
    public readonly long id;
    
    public UniqueMeshChunkID(long id) => this.id = id;

    public bool Equals(UniqueMeshChunkID other) => id == other.id;
    public override bool Equals(object obj) => obj is UniqueMeshChunkID other && Equals(other);

    public override int GetHashCode() => id.GetHashCode();

    public static bool operator ==(UniqueMeshChunkID left, UniqueMeshChunkID right) => left.Equals(right);
    public static bool operator !=(UniqueMeshChunkID left, UniqueMeshChunkID right) => !left.Equals(right);
}