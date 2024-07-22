using MaxMath;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace VLib
{
    public static class ShortExt
    {
        public static bool IsValidByte(this short value, out byte byteValue)
        {
            if (value >= 0 && value <= byte.MaxValue)
            {
                byteValue = (byte) value;
                return true;
            }
            byteValue = 0;
            return false;
        }
        
        public static bool IsValidByte2(this short2 value, out byte2 byteValue)
        {
            if (math.all(value >= 0) && math.all(value <= byte.MaxValue))
            {
                byteValue = (byte2) value;
                return true;
            }
            byteValue = byte2.zero;
            return false;
        }

        public static bool IsValidByte2(this ushort2 value, out byte2 byteValue)
        {
            if (math.all(value >= 0) && math.all(value <= byte.MaxValue))
            {
                byteValue = (byte2) value;
                return true;
            }
            byteValue = byte2.zero;
            return false;
        }

        ///<summary> Throws in editor if value out of range </summary>
        public static byte ToByte(this ushort value)
        {
            Assert.IsTrue(value <= byte.MaxValue);
            return (byte) value;
        }
        
        public static short ToShort(this ushort value)
        {
            Assert.IsTrue(value <= short.MaxValue);
            return (short) value;
        }
    }
}