using System;
using Unity.Mathematics;

namespace VLib.Structures
{
    [Serializable]
    public struct Ushort2 : IEquatable<Ushort2>
    {
        public ushort x;
        public ushort y;
        
        public Ushort2(ushort x, ushort y)
        {
            this.x = x;
            this.y = y;
        }
        
        public static implicit operator uint2(Ushort2 value) => new(value.x, value.y);
        public static explicit operator Ushort2(int2 value) => new((ushort)value.x, (ushort)value.y);
        public static explicit operator int2(Ushort2 value) => new(value.x, value.y);
        public static explicit operator Ushort2(float2 value) => new((ushort)value.x, (ushort)value.y);
        public static explicit operator float2(Ushort2 value) => new(value.x, value.y);

        public bool Equals(Ushort2 other) => x == other.x && y == other.y;
        public override bool Equals(object obj) => obj is Ushort2 other && Equals(other);
        
        public static bool2 operator ==(Ushort2 left, Ushort2 right) => new bool2(left.x.Equals(right.x), left.y.Equals(right.y));
        public static bool2 operator !=(Ushort2 left, Ushort2 right) => new bool2(!left.x.Equals(right.x), !left.y.Equals(right.y));

        public override int GetHashCode()
        {
            unchecked
            {
                return (x.GetHashCode() * 397) ^ y.GetHashCode();
            }
        }
    }
}