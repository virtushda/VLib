using Unity.Mathematics;

namespace VLib.Structures
{
    public struct Sbyte2
    {
        public sbyte x;
        public sbyte y;
        
        public Sbyte2(sbyte x, sbyte y)
        {
            this.x = x;
            this.y = y;
        }

        public bool IsValidByte2(out Byte2 byte2)
        {
            if (x >= 0 && y >= 0)
            {
                byte2 = new Byte2((byte) x, (byte) y);
                return true;
            }
            byte2 = new Byte2();
            return false;
        }
        
        /*public bool FitsInSbyte(int value, out sbyte sbyteValue)
        {
            if (value >= sbyte.MinValue && value <= sbyte.MaxValue)
            {
                sbyteValue = (sbyte) value;
                return true;
            }
            sbyteValue = 0;
            return false;
        }*/
        
        public static implicit operator int2(Sbyte2 value) => new(value.x, value.y);
        public static explicit operator Sbyte2(int2 value)
        {
            BurstAssert.True(math.all(value >= sbyte.MinValue) && math.all(value <= sbyte.MaxValue));
            return new Sbyte2((sbyte) value.x, (sbyte) value.y);
        }
        public static explicit operator Sbyte2(int value)
        {
            BurstAssert.True(value >= sbyte.MinValue && value <= sbyte.MaxValue);
            return new Sbyte2((sbyte) value, (sbyte) value);
        }
    }
}