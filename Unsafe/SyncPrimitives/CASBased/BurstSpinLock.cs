//#define MARK_THREAD_OWNERS

#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
#define DEBUG_ADDITIONAL_CHECKS
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IL2CPP.CompilerServices;
using Unity.Logging;

namespace VLib
{
    /// <summary> Implement a very basic, Burst-compatible SpinLock that mirrors the basic .NET SpinLock API. </summary>
    [BurstCompile]
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    public unsafe struct BurstSpinLock
    {
        private VUnsafeBufferedRef<long> m_LockHolder;
        GlobalBurstTimer burstTimerRef;
        
        /// <summary> Checks locked buffer length as well to detect corruption </summary>
        public bool IsCreatedAndValid => m_LockHolder.IsValid && burstTimerRef.IsCreated;

        /// <summary> Constructor for the spin lock </summary>
        /// <param name="allocator">allocator to use for internal memory allocation. Usually should be Allocator.Persistent</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BurstSpinLock(Allocator allocator, GlobalBurstTimer burstTimerRef = default) // Is optional to allow 
        {
            this.burstTimerRef = burstTimerRef;
            m_LockHolder = new VUnsafeBufferedRef<long>(0, allocator);
        }

        /// <summary> Dispose this spin lock. <see cref="IDisposable"/> </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DisposeUnsafe()
        {
            if (IsCreatedAndValid)
            {
                if (!TryEnter(.5f))
                    UnityEngine.Debug.LogError("SpinLock could not be captured for dispose!");
                m_LockHolder.DisposeRefToDefault();
            }
            else
            {
                UnityEngine.Debug.LogError("SpinLock was not created, but you're disposing it!");
            }
        }

        /// <summary> Check lock status without interfering or blocking </summary>
        public bool Locked => Interlocked.Read(ref m_LockHolder.ValueRef) != 0;

        /// <summary> Use the lock in a using statement/block. Implicitly casts to bool for clean checking. </summary>
        public BurstSpinLockScoped Scoped(float timeoutSeconds = 2f) => new BurstSpinLockScoped(this, timeoutSeconds);
        
        [Conditional("DEBUG_ADDITIONAL_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckIfLockCreated()
        {
            if (!IsCreatedAndValid)
                throw new Exception("Lock wasn't created, but you're accessing it");
        }

        /// <summary> Try to lock. Blocking until timeout or acquisition. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEnter(float timeoutSeconds)
        {
            CheckIfLockCreated();

#if MARK_THREAD_OWNERS
            var threadId = Baselib.LowLevel.Binding.Baselib_Thread_GetCurrentThreadId().ToInt64();
            BurstSpinLockCheckFunctions.CheckForRecursiveLock(threadId, ref lockVar);
#else
            var threadId = 1;
#endif
            ref long lockVar = ref m_LockHolder.ValueRef;
            var forceExitTime = burstTimerRef.Time + timeoutSeconds;
            while (Interlocked.CompareExchange(ref lockVar, threadId, 0) != 0)
            {
                if (burstTimerRef.Time > forceExitTime)
                    return false;
                
                Common.Pause();
                    
                // Don't have access for some reason, thanks Unity
                //Baselib.LowLevel.Binding.Baselib_Thread_YieldExecution();
            }

            return true;
        }

        /// <summary> Unlock </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Exit()
        {
            CheckIfLockCreated();

            ref long lockVar = ref m_LockHolder.ValueRef;
            // TODO: Enhance ?
            BurstSpinLockCheckFunctions.CheckWeCanExit(ref lockVar);
            Interlocked.Exchange(ref lockVar, 0);
        }
    }

    [GenerateTestsForBurstCompatibility]
    public struct BurstSpinLockScoped : IDisposable
    {
        BurstSpinLock spinLock;
        /// <summary> Check this, or implicitly cast this struct to 'bool' to check whether the lock acquired successfully! </summary>
        public bool Succeeded { get; }
        public static implicit operator bool(BurstSpinLockScoped scoped) => scoped.Succeeded;

        public BurstSpinLockScoped(BurstSpinLock spinLock, float timeoutSeconds)
        {
            this.spinLock = spinLock;
            Succeeded = spinLock.TryEnter(timeoutSeconds);
        }

        public void Dispose()
        {
            if (Succeeded)
                spinLock.Exit();
        }
    }
}