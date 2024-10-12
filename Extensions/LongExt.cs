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
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void CheckValueInRange(this long valueL, long min, long max)
        {
            if (valueL < min || valueL > max)
                throw new ArgumentOutOfRangeException($"Value '{valueL}' is not in range {min} to {max}!");
        }
    }
}