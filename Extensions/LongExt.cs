using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace VLib
{
    public static class LongExt
    {
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
    }
}