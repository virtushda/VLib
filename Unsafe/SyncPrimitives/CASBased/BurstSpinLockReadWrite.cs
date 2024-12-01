//#define DEBUG_DEADLOCKS

//#define MARK_THREAD_OWNERS

#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
#define DEBUG_ADDITIONAL_CHECKS
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IL2CPP.CompilerServices;
using Unity.Jobs;
using Debug = UnityEngine.Debug;

namespace VLib
{
    /// <summary>
    /// Implement a very basic, Burst-compatible read-write SpinLock
    /// </summary>
    [BurstCompile]
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    public struct BurstSpinLockReadWrite
    {
        private const int MemorySize = 16; // * sizeof(long) == 128 byte
        private const int LockLocation = 0;
        private const int ReadersLocation = 8; // * sizeof(long) == 64 byte offset (cache line)

        // Make it a damn pointer so it don't duplicate and fail to recognize a lock!!!
        [NativeDisableUnsafePtrRestriction]
        private VUnsafeBufferedRef<UnsafeList<long>> m_LockHolder;

        readonly ref UnsafeList<long> m_Locked => ref m_LockHolder.ValueRef;

        /// <param name="allocator">allocator to use for internal memory allocation. Usually should be Allocator.Persistent</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BurstSpinLockReadWrite(Allocator allocator)
        {
            // Create a list, then create a separate double-buffered pointer to it
            var lockBuffer = new UnsafeList<long>(MemorySize, allocator);
            m_LockHolder = new VUnsafeBufferedRef<UnsafeList<long>>(lockBuffer, allocator);

            ref var lockedCache = ref m_Locked;
            for (var i = 0; i < MemorySize; i++)
                lockedCache.AddNoResize(0);
        }

        /// <summary> Dispose this spin lock. 'Unsafe' because the caller could now be holding a disposed lock reference, and it needs to be 'default'ed </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DisposeUnsafe()
        {
            if (IsCreated)
            {
                if (!EnterExclusive(1))
                    Debug.LogError("Failed to dispose BurstSpinLockReadWrite, it is still locked after 1 second");
                m_Locked.Dispose();
                m_LockHolder.DisposeRefToDefault();
            }
            else
            {
                Debug.LogError("Failed to dispose BurstSpinLockReadWrite, it is not valid");
            }
        }

        public readonly bool LockedExclusive => Interlocked.Read(ref m_Locked.ElementAt(LockLocation)) != 0;
        public readonly bool LockedForRead => Interlocked.Read(ref m_Locked.ElementAt(ReadersLocation)) != 0;
        public readonly bool LockedAny => LockedExclusive || LockedForRead;
        
        /// <summary> Checks locked buffer length as well to detect corruption </summary>
        public readonly bool IsCreated => m_LockHolder.IsCreated;

        public unsafe long Id => (long) m_Locked.Ptr;

        [Conditional("DEBUG_ADDITIONAL_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly void CheckIfLockCreated()
        {
            // Check the burst timer as well.. found a situation where the m_locked list thought it was created but it's allocator was invalid and capacity 0...
            if (!IsCreated)
                throw new Exception("RWLock wasn't created, but you're accessing it");
        }

        /// <summary> Lock Exclusive. Will block if cannot lock immediately. <br/>
        /// Be warned, timeouts less than maximumDeltaTime may not act correctly with stalls / the-debugger. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EnterExclusive(float timeoutSeconds = BurstSpinLock.DefaultTimeout)
        {
            // Cache Ptr
            var m_LockedUnsafePtr = m_Locked;
            return BurstSpinLockReadWriteFunctions.TryEnterExclusiveBlocking(
                ref m_LockedUnsafePtr.ElementAt(LockLocation),
                ref m_LockedUnsafePtr.ElementAt(ReadersLocation),
                timeoutSeconds);
        }

        /// <summary> Try to lock Exclusive. Won't block </summary>
        /// <returns>True if locked</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEnterExclusive()
        {
            ref var lockRef = ref m_Locked;
            return BurstSpinLockReadWriteFunctions.TryEnterExclusive(ref lockRef.ElementAt(LockLocation), ref lockRef.ElementAt(ReadersLocation));
        }

        /// <summary> Unlock </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExitExclusive() => BurstSpinLockReadWriteFunctions.ExitExclusive(ref m_Locked.ElementAt(LockLocation));

        /// <summary> Lock for Read. Will block if exclusive is locked <br/>
        /// Be warned, timeouts less than maximumDeltaTime may not act correctly with stalls / the-debugger. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EnterRead(float timeoutSeconds = BurstSpinLock.DefaultTimeout)
        {
            ref var lockRef = ref m_Locked;
            return BurstSpinLockReadWriteFunctions.TryEnterReadBlocking(
                ref lockRef.ElementAt(LockLocation),
                ref lockRef.ElementAt(ReadersLocation),
                timeoutSeconds);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExitRead() => BurstSpinLockReadWriteFunctions.ExitRead(ref m_Locked.ElementAt(ReadersLocation));

        /// <summary> Lock and return an IDisposable struct. <br/>
        /// Be warned, timeouts less than maximumDeltaTime may not act correctly with stalls / the-debugger. </summary>
        public BurstScopedExclusiveLock ScopedExclusiveLock(float timeoutSeconds = BurstSpinLock.DefaultTimeout) => new(this, timeoutSeconds);
        
        /// <summary> Lock and return an IDisposable struct. <br/>
        /// Be warned, timeouts less than maximumDeltaTime may not act correctly with stalls / the-debugger. </summary>
        public BurstScopedReadLock ScopedReadLock(float timeoutSeconds = BurstSpinLock.DefaultTimeout) => new(this, timeoutSeconds);

        public JobHandle StartLockExclusiveJob(float timeoutSeconds = BurstSpinLock.DefaultTimeout, JobHandle inDeps = default) => new LockExclusiveJob(this, timeoutSeconds).Schedule(inDeps);
        
        public JobHandle StartUnlockExclusiveJob(JobHandle inDeps = default) => new UnlockExclusiveJob(this).Schedule(inDeps);
        
        public JobHandle StartLockReadJob(float timeoutSeconds = BurstSpinLock.DefaultTimeout, JobHandle inDeps = default) => new LockReadJob(this, timeoutSeconds).Schedule(inDeps);
        
        public JobHandle StartUnlockReadJob(JobHandle inDeps = default) => new UnlockReadJob(this).Schedule(inDeps);

        [BurstCompile]
        public struct LockExclusiveJob : IJob
        {
            BurstSpinLockReadWrite theLock;
            float timeout;
            
            public LockExclusiveJob(BurstSpinLockReadWrite theLock, float timeout)
            {
                this.theLock = theLock;
                this.timeout = timeout;
            }

            public void Execute() => theLock.EnterExclusive(timeout);
        }

        [BurstCompile]
        public struct UnlockExclusiveJob : IJob
        {
            BurstSpinLockReadWrite theLock;
            
            public UnlockExclusiveJob(BurstSpinLockReadWrite theLock) => this.theLock = theLock;

            public void Execute() => theLock.ExitExclusive();
        }

        [BurstCompile]
        public struct LockReadJob : IJob
        {
            BurstSpinLockReadWrite theLock;
            float timeout;
            
            public LockReadJob(BurstSpinLockReadWrite theLock, float timeout)
            {
                this.theLock = theLock;
                this.timeout = timeout;
            }

            public void Execute() => theLock.EnterRead(timeout);
        }

        [BurstCompile]
        public struct UnlockReadJob : IJob
        {
            BurstSpinLockReadWrite theLock;
            
            public UnlockReadJob(BurstSpinLockReadWrite theLock) => this.theLock = theLock;

            public void Execute() => theLock.ExitRead();
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public readonly void ConditionalCheckReadLockHeld()
        {
            if (!LockedForRead)
                throw new InvalidOperationException("Read lock must be held!");
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public readonly void ConditionalCheckWriteLockHeld()
        {
            if (!LockedExclusive)
                throw new InvalidOperationException("Write lock must be held!");
        }
    }

    /// <summary> IDisposable scoped structure that holds <see cref="BurstSpinLockReadWrite"/> in exclusive mode. Should be using with <c>using</c> </summary>
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    public struct BurstScopedExclusiveLock : IAllocating
    {
        private BurstSpinLockReadWrite m_parentLock;
        /// <summary> Check this, or implicitly cast this struct to 'bool' to check whether the lock acquired successfully! </summary>
        [MarshalAs(UnmanagedType.U1)] public readonly bool succeeded;
        public static implicit operator bool(BurstScopedExclusiveLock d) => d.succeeded;
        
        public bool IsCreated => m_parentLock.IsCreated;

        /// <summary> Creates ScopedReadLock and locks SpinLockReadWrite in exclusive mode </summary>
        /// <param name="sl">SpinLock to lock</param>
        /// <param name="timeoutSeconds">Timeout in seconds</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BurstScopedExclusiveLock(in BurstSpinLockReadWrite sl, float timeoutSeconds = BurstSpinLock.DefaultTimeout)
        {
            m_parentLock = sl;
            if (!(succeeded = m_parentLock.EnterExclusive(timeoutSeconds)))
                Debug.LogError("Failed to acquire exclusive lock!");
        }

        /// <summary> Unlocks the lock </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (succeeded)
                m_parentLock.ExitExclusive();
        }
    }

    /// <summary> IDisposable scoped structure that holds <see cref="BurstSpinLockReadWrite"/> in read mode. Should be using with <c>using</c> </summary>
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    public struct BurstScopedReadLock : IAllocating
    {
        private BurstSpinLockReadWrite m_parentLock;

        /// <summary> Check this, or implicitly cast this struct to 'bool' to check whether the lock acquired successfully! </summary>
        [MarshalAs(UnmanagedType.U1)] public readonly bool succeeded;
        public static implicit operator bool(BurstScopedReadLock d) => d.succeeded;

        public bool IsCreated => m_parentLock.IsCreated;

        /// <summary> Creates ScopedReadLock and locks SpinLockReadWrite in read mode </summary>
        /// <param name="sl">SpinLock to lock</param>
        /// <param name="timeoutSeconds">Timeout in seconds</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BurstScopedReadLock(in BurstSpinLockReadWrite sl, float timeoutSeconds = BurstSpinLock.DefaultTimeout)
        {
            m_parentLock = sl;
            if (!(succeeded = m_parentLock.EnterRead(timeoutSeconds)))
                Debug.LogError("Failed to acquire read lock!");
        }

        /// <summary> Unlocks the lock </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (succeeded)
                m_parentLock.ExitRead();
        }
    }
}