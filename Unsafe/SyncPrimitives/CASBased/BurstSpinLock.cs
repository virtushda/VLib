//#define MARK_THREAD_OWNERS

#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
#define DEBUG_ADDITIONAL_CHECKS
#define CLAIM_TRACKING
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.IL2CPP.CompilerServices;
using Unity.Jobs;
using VLib.Jobs;
using VLib.Systems;
using Debug = UnityEngine.Debug;

namespace VLib
{
    /// <summary> Implement a very basic, Burst-compatible SpinLock that mirrors the basic .NET SpinLock API. </summary>
    [BurstCompile]
    /*[Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]*/
    public struct BurstSpinLock
    {
        public const float DefaultTimeout = 5f;
        
        private RefStruct<long> m_LockHolder;
        
        /// <summary> Checks locked buffer length as well to detect corruption </summary>
        public bool IsCreated => m_LockHolder.IsCreated; // NOTE: If this check is changed to anything other than using the reference holder's 'IsCreated', conditional checks may be needed once again!!!

        /// <summary> Constructor for the spin lock </summary>
        /// <param name="allocator">allocator to use for internal memory allocation. Usually should be Allocator.Persistent</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BurstSpinLock(Allocator allocator
#if CLAIM_TRACKING
            , [CallerLineNumber] int callerLine = -1
#endif
            )
        {
            m_LockHolder = RefStruct<long>.Create(0, allocator
#if CLAIM_TRACKING
                , callerLine
#endif
                );
        }

        /// <summary> Dispose this spin lock. <see cref="IDisposable"/> </summary>
        /// <remarks>
        /// Caller must guarantee external quiescence before calling this method.
        /// There must be no concurrent <see cref="TryEnter"/>, <see cref="Exit"/>, waiting/spinning callers, or jobs/threads that may still touch this lock instance.
        /// Disposing while concurrent entrants/waiters exist is unsafe and results in undefined behavior.
        /// </remarks>
        // TODO: Enhance with rolling read check that attempts to let final entrants exit before disposing.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DisposeUnsafe()
        {
            if (IsCreated)
            {
                if (!TryEnter(DefaultTimeout))
                {
                    Debug.LogError("SpinLock could not be captured for dispose! Memory will NOT be freed to avoid corrupting the holding thread.");
                    return;
                }
                // Caller must enforce quiescence; racing waiters/spinners here is undefined behavior and can dereference invalid memory.
                m_LockHolder.DisposeRefToDefault();
            }
            else
                Debug.LogError("SpinLock was not created, but you're disposing it!");
        }

        /// <summary> Check lock status without interfering or blocking </summary>
        public bool Locked => Interlocked.Read(ref m_LockHolder.ValueRef) != 0;

        /// <summary> Use the lock in a using statement/block. Implicitly casts to bool for clean checking. <br/>
        /// Be warned, timeouts less than maximumDeltaTime may not act correctly with stalls / the-debugger. </summary>
        public BurstSpinLockScoped Scoped(float timeoutSeconds = DefaultTimeout) => new(this, timeoutSeconds);
        
        [Conditional("DEBUG_ADDITIONAL_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckIfLockCreated()
        {
            if (!IsCreated)
                throw new Exception("Lock wasn't created, but you're accessing it");
        }

        /// <summary> Try to lock. Blocking until timeout or acquisition. <br/>
        /// Be warned, timeouts less than maximumDeltaTime may not act correctly with stalls / the-debugger. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEnter(float timeoutSeconds)
        {
            CheckIfLockCreated();
            // This ref aliases lock-owned memory; if DisposeUnsafe races, the reference can become invalid.
            ref long lockVar = ref m_LockHolder.ValueRef;
#if MARK_THREAD_OWNERS
            var threadId = Baselib.LowLevel.Binding.Baselib_Thread_GetCurrentThreadId().ToInt64();
            BurstSpinLockCheckFunctions.CheckForRecursiveLock(threadId, ref lockVar);
#else
            var threadId = int.MaxValue;
#endif
            var forceExitTime = VTime.intraFrameTime + timeoutSeconds;
            while (Interlocked.CompareExchange(ref lockVar, threadId, 0) != 0)
            {
                if (VTime.intraFrameTime > forceExitTime)
                {
                    Debug.LogError("SpinLock timed out");
                    return false;
                }

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
        [MarshalAs(UnmanagedType.U1)] public readonly bool succeeded;
        public static implicit operator bool(BurstSpinLockScoped scoped) => scoped.succeeded;

        public BurstSpinLockScoped(BurstSpinLock spinLock, float timeoutSeconds)
        {
            this.spinLock = spinLock;
            succeeded = spinLock.TryEnter(timeoutSeconds);
        }

        public void Dispose()
        {
            if (succeeded)
                spinLock.Exit();
        }

        public JobHandle GenerateDisposeJob(JobHandle dependencies) => new BurstSpinLockScopedReleaseJob(this).Schedule(dependencies);
    }
}