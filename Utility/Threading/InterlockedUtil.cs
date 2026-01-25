using System.Threading;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace VLib.Threading
{
    public static class InterlockedUtil
    {
        /// <summary> Atomically attempt to write a value. If the write is interrupted, it returns false. </summary>
        public static bool TryAtomicWrite(ref int target, int newValue, out int overwrittenValue)
        {
            // Store a copy of the target value to ensure it didn't change between the read and write
            overwrittenValue = target;
            return Interlocked.CompareExchange(ref target, newValue, overwrittenValue) == overwrittenValue;
        }

        /// <summary> Supreme method from the school of insanity. Spin write against a value atomically to ensure the write operation can handle being interrupted. </summary>
        public static bool TrySpinningAtomicWrite(ref int target, int newValue, int maxIterations = 128)
        {
            while (--maxIterations > 0)
            {
                if (TryAtomicWrite(ref target, newValue, out _))
                    return true;
            }
            Debug.LogError("Failed to write atomically after spinning, you are not the dragon warrior!");
            return false;
        }

        /// <summary> Attempts to write an increased value, failing if the value is already raised above the newvalue. </summary>
        public static bool TrySpinningAtomicWriteMax(ref int target, int newValue, int maxIterations = 128)
        {
            while (--maxIterations > 0)
            {
                if (newValue <= Volatile.Read(ref target))
                    return false;
                if (TryAtomicWrite(ref target, newValue, out _))
                    return true;
            }
            Debug.LogError("Failed to write atomically after spinning, you are not the dragon warrior!");
            return false;
        }

        /// <summary> Performs an increment that is wrapped by a modulo value. Spins until successful or a HUGE amount of tries have passed. </summary>
        public static int IncrementModuloSpinning(ref int target, int modulo)
        {
            if (modulo <= 1)
                throw new System.ArgumentException("Modulo must be greater than 1.");

            int tries = 10000;
            while (--tries > 0)
            {
                int original = Volatile.Read(ref target);
                int incremented = (original + 1) % modulo;
                if (Interlocked.CompareExchange(ref target, incremented, original) == original)
                    return incremented;
            }
            
            Debug.LogError("Failed to increment atomically after spinning, you are not the dragon warrior!");
            return target;
        }

        /// <summary> Uses a tight loop to decrement a value if it is above a threshold. Operation cannot be atomic, but is defensive to ensure the threshold is not breached. </summary>
        public static int DecrementIfAbove(ref int value, int threshold)
        {
            int tries = 10000;
            do
            {
                var current = Volatile.Read(ref value);
                // Already below threshold
                if (current <= threshold)
                    return value;
                
                var target = current - 1;
                // Decrement with atomic comparison to ensure we don't go below threshold even in tight contention scenarios
                if (Interlocked.CompareExchange(ref value, target, current) == current)
                    return target;
            }
            while (--tries > 0);
            
            Debug.LogError("Failed to decrement atomically after spinning, you are not the dragon warrior!");
            return value;
        }
    }
}