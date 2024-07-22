using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Burst.Intrinsics;
using Unity.IL2CPP.CompilerServices;

namespace VLib
{
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    public static class BurstSpinLockReadWriteFunctions
    {
        /// <summary> Lock Exclusive. Will block if cannot lock immediately </summary>
        /// <returns>True if lock acquired, false if not </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEnterExclusiveBlocking(ref long lockVar, ref long readersVar, in GlobalBurstTimer burstTimer, float timeoutSeconds)
        {
#if MARK_THREAD_OWNERS
            var threadId = Baselib.LowLevel.Binding.Baselib_Thread_GetCurrentThreadId().ToInt64();
            BurstSpinLockCheckFunctions.CheckForRecursiveLock(threadId, ref lockVar);
#else
            var threadId = 1;
#endif

            // Safety expected to be implemented higher up
            float exitLockTime = burstTimer.TimeUnsafe + timeoutSeconds;
            
            while (Interlocked.CompareExchange(ref lockVar, threadId, 0) != 0)
            {
                if (burstTimer.Time > exitLockTime)
                    return false;
                Common.Pause();
                    
                // Don't have access for some reason, thanks Unity
                //Baselib.LowLevel.Binding.Baselib_Thread_YieldExecution();
            }

#if DEBUG_DEADLOCKS
            var deadlockGuard = 0;
#endif

            // while we have readers
            while (Interlocked.Read(ref readersVar) != 0)
            {
                if (burstTimer.Time > exitLockTime)
                    return false;
                Common.Pause();
                    
                // Don't have access for some reason, thanks Unity
                //Baselib.LowLevel.Binding.Baselib_Thread_YieldExecution();

#if DEBUG_DEADLOCKS
                if (++deadlockGuard == 512)
                {
                    UnityEngine.Debug.LogError("Cannot get spin lock, because of readers");
                    Interlocked.Exchange(ref lockVar, 0);
                    throw new Exception();
                }
#endif
            }

            return true;
        }

        /// <summary> Try to lock Exclusive. Won't block </summary>
        /// <returns>True if locked</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEnterExclusive(ref long lockVar, ref long readersVar)
        {
            if (Interlocked.Read(ref readersVar) != 0)
                return false;

#if MARK_THREAD_OWNERS
            var threadId = Baselib.LowLevel.Binding.Baselib_Thread_GetCurrentThreadId().ToInt64();
            BurstSpinLockCheckFunctions.CheckForRecursiveLock(threadId, ref lockVar);
#else
            var threadId = 1;
#endif

            if (Interlocked.CompareExchange(ref lockVar, threadId, 0) != 0)
                return false;

            if (Interlocked.Read(ref readersVar) != 0)
            {
                Interlocked.Exchange(ref lockVar, 0);
            }

            return true;
        }

        /// <summary>
        /// Unlock
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExitExclusive(ref long lockVar)
        {
            BurstSpinLockCheckFunctions.CheckWeCanExit(ref lockVar);
            Interlocked.Exchange(ref lockVar, 0);
        }

        /// <summary>
        /// Lock for Read. Will block if exclusive is locked
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEnterReadBlocking(ref long lockVar, ref long readersVar, in GlobalBurstTimer burstTimer, float timeoutSeconds)
        {
            BurstSpinLockCheckFunctions.CheckForRecursiveLock(ref lockVar);

            // Don't need timer safety in here, callers are expected to implement safety higher up
            var exitLockTime = burstTimer.TimeUnsafe + timeoutSeconds;
            
            // Loop until we get lock or time out
            while (true)
            {
                Interlocked.Increment(ref readersVar);

                // if not locked
                if (Interlocked.Read(ref lockVar) == 0)
                {
                    return true;
                }

                // fail, it is locked
                Interlocked.Decrement(ref readersVar);

                // while it is locked - spin
                while (Interlocked.Read(ref lockVar) != 0)
                {
                    if (burstTimer.Time > exitLockTime)
                        return false;
                    Common.Pause();
                    
                    // Don't have access for some reason, thanks Unity
                    //Baselib.LowLevel.Binding.Baselib_Thread_YieldExecution();
                }
            }
        }

        /// <summary>
        /// Exit read lock. EnterRead must be called before this call by the same thread
        /// </summary>
        /// <param name="readersVar"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExitRead(ref long readersVar)
        {
            Interlocked.Decrement(ref readersVar);
            CheckForNegativeReaders(ref readersVar);
        }

        [Conditional("DEBUG_ADDITIONAL_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CheckForNegativeReaders(ref long readers)
        {
            if (Interlocked.Read(ref readers) < 0)
                throw new Exception("Reader count cannot be negative!");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasReadLock(ref long readLock)
        {
            return Interlocked.Read(ref readLock) > 0;
        }
    }
}