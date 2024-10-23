using System;
using Unity.Mathematics;

namespace VLib.Structures
{
    [Serializable]
    public struct Short3 : IEquatable<Short3>
    {
        public short x;
        public short y;
        public short z;
        
        public Short3(short x, short y, short z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        
        public static implicit operator Short3(int3 value) => new((short) value.x, (short) value.y, (short) value.z);
        public static implicit operator float3(Short3 value) => new(value.x, value.y, value.z);
        public static explicit operator Short3(float3 value)
        {
            BurstAssert.True(math.all(math.abs(value) <= short.MaxValue));
            return new Short3((short) value.x, (short) value.y, (short) value.z);
        }

        public bool Equals(Short3 other) => x == other.x && y == other.y && z == other.z;
        public override bool Equals(object obj) => obj is Short3 other && Equals(other);
        
        public static bool3 operator ==(Short3 left, Short3 right) => new(left.x == right.x, left.y == right.y, left.z == right.z);
        public static bool3 operator !=(Short3 left, Short3 right) => new(left.x != right.x, left.y != right.y, left.z != right.z);
        public static bool3 operator ==(Short3 left, int3 right) => new(left.x == right.x, left.y == right.y, left.z == right.z);
        public static bool3 operator !=(Short3 left, int3 right) => new(left.x != right.x, left.y != right.y, left.z != right.z);
        
        public override int GetHashCode()
        {
            unchecked
            {
                return (x.GetHashCode() * 197) ^ (y.GetHashCode() * 197) ^ z.GetHashCode();
            }
        }
    }
}