using System;
using Unity.Mathematics;

namespace VLib.Structures
{
    [Serializable]
    public struct Ushort3 : IEquatable<Ushort3>
    {
        public ushort x;
        public ushort y;
        public ushort z;
        
        public Ushort3(ushort x, ushort y, ushort z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        
        public static implicit operator uint3(Ushort3 value) => new(value.x, value.y, value.z);
        public static explicit operator Ushort3(int3 value) => new((ushort)value.x, (ushort)value.y, (ushort)value.z);
        public static explicit operator int3(Ushort3 value) => new(value.x, value.y, value.z);
        public static explicit operator Ushort3(float3 value) => new((ushort)value.x, (ushort)value.y, (ushort)value.z);
        public static explicit operator float3(Ushort3 value) => new(value.x, value.y, value.z);

        public bool Equals(Ushort3 other) => x == other.x && y == other.y && z == other.z;
        public override bool Equals(object obj) => obj is Ushort3 other && Equals(other);

        public static bool3 operator ==(Ushort3 left, Ushort3 right) => new bool3(left.x == right.x, left.y == right.y, left.z == right.z);
        public static bool3 operator !=(Ushort3 left, Ushort3 right) => new bool3(left.x != right.x, left.y != right.y, left.z != right.z);
        public static bool3 operator ==(Ushort3 left, int3 right) => new bool3(left.x == right.x, left.y == right.y, left.z == right.z);
        public static bool3 operator !=(Ushort3 left, int3 right) => new bool3(left.x != right.x, left.y != right.y, left.z != right.z);

        public override int GetHashCode()
        {
            unchecked
            {
                return (x.GetHashCode() * 397) ^ (y.GetHashCode() * 397) ^ z.GetHashCode();
            }
        }
    }
}