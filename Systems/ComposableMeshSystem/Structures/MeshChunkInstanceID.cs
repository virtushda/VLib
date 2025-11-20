using System;

/// <summary> Unmanaged id for safely referencing a mesh chunk instance by ID. </summary>
public readonly struct MeshChunkInstanceID : IEquatable<MeshChunkInstanceID>
{
    public readonly long id;

    public bool IsValid => id > 0;

    public MeshChunkInstanceID(long id) => this.id = id;

    public bool Equals(MeshChunkInstanceID other) => id == other.id;
    public override bool Equals(object obj) => obj is MeshChunkInstanceID other && Equals(other);

    public override int GetHashCode() => id.GetHashCode();

    public static bool operator ==(MeshChunkInstanceID left, MeshChunkInstanceID right) => left.Equals(right);
    public static bool operator !=(MeshChunkInstanceID left, MeshChunkInstanceID right) => !left.Equals(right);
}