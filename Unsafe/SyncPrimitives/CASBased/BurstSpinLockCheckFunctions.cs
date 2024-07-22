using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.IL2CPP.CompilerServices;

namespace VLib
{
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    public static class BurstSpinLockCheckFunctions
    {
        [Conditional("DEBUG_ADDITIONAL_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckForRecursiveLock(in long threadId, ref long lockVar)
        {
#if MARK_THREAD_OWNERS
            var currentOwnerThreadId = Interlocked.Read(ref lockVar);

            if (threadId == currentOwnerThreadId)
            {
                UnityEngine.Debug.LogError(string.Format("Recursive lock! Thread {0}", threadId));
                throw new Exception($"Recursive lock! Thread {threadId}");
            }
#endif
        }

        [Conditional("DEBUG_ADDITIONAL_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckForRecursiveLock(ref long lockVar)
        {
#if MARK_THREAD_OWNERS
            var threadId = Baselib.LowLevel.Binding.Baselib_Thread_GetCurrentThreadId().ToInt64();
            CheckForRecursiveLock(threadId, ref lockVar);
#endif
        }

        [Conditional("DEBUG_ADDITIONAL_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckWeCanExit(ref long lockVar)
        {
            var currentOwnerThreadId = Interlocked.Read(ref lockVar);
            if (currentOwnerThreadId == 0)
                UnityEngine.Debug.LogError("Exit is called on not locked lock");
            //throw new Exception("Exit is called on not locked lock"); // No silly, just log

#if MARK_THREAD_OWNERS
            var threadId = Baselib.LowLevel.Binding.Baselib_Thread_GetCurrentThreadId().ToInt64();

            if (threadId != currentOwnerThreadId)
            {
                UnityEngine.Debug.LogError(string.Format("Exit is called from the other ({0}) thread, owner thread = {1}", threadId, currentOwnerThreadId));
                throw new Exception($"Exit is called from the other ({threadId}) thread, owner thread = {currentOwnerThreadId}");
            }
#endif
        }

        [Conditional("DEBUG_ADDITIONAL_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckLocked(ref long lockVar)
        {
            if (Interlocked.Read(ref lockVar) == 0)
                throw new Exception("Exit is called on not locked lock");
        }
    }
}