using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Burst.Intrinsics;
using Unity.IL2CPP.CompilerServices;
using VLib.Systems;

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
        public static bool TryEnterExclusiveBlocking(ref long lockVar, ref long readersVar, float timeoutSeconds)
        {
            var exitLockTime = VTime.intraFrameTime + timeoutSeconds;

            // This used to be set by special unity code to the thread ID under the hood for recursive lock checking, but idk how they're accessing 'BaseLib'
            // If checks are needed, I'll have to figure something out
            var threadId = 1;
            
            // Contend for the exclusive lock, take it if possible
            while (Interlocked.CompareExchange(ref lockVar, threadId, 0) != 0) // If != 0, the lock exclusive lock is held elsewhere and we must wait
            {
                if (VTime.intraFrameTime > exitLockTime)
                    return false; // Timeout
                Common.Pause();
            }
            
            // We have the write lock held at this point, spin against read locks until there are none.
            // while we have readers
            while (Interlocked.Read(ref readersVar) != 0)
            {
                if (VTime.intraFrameTime > exitLockTime)
                {
                    // We timed out, release write lock
                    Interlocked.Exchange(ref lockVar, 0);
                    return false;
                }
                Common.Pause();
            }

            return true;
        }

        /// <summary> Try to lock Exclusive. Won't block </summary>
        /// <returns>True if locked</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEnterExclusive(ref long lockVar, ref long readersVar)
        {
            if (Interlocked.Read(ref readersVar) != 0)
                return false; // Read lock active

#if MARK_THREAD_OWNERS
            var threadId = Baselib.LowLevel.Binding.Baselib_Thread_GetCurrentThreadId().ToInt64();
            BurstSpinLockCheckFunctions.CheckForRecursiveLock(threadId, ref lockVar);
#else
            var threadId = 1;
#endif

            // Take the lock
            if (Interlocked.CompareExchange(ref lockVar, threadId, 0) != 0)
                return false; // If here, someone else is holding the exclusive lock already

            // Check to see if read lock was taken while we were grabbing the exclusive lock
            if (Interlocked.Read(ref readersVar) != 0)
            {
                // Release exclusive lock
                Interlocked.Exchange(ref lockVar, 0);
                // We didn't get the lock
                return false;
            }

            // We acquired the lock
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
        public static bool TryEnterReadBlocking(ref long lockVar, ref long readersVar, float timeoutSeconds)
        {
            BurstSpinLockCheckFunctions.CheckForRecursiveLock(ref lockVar);

            var exitLockTime = VTime.intraFrameTime + timeoutSeconds;
            
            // Loop until we get lock or time out
            while (true)
            {
                // Take read lock
                Interlocked.Increment(ref readersVar);

                // if not exclusive locked, we're all set
                if (Interlocked.Read(ref lockVar) == 0)
                    return true;

                // Exclusive lock is held, release read lock
                Interlocked.Decrement(ref readersVar);

                // while it is locked - spin
                while (Interlocked.Read(ref lockVar) != 0)
                {
                    if (VTime.intraFrameTime > exitLockTime)
                        return false; // Timed out
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

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CheckForNegativeReaders(ref long readers)
        {
            if (Interlocked.Read(ref readers) < 0)
                throw new Exception("Reader count cannot be negative!");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasReadLock(ref long readLock) => Interlocked.Read(ref readLock) > 0;
    }
}