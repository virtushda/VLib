using System;
using Unity.Mathematics;

namespace VLib.Structures
{
    [Serializable]
    public struct Nibble2 : IEquatable<Nibble2>
    {
        public const byte MinValue = 0;
        public const byte MaxValue = 15;

        public byte rawValue;

        public byte x
        {
            get => BitUtility.Unpack_Byte_To_2bytes(rawValue).x;
            set => rawValue = BitUtility.Pack_2bytes_To_Byte(value, y);
        }

        public byte y
        {
            get => BitUtility.Unpack_Byte_To_2bytes(rawValue).y;
            set => rawValue = BitUtility.Pack_2bytes_To_Byte(x, value);
        }

        public Nibble2(byte x, byte y) => rawValue = BitUtility.Pack_2bytes_To_Byte(x, y);

        public static byte ClampToNibbleRange(int value) => (byte) math.clamp(value, 0, 15);
        public static byte Float01ToNibbleRange(float value) => (byte) math.clamp(value * 15.999999f, 0, 15);

        public static implicit operator int2(Nibble2 value) => new(value.x, value.y);
        public static implicit operator uint2(Nibble2 value) => new(value.x, value.y);
        public static explicit operator Nibble2(int2 value) => new(ClampToNibbleRange(value.x), ClampToNibbleRange(value.y));
        public static explicit operator Nibble2(float2 value) => new(Float01ToNibbleRange(value.x), Float01ToNibbleRange(value.y));
        public static explicit operator float2(Nibble2 value) => new(value.x, value.y);

        public bool Equals(Nibble2 other) => rawValue == other.rawValue;
        public override bool Equals(object obj) => obj is Nibble2 other && Equals(other);

        public override int GetHashCode() => rawValue;
        
        public static bool2 operator ==(Nibble2 left, Nibble2 right) => new bool2(left.rawValue.Equals(right.rawValue));
        public static bool2 operator !=(Nibble2 left, Nibble2 right) => new bool2(!left.rawValue.Equals(right.rawValue));
    }
}