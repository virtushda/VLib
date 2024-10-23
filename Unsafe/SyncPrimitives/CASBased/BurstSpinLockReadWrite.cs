//#define DEBUG_DEADLOCKS

//#define MARK_THREAD_OWNERS

#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
#define DEBUG_ADDITIONAL_CHECKS
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
    public unsafe struct BurstSpinLockReadWrite
    {
        private const int MemorySize = 16; // * sizeof(long) == 128 byte
        private const int LockLocation = 0;
        private const int ReadersLocation = 8; // * sizeof(long) == 64 byte offset (cache line)

        // Make it a damn pointer so it don't duplicate and fail to recognize a lock!!!
        [NativeDisableUnsafePtrRestriction]
        private VUnsafeBufferedRef<UnsafeList<long>> m_LockHolder;

        UnsafeList<long>* m_Locked => m_LockHolder.TPtr;
        UnsafeList<long>* m_LockedUnsafe => m_LockHolder.TPtrNoSafety;

        /// <param name="allocator">allocator to use for internal memory allocation. Usually should be Allocator.Persistent</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BurstSpinLockReadWrite(Allocator allocator)
        {
            // Create a list, then create a separate double-buffered pointer to it
            var lockBuffer = new UnsafeList<long>(MemorySize, allocator);
            m_LockHolder = new VUnsafeBufferedRef<UnsafeList<long>>(lockBuffer, allocator);

            var lockedCache = m_Locked;
            for (var i = 0; i < MemorySize; i++)
            {
                lockedCache->AddNoResize(0);
            }
        }

        /// <summary> Dispose this spin lock. 'Unsafe' because the caller could now be holding a disposed lock reference, and it needs to be 'default'ed </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DisposeUnsafe()
        {
            if (IsCreatedAndValid)
            {
                if (!EnterExclusive(1))
                    Debug.LogError("Failed to dispose BurstSpinLockReadWrite, it is still locked after 1 second");
                m_Locked->Dispose();
                m_LockHolder.DisposeRefToDefault();
            }
            else
            {
                Debug.LogError("Failed to dispose BurstSpinLockReadWrite, it is not valid");
            }
        }

        public bool LockedExclusive => Interlocked.Read(ref m_Locked->ElementAt(LockLocation)) != 0;
        public bool LockedForRead => Interlocked.Read(ref m_Locked->ElementAt(ReadersLocation)) != 0;
        public bool LockedAny => LockedExclusive || LockedForRead;
        
        /// <summary> Checks locked buffer length as well to detect corruption </summary>
        public bool IsCreatedAndValid => m_LockHolder.IsValid/* && m_Locked->IsCreated && m_Locked->Length == MemorySize && burstTimerRef.IsCreated*/;

        public long Id => (long) m_Locked->Ptr;

        [Conditional("DEBUG_ADDITIONAL_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckIfLockCreated()
        {
            // Check the burst timer as well.. found a situation where the m_locked list thought it was created but it's allocator was invalid and capacity 0...
            if (!IsCreatedAndValid)
                throw new Exception("RWLock wasn't created, but you're accessing it");
        }

        /// <summary> Lock Exclusive. Will block if cannot lock immediately </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EnterExclusive(float timeoutSeconds = .25f)
        {
            CheckIfLockCreated();

            // Safety checked in above conditional method, use safety-less stuff from here on
            // Cache Ptr
            var m_LockedUnsafePtr = m_LockedUnsafe;
            return BurstSpinLockReadWriteFunctions.TryEnterExclusiveBlocking(
                ref m_LockedUnsafePtr->ElementAt(LockLocation),
                ref m_LockedUnsafePtr->ElementAt(ReadersLocation),
                timeoutSeconds);
        }

        /// <summary> Try to lock Exclusive. Won't block </summary>
        /// <returns>True if locked</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEnterExclusive()
        {
            CheckIfLockCreated();

            return BurstSpinLockReadWriteFunctions.TryEnterExclusive(ref m_Locked->ElementAt(LockLocation), ref m_Locked->ElementAt(ReadersLocation));
        }

        /// <summary> Unlock </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExitExclusive()
        {
            CheckIfLockCreated();

            BurstSpinLockReadWriteFunctions.ExitExclusive(ref m_Locked->ElementAt(LockLocation));
        }

        /// <summary>
        /// Lock for Read. Will block if exclusive is locked
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EnterRead(float timeoutSeconds = .25f)
        {
            CheckIfLockCreated();

            // Above check handles safety, use safety-less stuff from here on
            return BurstSpinLockReadWriteFunctions.TryEnterReadBlocking(
                ref m_LockedUnsafe->ElementAt(LockLocation),
                ref m_LockedUnsafe->ElementAt(ReadersLocation),
                timeoutSeconds);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExitRead()
        {
            CheckIfLockCreated();

            // Above check handles safety, use safety-less stuff from here on
            BurstSpinLockReadWriteFunctions.ExitRead(ref m_LockedUnsafe->ElementAt(ReadersLocation));
        }
        
        public BurstScopedExclusiveLock ScopedExclusiveLock(float timeoutSeconds = 1) => new(this, timeoutSeconds);
        
        public BurstScopedReadLock ScopedReadLock(float timeoutSeconds = 1) => new(this, timeoutSeconds);

        public JobHandle StartLockExclusiveJob(float timeoutSeconds = 1, JobHandle inDeps = default) => new LockExclusiveJob(this, timeoutSeconds).Schedule(inDeps);
        
        public JobHandle StartUnlockExclusiveJob(float timeoutSeconds = 1, JobHandle inDeps = default) => new UnlockExclusiveJob(this, timeoutSeconds).Schedule(inDeps);
        
        public JobHandle StartLockReadJob(float timeoutSeconds = 1, JobHandle inDeps = default) => new LockReadJob(this, timeoutSeconds).Schedule(inDeps);
        
        public JobHandle StartUnlockReadJob(float timeoutSeconds = 1, JobHandle inDeps = default) => new UnlockReadJob(this, timeoutSeconds).Schedule(inDeps);

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
            float timeout;
            
            public UnlockExclusiveJob(BurstSpinLockReadWrite theLock, float timeout)
            {
                this.theLock = theLock;
                this.timeout = timeout;
            }

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
            float timeout;
            
            public UnlockReadJob(BurstSpinLockReadWrite theLock, float timeout)
            {
                this.theLock = theLock;
                this.timeout = timeout;
            }

            public void Execute() => theLock.ExitRead();
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void ConditionalCheckReadLockHeld()
        {
            if (!LockedForRead)
                throw new InvalidOperationException("Read lock must be held!");
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void ConditionalCheckWriteLockHeld()
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
        public bool Succeeded { get; }
        public static implicit operator bool(BurstScopedExclusiveLock d) => d.Succeeded;
        
        public bool IsCreated => m_parentLock.IsCreatedAndValid;

        /// <summary> Creates ScopedReadLock and locks SpinLockReadWrite in exclusive mode </summary>
        /// <param name="sl">SpinLock to lock</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BurstScopedExclusiveLock(in BurstSpinLockReadWrite sl, float timeoutSeconds = 1)
        {
            m_parentLock = sl;
            if (!(Succeeded = m_parentLock.EnterExclusive(timeoutSeconds)))
                Debug.LogError("Failed to acquire exclusive lock!");
        }

        /// <summary> Unlocks the lock </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (Succeeded)
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
        public bool Succeeded { get; }
        public static implicit operator bool(BurstScopedReadLock d) => d.Succeeded;

        public bool IsCreated => m_parentLock.IsCreatedAndValid;
        
        /// <summary> Creates ScopedReadLock and locks SpinLockReadWrite in read mode </summary>
        /// <param name="sl">SpinLock to lock</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BurstScopedReadLock(in BurstSpinLockReadWrite sl, float timeoutSeconds = 1)
        {
            m_parentLock = sl;
            if (!(Succeeded = m_parentLock.EnterRead(timeoutSeconds)))
                Debug.LogError("Failed to acquire read lock!");
        }

        /// <summary> Unlocks the lock </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (Succeeded)
                m_parentLock.ExitRead();
        }
    }
}