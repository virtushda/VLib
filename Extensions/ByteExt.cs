using System.Runtime.CompilerServices;
//using MaxMath;
using Unity.Mathematics;

namespace VLib
{
    public static class ByteExt
    {
        /// <summary> Inverse of 1 / 255 </summary>
        public const float ByteInvMult = 1f / 255f;
        
        /// <summary> Converts byte range (0 to 255) to float range (0 to 1) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToPercent01(this byte b) => b * ByteInvMult;
        
        public static bool SbyteIsValidByte(this sbyte value, out byte byteValue)
        {
            if (value >= 0)
            {
                byteValue = (byte) value;
                return true;
            }
            byteValue = 0;
            return false;
        }
        
        /*public static bool IsValidByte2(this sbyte2 value, out byte2 byteValue, bool scaleToByteMax) 
        {
            if (math.all(value >= sbyte2.zero))
            {
                byteValue = (byte2) value * 2; // Should be safe, sbyte max should never be more than half of byte max
                return true;
            }
            byteValue = byte2.zero;
            return false;
        }

        public static bool IsValidSbyte2(this byte2 value, out sbyte2 sbValue, bool prescaleToSbyteMax)
        {
            var scaledValue = prescaleToSbyteMax ? value / 2 : value;
            if (math.all(scaledValue >= byte2.zero))
            {
                sbValue = (sbyte2) scaledValue;
                return true;
            }
            sbValue = sbyte2.zero;
            return false;
        }*/
    }
}