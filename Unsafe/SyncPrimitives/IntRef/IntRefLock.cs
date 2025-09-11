using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using VLib.Systems;
using Debug = UnityEngine.Debug;

namespace VLib.SyncPrimitives.IntRef
{
    /// <summary> A much smaller, less cache-efficient but more computationally and memory efficient lock that provides simple exclusive lock access in unmanaged code. </summary>
    public struct IntRefScopeLock : IDisposable
    {
        VUnsafeRef<int> refValue;
        
        public bool IsCreated => refValue.IsCreated;
        public static implicit operator bool(IntRefScopeLock d) => d.IsCreated;

        public IntRefScopeLock(VUnsafeRef<int> refValue, float timeoutSeconds = 2f)
        {
            refValue.ConditionalCheckIsCreated();

            if (!refValue.ValueRef.TryAtomicLockUnsafe(timeoutSeconds))
            {
                Debug.LogError("Failed to acquire lock!");
                this = default;
                return;
            }
            
            // Only set if lock obtained
            this.refValue = refValue;
        }

        public void Dispose()
        {
            if (refValue.IsCreated)
                refValue.ValueRef.AtomicUnlockChecked();
        }
    }

    public static class IntRefLockExt
    {
        public static IntRefScopeLock ScopedAtomicLock(this VUnsafeRef<int> refValue, float timeoutSeconds = 2f) => new(refValue, timeoutSeconds);
        
        public static bool TryScopedAtomicLock(this VUnsafeRef<int> refValue, out IntRefScopeLock scopeLock, float timeoutSeconds = 2f)
        {
            scopeLock = new(refValue, timeoutSeconds);
            return scopeLock.IsCreated;
        }

        /// <summary> Recommend <see cref="ScopedAtomicLock"/> over this.
        /// <br/> For fast thread-safe ops, the lock must be release properly or the application can hang! (Use try-catch, etc)
        /// This method will block indefinitely until the lock is acquired. USE CAUTION</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAtomicLockUnsafe(ref this int lockValue, float timeoutSeconds = 2f)
        {
            var timeoutTime = (float)(VTime.intraFrameTime + timeoutSeconds);
            // If threadlock isn't 0, the lock is taken
            while (Interlocked.CompareExchange(ref lockValue, 1, 0) != 0)
            {
                if (VTime.intraFrameTime > timeoutTime)
                    return false;
            }
            return true;
        }

        /// <summary>Recommend <see cref="ScopedAtomicLock"/> over this.
        /// <br/>Forcibly exits the atomic lock.</summary> 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AtomicUnlockChecked(ref this int lockValue)
        {
            ConditionalErrorIfZero(lockValue);
            Interlocked.Exchange(ref lockValue, 0);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        static void ConditionalErrorIfZero(this int value)
        {
            if (value == 0)
                Debug.LogError($"Value '{value}' is zero!");
        }
    }
}