using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace VLib
{
    public static class LongExt
    {
        /// <summary> Clamps the long to uint range. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUIntClamped(this long value, bool errorIfClamped = true)
        {
            switch (value)
            {
                case < 0: 
                    if (errorIfClamped)
                        UnityEngine.Debug.LogError($"Value '{value}' is below 0, clamping to 0...");
                    return 0;
                case > uint.MaxValue: 
                    if (errorIfClamped)
                        UnityEngine.Debug.LogError($"Value '{value}' is above uint.MaxValue, clamping to {uint.MaxValue}...");
                    return uint.MaxValue;
                default: return (uint) value;
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUIntOrThrow(this long value)
        {
            return value switch
            {
                < 0 => throw new ArgumentOutOfRangeException($"Value '{value}' is below 0!"),
                > uint.MaxValue => throw new ArgumentOutOfRangeException($"Value '{value}' is above uint.MaxValue!"),
                _ => (uint) value
            };
        }
        
        /// <summary> Offsets the long into ulong range, guarding against overflow. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ToUlongRange(this long value)
        {
            if (value < 0)
                return (ulong) (value + long.MaxValue); // Offset before cast
            else
                return ((ulong) value) + long.MaxValue; // Cast before offset
        }

        /// <summary> Offsets the ulong into long range, guarding against overflow. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ToLongRange(this ulong value)
        {
            if (value <= long.MaxValue) // In long range
                return ((long)value) - long.MaxValue; // Cast before offset
            else // Outside long range
                return (long) (value - long.MaxValue); // Offset before cast
        }
        
        public static string AsTimeToPrint(this long seconds) => ((double) seconds).AsTimeToPrint();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUnityMathematicsHash(this long value) => (uint) (value ^ (value >> 32));

        /// <summary> ID provisioning method to gets you the FULL ulong range. Start your long at -long.MaxValue. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong IncrementToUlong(ref this long nextID) => Interlocked.Increment(ref nextID).ToUlongRange();
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void CheckValueInRange(this long valueL, long min, long max)
        {
            if (valueL < min || valueL > max)
                throw new ArgumentOutOfRangeException($"Value '{valueL}' is not in range {min} to {max}!");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint HashToUInt(this ulong value) => (uint)(value ^ (value >> 32));

        ///<summary>Takes a ulong value and uniformly redistributes it somewhere else in the ulong range</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Rehash(this ulong value)
        {
            value ^= value >> 33;
            value *= 0xff51afd7ed558ccdL;
            value ^= value >> 33;
            value *= 0xc4ceb9fe1a85ec53L;
            value ^= value >> 33;
            return value;
        }

        /// <summary>If using a ulong as a fixed size array of 8 bytes, this method will return a byte
        /// where each bit will be one if the byte value at each position in the original ulong was larger than zero</summary>
        public static byte GetNonZeroBytesAsBits(this ulong value)
        {
            const ulong mask = 0x8080808080808080UL;
    
            // Create a mask where each byte has its high bit set if the byte is non-zero
            var temp = value;
            temp |= temp << 1;
            temp |= temp << 2;
            temp |= temp << 4;
            temp &= mask;
    
            // Compress the high bits into a single byte
            temp = (temp >> 7) | (temp >> 14) | (temp >> 21) | (temp >> 28) |
                   (temp >> 35) | (temp >> 42) | (temp >> 49) | (temp >> 56);
            return (byte)(temp & 0xFF);
        }
    }
}