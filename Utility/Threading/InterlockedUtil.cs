using System.Threading;
using UnityEngine;

namespace VLib.Threading
{
    public static class InterlockedUtil
    {
        /// <summary> Atomically attempt to write a value. If the write is interrupted, it returns false. </summary>
        public static bool TryAtomicWrite(ref int target, int newValue)
        {
            // Store a copy of the target value to ensure it didn't change between the read and write
            var comparand = target;
            return Interlocked.CompareExchange(ref target, newValue, comparand) == comparand;
        }

        /// <summary> Supreme method from the school of insanity. Spin write against a value atomically to ensure the write operation can handle being interrupted. </summary>
        public static bool TrySpinningAtomicWrite(ref int target, int newValue, int maxIterations = 128)
        {
            while (--maxIterations > 0)
            {
                if (TryAtomicWrite(ref target, newValue))
                    return true;
            }
            Debug.LogError("Failed to write atomically after spinning, you are not the dragon warrior!");
            return false;
        }
    }
}