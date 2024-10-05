using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Mathematics;
using Debug = UnityEngine.Debug;

namespace VLib
{
    public static class UnsignedExt
    {
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void CheckPositive(this int value)
        {
            if (value < 0)
                throw new InvalidOperationException($"Value '{value}' is not positive!");
        }
        
        /// <summary> Subtracts from a uint without allowing it to fall below zero. </summary>
        public static uint SubtractClamped(this uint value, uint subtractValue)
        {
            return value < subtractValue ? 0 : value - subtractValue;
        }
    }
    
    public static class IntExt
    {
        public static void CheckInRangeAndThrow(this int index, int minInclusive, int maxExclusive)
        {
            if (index < minInclusive || index >= maxExclusive)
                throw new IndexOutOfRangeException($"Index '{index}' is not in range '{minInclusive}' [inclusive] to '{maxExclusive}' [exclusive]...");
        }

        public static string AsDataSizeInBytesToReadableString(this float value) => AsDataSizeInBytesToReadableString((long) value);

        public static string AsDataSizeInBytesToReadableString(this int value) => AsDataSizeInBytesToReadableString((long) value);

        public static string AsDataSizeInBytesToReadableString(this double value) => AsDataSizeInBytesToReadableString((long) value);
        
        public static string AsDataSizeInBytesToReadableString(this long value)
        {
            if (value < 1000)
                return $"{value} bytes";
            
            double valueF = value;
            
            if (value < 1000000)
                return $"{(valueF / 1000).ToString("N")} kilobytes";
            if (value < 1000000000)
                return $"{(valueF / 1000000).ToString("N")} megabytes";
            if (value < 1000000000000)
                return $"{(valueF / 1000000000).ToString("N")} gigabytes";
            
            return $"{(valueF / 1000000000000).ToString("N")} terabytes";
        }

        public static float ToPercent01(this ushort value) => (float) value / ushort.MaxValue;

        public static byte AsClampedByte(this int value) => (byte) math.clamp(value, byte.MinValue, byte.MaxValue);

        public static string AsTimeToPrint(this int seconds) => ((double) seconds).AsTimeToPrint();

        public struct IntRefScopeLock : IDisposable
        {
            VUnsafeRef<int> refValue;
            
            public IntRefScopeLock(VUnsafeRef<int> refValue)
            {
                this.refValue = refValue;
                refValue.ValueRef.AtomicLockUnsafe();
            }

            public void Dispose() => refValue.ValueRef.AtomicUnlockChecked();
        }
        
        public static IntRefScopeLock ScopedAtomicLock(this VUnsafeRef<int> refValue) => new(refValue);

        /// <summary> Recommend <see cref="ScopedAtomicLock"/> over this.
        /// <br/> For fast thread-safe ops, the lock must be release properly or the application can hang! (Use try-catch, etc)
        /// This method will block indefinitely until the lock is acquired. USE CAUTION</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AtomicLockUnsafe(ref this int lockValue)
        {
            // If threadlock isn't 0, the lock is taken
            while (Interlocked.CompareExchange(ref lockValue, 1, 0) != 0) {}
        }

        /// <summary>Recommend <see cref="ScopedAtomicLock"/> over this.
        /// <br/>Forcibly exits the atomic lock.</summary> 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AtomicUnlockChecked(ref this int lockValue)
        {
            ConditionalErrorIfNonZero(lockValue);
            Interlocked.Exchange(ref lockValue, 0);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void ConditionalErrorIfNonZero(this int value)
        {
            if (value == 0)
                Debug.LogError($"Value '{value}' is zero!");
        }

        /// <summary>Recommend <see cref="ScopedAtomicLock"/> over this.
        /// <br/>Forcibly exits the atomic lock.</summary> 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AtomicUnlockUnsafe(ref this int lockValue) => Interlocked.Exchange(ref lockValue, 0);

        public static ushort ToUshortSafe(this int value, bool logIfBelow0 = true, bool logIfAboveMax = true)
        {
            if (value < 0)
            {
                if (logIfBelow0)
                    Debug.LogError($"Value '{value}' is below 0, clamping to 0...");
                return 0;
            }
            if (value > ushort.MaxValue)
            {
                if (logIfAboveMax)
                    Debug.LogError($"Value '{value}' is above ushort.MaxValue, clamping to {ushort.MaxValue}...");
                return ushort.MaxValue;
            }
            return (ushort) value;
        }

        public static bool InByteRange(this int value, out byte valueB)
        {
            if (value < byte.MinValue)
            {
                valueB = byte.MinValue;
                return false;
            }
            if (value > byte.MaxValue)
            {
                valueB = byte.MaxValue;
                return false;
            }
            valueB = (byte) value;
            return true;
        }
        
        public static short ToShortSafe(this int value, bool logIfBelow0 = true, bool logIfAboveMax = true)
        {
            if (value < short.MinValue)
            {
                if (logIfBelow0)
                    Debug.LogError($"Value '{value}' is below short.MinValue, clamping to {short.MinValue}...");
                return short.MinValue;
            }
            if (value > short.MaxValue)
            {
                if (logIfAboveMax)
                    Debug.LogError($"Value '{value}' is above short.MaxValue, clamping to {short.MaxValue}...");
                return short.MaxValue;
            }
            return (short) value;
        }

        ///<summary> Automatically select (on the main thread) a random value based on a collection length. </summary>
        public static bool TryLengthValueToRandomSelection(this int lengthValue, out int randomIndex)
        {
            if (lengthValue < 1)
            {
                Debug.LogError("Cannot select a random value from a collection with no elements!");
                randomIndex = -1;
                return false;
            }
            else if (lengthValue == 1)
            {
                randomIndex = 0;
                return true;
            }
            else
            {
                randomIndex = UnityEngine.Random.Range(0, lengthValue);
                return true;
            }
        }
    }
}