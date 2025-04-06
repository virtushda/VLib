#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
#define DEBUG_ADDITIONAL_CHECKS
//#define DEADLOCK_DEBUG // Reports additional information during lock timeouts. Enables a mechanism to identify who is holding the exclusive lock.
//#define RECURSIVE_READ_DEBUG // IF ORDERED, it is not safe to recursively read lock within one thread, this causes a circular deadlock with another thread's write lock.
#endif

#if ENABLE_PROFILER
//#define PROFILE_SPINS
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IL2CPP.CompilerServices;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Profiling;
using VLib.Systems;
using VLib.Threading;
using Debug = UnityEngine.Debug;

namespace VLib
{
    /// <summary>
    /// A read-write spinlock that is fully burst compatible. <br/>
    /// Timeouts are supported to be able to detect and protect against deadlocks. <br/>
    ///
    /// This lock type is not safely reentrant. <br/>
    /// You can technically take the read lock recursively, but there exists a dangerous deadlock case: <br/>
    /// If you enter a read lock, then another thread enters the write lock, they will contend as normal.
    /// But if the read lock thread then tries to enter the read lock again, the second readlock will contend with the write lock, while the write lock is still contending with the first read lock.
    /// </summary>
    [BurstCompile]
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    public struct BurstSpinLockReadWrite
    {
        /// <summary> Exclusive lock location in the list of longs </summary>
        const int LockLocation = 0;
        /// <summary> Index in a list of longs that holds the read lock count. </summary>
        const int ReadersLocation = JobsUtility.CacheLineSize / sizeof(long); // 64 byte offset (cache line)
        /// <summary> Size of the memory allocated for the lock to avoid false sharing. </summary>
        const int MemorySize = JobsUtility.CacheLineSize * 2 / sizeof(long); // 128 byte
        
#if PROFILE_SPINS
        public static readonly ProfilerMarker SpinProfilerMarker_ReadAgainstWrite = new("Spin|ReadAgainstWrite");
        public static readonly ProfilerMarker SpinProfilerMarker_WriteAgainstWrite = new("Spin|WriteAgainstWrite");
        public static readonly ProfilerMarker SpinProfilerMarker_WriteAgainstRead = new("Spin|WriteAgainstRead");
#endif

        // Copy-safe, memory corruption resistant, protects against uninitialized memory
        [NativeDisableUnsafePtrRestriction] RefStruct<Data> m_LockHolder;

        public struct Data
        {
            public UnsafeList<long> m_LockHolder;

            /// <summary> Whether requests to lock maintain a strict order. If true, recursive read locks are inherently unsafe. <br/>
            /// ThreadA-Reader -> ThreadB-Writer -> ThreadA-Reader creates a circular deadlock. </summary>
            public bool ordered;
            
            public ref long ExclusiveLockValue => ref m_LockHolder.ElementAt(LockLocation);
            public ref long ReadersLockValue => ref m_LockHolder.ElementAt(ReadersLocation);
            
#if DEADLOCK_DEBUG
            /// <summary> Supply unique information (perhaps with an enum), then you'll be able to identify who is holding the exclusive lock, even in burst. </summary>
            public int WriteLockID { get; private set; }
            /// <summary> Used to mark a lock as held, but by the locking mechanism in the process of fully acquiring the lock. </summary>
            public bool TempHold { get; private set; }
            
            public void DeadlockDebug_SetWriteLockID(int id, bool holdIsTemp)
            {
                WriteLockID = id;
                TempHold = holdIsTemp;
            }
#endif
            
#if RECURSIVE_READ_DEBUG
            BurstSpinLock recursiveReadLock;
            UnsafeHashSet<VThreadID> recursiveReadLockThreadIDs;
            UnsafeHashMap<ulong, VThreadID> recursiveReadLockThreadIDMap;
#endif
            
            [Conditional("RECURSIVE_READ_DEBUG")]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void InitRecursiveReadLock(Allocator allocator)
            {
#if RECURSIVE_READ_DEBUG
                BurstAssert.False(recursiveReadLock.IsCreated);
                recursiveReadLock = new BurstSpinLock(allocator);
                recursiveReadLockThreadIDs = new (1, allocator);
                recursiveReadLockThreadIDMap = new (1, allocator);
#endif
            }
            
            [Conditional("RECURSIVE_READ_DEBUG")]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void DisposeRecursiveReadLockDebug()
            {
#if RECURSIVE_READ_DEBUG
                recursiveReadLock.DisposeRefToDefault();
                if (recursiveReadLockThreadIDs.IsCreated) 
                    recursiveReadLockThreadIDs.Dispose();
                if (recursiveReadLockThreadIDMap.IsCreated) 
                    recursiveReadLockThreadIDMap.Dispose();
#endif
            }

            /*[Conditional("RECURSIVE_READ_DEBUG")]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void CheckForRecursiveReadLock()
            {
#if RECURSIVE_READ_DEBUG
                using var scopedLock = recursiveReadLock.Scoped();
                if (!scopedLock)
                {
                    Debug.LogError("Failed to acquire recursive read lock!");
                    return;
                }

                if (!VThreadUtil.TryGetCurrentThreadID(out var threadID))
                {
                    Debug.LogError("Failed to get thread ID! (Disabling burst should resolve this)");
                    return;
                }

                // Check if the thread ID is already in the list
                if (recursiveReadLockThreadIDs.Contains(threadID))
                    Debug.LogError($"Recursive read lock detected! Thread ID: {threadID}");
#endif
            }*/

            [Conditional("RECURSIVE_READ_DEBUG")]
            public void AddRecursiveReadLockDebugThreadID(ulong lockInstanceID)
            {
#if RECURSIVE_READ_DEBUG
                // If lock is orderless, recursive reads are allowed
                if (!ordered)
                    return;
                
                using var scopedLock = recursiveReadLock.Scoped();
                if (!scopedLock)
                {
                    Debug.LogError("Failed to acquire recursive read lock!");
                    return;
                }
                if (!VThreadUtil.TryGetCurrentThreadID(out var threadID))
                {
                    Debug.LogError("Failed to get thread ID! (Disabling burst should resolve this)");
                    return;
                }
                if (lockInstanceID > 0)
                    if (!recursiveReadLockThreadIDMap.TryAdd(lockInstanceID, threadID))
                        Debug.LogError($"Failed to add recursive read lock to threadIDMap while on {threadID}");
                if (!recursiveReadLockThreadIDs.Add(threadID))
                    Debug.LogError($"Recursive read detected on {threadID}");
#endif
            }

            [Conditional("RECURSIVE_READ_DEBUG")]
            public void RemoveRecursiveReadLockThreadID(ulong lockInstanceID)
            {
#if RECURSIVE_READ_DEBUG
                // If lock is orderless, recursive reads are allowed
                if (!ordered)
                    return;
                
                using var scopedLock = recursiveReadLock.Scoped();
                if (!scopedLock)
                {
                    Debug.LogError("Failed to acquire recursive read lock!");
                    return;
                }
                if (!VThreadUtil.TryGetCurrentThreadID(out var threadID))
                {
                    Debug.LogError("Failed to get thread ID! (Disabling burst should resolve this)");
                    return;
                }
                
                if (recursiveReadLockThreadIDMap.TryGetValue(lockInstanceID, out var threadIDInitiallyLockedOn))
                {
                    // Ensure we remove the thread ID from the thread we LOCKED on, not the thread we're unlocking on
                    // Scoped structs support being copied between threads.
                    recursiveReadLockThreadIDMap.Remove(lockInstanceID);
                    if (!recursiveReadLockThreadIDs.Remove(threadIDInitiallyLockedOn))
                        Debug.LogError($"Failed to remove recursive read lock thread ID {threadIDInitiallyLockedOn}");
                }
                else
                {
                    if (!recursiveReadLockThreadIDs.Remove(threadID))
                        Debug.LogError($"Failed to remove recursive read lock thread ID {threadID}");
                }
#endif
            }
        }
        
        readonly ref Data InternalData => ref m_LockHolder.ValueRef;
        readonly ref UnsafeList<long> m_Locked => ref InternalData.m_LockHolder;

        /// <param name="allocator">allocator to use for internal memory allocation. Usually should be Allocator.Persistent</param>
        /// <param name="isOrdered"> <para> Whether all requests to the lock maintain a strict order. </para>
        /// <para> If true: Recursive read locks are inherently unsafe because: [ThreadA-Reader -> ThreadB-Writer -> ThreadA-Reader] creates a circular deadlock. </para>
        /// <para> If false: Recursive read locks are allowed, but write lock attempts will only block new readlocks for very short periods of time.
        /// This allows recursive reads to push through, and the write lock can come back around to try again. </para>
        /// <para> Orderless locks are ideal for complex situations where you need to protect a resource, and access order is not strictly important. </para> </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BurstSpinLockReadWrite(Allocator allocator, bool isOrdered = true)
        {
            // Create a list, then create a separate double-buffered pointer to it
            var lockBuffer = new Data {m_LockHolder = new UnsafeList<long>(MemorySize, allocator), ordered = isOrdered};
            lockBuffer.InitRecursiveReadLock(allocator);
            m_LockHolder = RefStruct<Data>.Create(lockBuffer, allocator);

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
                if (!EnterExclusive())
                    Debug.LogError("Failed to dispose BurstSpinLockReadWrite, it is still locked after 5 seconds.");
                m_Locked.DisposeRefToDefault();
                InternalData.DisposeRecursiveReadLockDebug();
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
        
        public readonly bool IsCreated => m_LockHolder.IsCreated;

        /// <summary> A unique lock ID. </summary>
        public unsafe long Id => (long) m_Locked.Ptr;

        /// <summary> Lock Exclusive. Will block if cannot lock immediately. <br/>
        /// Be warned, timeouts less than maximumDeltaTime may not act correctly with stalls / the-debugger. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EnterExclusive(float timeoutSeconds = BurstSpinLock.DefaultTimeout
#if DEADLOCK_DEBUG
            , [CallerLineNumber] int writeLockID = 0
#endif
        )
        {
            ConditionalCheckLockCreated();
            ref var internalData = ref InternalData;
            
            if (internalData.ordered)
            {
                return BurstSpinLockReadWriteFunctions.TryEnterExclusiveBlocking(ref InternalData, timeoutSeconds
#if DEADLOCK_DEBUG
                , writeLockID
#endif
                );
            }
            
            return BurstSpinLockReadWriteFunctions.TryEnterExclusiveBlocking_Orderless(ref InternalData, timeoutSeconds
#if DEADLOCK_DEBUG
                , writeLockID
#endif
            );

            // Cannot assert here as the TryEnterRead will temporarily take a read lock before checking if the exclusive lock is set
            //BurstAssert.True(!locked || !LockedForRead); // If we got a write lock, there should be no read locks
            //return locked;
        }

        /// <summary> Unlock </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExitExclusive()
        {
            ConditionalCheckLockCreated();
            BurstSpinLockReadWriteFunctions.ExitExclusive(ref InternalData);
        }

        /// <summary> Lock for Read. Will block if exclusive is locked <br/>
        /// Be warned, timeouts less than maximumDeltaTime may not act correctly with stalls / the-debugger. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EnterRead(float timeoutSeconds = BurstSpinLock.DefaultTimeout
#if RECURSIVE_READ_DEBUG
            , ulong readLockID = 0
#endif
        )
        {
            ConditionalCheckLockCreated();
            return BurstSpinLockReadWriteFunctions.TryEnterReadBlocking(ref InternalData, timeoutSeconds
#if RECURSIVE_READ_DEBUG
                , readLockID
#endif
            );
            
            // Cannot assert this, as exclusive lock can be active while TryEnterExclusiveBlocking is running in order t
            //BurstAssert.True(!locked || !LockedExclusive); // If we got a read lock, there should be no exclusive locks
            //return locked;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExitRead(
#if RECURSIVE_READ_DEBUG
            ulong readLockID = 0
#endif
        )
        {
            ConditionalCheckLockCreated();
            BurstSpinLockReadWriteFunctions.ExitRead(ref InternalData
#if RECURSIVE_READ_DEBUG
                , readLockID
#endif
            );
        }

        /// <summary> Lock and return an IDisposable struct. <br/>
        /// Be warned, timeouts less than maximumDeltaTime may not act correctly with stalls / the-debugger. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly BurstScopedExclusiveLock ScopedExclusiveLock(float timeoutSeconds = BurstSpinLock.DefaultTimeout
#if DEADLOCK_DEBUG
            , [CallerLineNumber] int writeLockID = 0
#endif
        )
        {
            return new BurstScopedExclusiveLock(this, timeoutSeconds
#if DEADLOCK_DEBUG
                , writeLockID
#endif
            );
        }

        /// <summary> <inheritdoc cref="ScopedExclusiveLock"/> <br/>
        /// This version lets you inject debug without having to use #if blocks everywhere. Slightly slower. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly BurstScopedExclusiveLock ScopedExclusiveLockDebug(float timeoutSeconds = BurstSpinLock.DefaultTimeout, int writeLockID = 0)
        {
            return ScopedExclusiveLock(timeoutSeconds
#if DEADLOCK_DEBUG
                , writeLockID
#endif
            );
        }

        /// <summary> Lock and return an IDisposable struct. <br/>
        /// Be warned, timeouts less than maximumDeltaTime may not act correctly with stalls / the-debugger. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly BurstScopedReadLock ScopedReadLock(float timeoutSeconds = BurstSpinLock.DefaultTimeout) => new(this, timeoutSeconds);

        public JobHandle StartLockExclusiveJob(float timeoutSeconds = BurstSpinLock.DefaultTimeout, JobHandle inDeps = default
#if DEADLOCK_DEBUG
            , [CallerLineNumber] int writeLockID = 0
#endif
        )
        {
            ConditionalCheckLockCreated();
            return new LockExclusiveJob(this, timeoutSeconds
#if DEADLOCK_DEBUG
                , writeLockID
#endif
            ).Schedule(inDeps);
        }

        public JobHandle StartUnlockExclusiveJob(JobHandle inDeps = default)
        {
            ConditionalCheckLockCreated();
            return new UnlockExclusiveJob(this).Schedule(inDeps);
        }

        public JobHandle StartLockReadJob(float timeoutSeconds = BurstSpinLock.DefaultTimeout, JobHandle inDeps = default)
        {
            ConditionalCheckLockCreated();
            return new LockReadJob(this, timeoutSeconds).Schedule(inDeps);
        }

        public JobHandle StartUnlockReadJob(JobHandle inDeps = default)
        {
            ConditionalCheckLockCreated();
            return new UnlockReadJob(this).Schedule(inDeps);
        }

        [BurstCompile]
        struct LockExclusiveJob : IJob
        {
            BurstSpinLockReadWrite theLock;
            float timeout;
#if DEADLOCK_DEBUG
            int writeLockID;
#endif
            
            public LockExclusiveJob(BurstSpinLockReadWrite theLock, float timeout
#if DEADLOCK_DEBUG
                , int writeLockID = 0
#endif
            )
            {
                this.theLock = theLock;
                this.timeout = timeout;
#if DEADLOCK_DEBUG
                this.writeLockID = writeLockID;
#endif
            }

            public void Execute()
            {
                theLock.EnterExclusive(timeout
#if DEADLOCK_DEBUG
                , writeLockID
#endif
                );
            }
        }

        [BurstCompile]
        struct UnlockExclusiveJob : IJob
        {
            BurstSpinLockReadWrite theLock;
            
            public UnlockExclusiveJob(BurstSpinLockReadWrite theLock) => this.theLock = theLock;

            public void Execute() => theLock.ExitExclusive();
        }

        [BurstCompile]
        struct LockReadJob : IJob
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
        struct UnlockReadJob : IJob
        {
            BurstSpinLockReadWrite theLock;
            
            public UnlockReadJob(BurstSpinLockReadWrite theLock) => this.theLock = theLock;

            public void Execute() => theLock.ExitRead();
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public readonly void ConditionalCheckLockCreated()
        {
            if (!IsCreated)
                throw new InvalidOperationException("Lock is not created!");
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
        BurstSpinLockReadWrite m_parentLock;
        /// <summary> Check this, or implicitly cast this struct to 'bool' to check whether the lock acquired successfully! </summary>
        [MarshalAs(UnmanagedType.U1)] public readonly bool succeeded;
        public static implicit operator bool(BurstScopedExclusiveLock d) => d.succeeded;
        
#if RECURSIVE_READ_DEBUG
        VSafetyHandle safetyHandle;
#endif
        
        /// <summary> Whether the internal lock copy is created </summary>
        public bool IsCreated => m_parentLock.IsCreated;

        /// <summary> Creates ScopedReadLock and locks SpinLockReadWrite in exclusive mode </summary>
        /// <param name="spinlock">SpinLock to lock</param>
        /// <param name="timeoutSeconds">Timeout in seconds</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BurstScopedExclusiveLock(in BurstSpinLockReadWrite spinlock, float timeoutSeconds = BurstSpinLock.DefaultTimeout
#if DEADLOCK_DEBUG
            , [CallerLineNumber] int writeLockID = 0
#endif
        )
        {
            spinlock.ConditionalCheckLockCreated();
            m_parentLock = spinlock;
            if (!(succeeded = m_parentLock.EnterExclusive(timeoutSeconds
#if DEADLOCK_DEBUG
                , writeLockID
#endif
            )))
            {
                Debug.LogError("Failed to acquire exclusive lock!");
                this = default; // Wipe the struct
                return;
            }
            
            // Success
#if RECURSIVE_READ_DEBUG
            safetyHandle = VSafetyHandle.Create();
#endif
        }

        /// <summary> Unlocks the lock </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (succeeded)
            {
#if RECURSIVE_READ_DEBUG
                safetyHandle.ConditionalCheckValid();
                safetyHandle.Dispose();
#endif
                m_parentLock.ExitExclusive();
            }
#if RECURSIVE_READ_DEBUG
            safetyHandle.ConditionalCheckNotValid();
            safetyHandle.Dispose();
#endif
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void ConditionalCheckLockHeld()
        {
            if (!succeeded)
                Debug.LogError("Write lock must be held!");
        }
    }

    /// <summary> IDisposable scoped structure that holds <see cref="BurstSpinLockReadWrite"/> in read mode. Should be using with <c>using</c> </summary>
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    public struct BurstScopedReadLock : IAllocating
    {
        BurstSpinLockReadWrite m_parentLock;

        /// <summary> Check this, or implicitly cast this struct to 'bool' to check whether the lock acquired successfully! </summary>
        [MarshalAs(UnmanagedType.U1)] public readonly bool succeeded;
        public static implicit operator bool(BurstScopedReadLock d) => d.succeeded;

#if RECURSIVE_READ_DEBUG
        VSafetyHandle safetyHandle;
#endif

        /// <summary> Whether the internal lock copy is created </summary>
        public bool IsCreated => m_parentLock.IsCreated;

        /// <summary> Creates ScopedReadLock and locks SpinLockReadWrite in read mode </summary>
        /// <param name="spinLock">SpinLock to lock</param>
        /// <param name="timeoutSeconds">Timeout in seconds</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BurstScopedReadLock(in BurstSpinLockReadWrite spinLock, float timeoutSeconds = BurstSpinLock.DefaultTimeout)
        {
            spinLock.ConditionalCheckLockCreated();
#if RECURSIVE_READ_DEBUG
            safetyHandle = VSafetyHandle.Create();
#endif
            m_parentLock = spinLock;
            if (!(succeeded = m_parentLock.EnterRead(timeoutSeconds
#if RECURSIVE_READ_DEBUG
                , safetyHandle.safetyIDCopy
#endif
            )))
            {
                Debug.LogError("Failed to acquire read lock!");
#if RECURSIVE_READ_DEBUG
                safetyHandle.Dispose();
#endif
                this = default; // Wipe the struct
                return;
            }
            // Success
        }

        /// <summary> Unlocks the lock </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (succeeded)
            {
                m_parentLock.ExitRead(
#if RECURSIVE_READ_DEBUG
                    safetyHandle.safetyIDCopy
#endif
                );
                
#if RECURSIVE_READ_DEBUG
                safetyHandle.ConditionalCheckValid();
                safetyHandle.Dispose();
#endif
            }
#if RECURSIVE_READ_DEBUG
            safetyHandle.ConditionalCheckNotValid();
            safetyHandle.Dispose();
#endif
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void ConditionalCheckLockHeld()
        {
            if (!succeeded)
                Debug.LogError("Read lock must be held!");
        }
    }
    
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    public static class BurstSpinLockReadWriteFunctions
    {
        /// <summary> Lock Exclusive. Will block if cannot lock immediately </summary>
        /// <returns>True if lock acquired, false if not </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEnterExclusiveBlocking(ref BurstSpinLockReadWrite.Data lockData, float timeoutSeconds
#if DEADLOCK_DEBUG
            , int writeLockID
#endif
        )
        {
            ref long exclusiveVar = ref lockData.ExclusiveLockValue;
            ref long readersVar = ref lockData.ReadersLockValue;
            
            var exitLockTime = VTime.intraFrameTime + timeoutSeconds;

            // This used to be set by special unity code to the thread ID under the hood for recursive lock checking, but idk how they're accessing 'BaseLib'
            // If checks are needed, I'll have to figure something out
            var threadId = 1;
            
            // Contend for the exclusive lock, take it if possible
            while (Interlocked.CompareExchange(ref exclusiveVar, threadId, 0) != 0) // If != 0, the lock exclusive lock is held elsewhere and we must wait
            {
#if PROFILE_SPINS
                using var profileScope = BurstSpinLockReadWrite.SpinProfilerMarker_WriteAgainstWrite.Auto();
#endif
                if (VTime.intraFrameTime > exitLockTime)
                {
#if DEADLOCK_DEBUG
                    Debug.LogError($"Failed to get exclusive lock, exclusive lock is already held [Holder ID: {lockData.WriteLockID}, TempHold: {lockData.TempHold}]");
#endif
                    return false; // Timeout
                }

                Common.Pause();
            }
            
#if DEADLOCK_DEBUG
            // Mark hold
            lockData.DeadlockDebug_SetWriteLockID(writeLockID, true);
#endif
            
            // We have the write lock held at this point, spin against read locks until there are none.
            // while we have readers
            while (Interlocked.Read(ref readersVar) != 0)
            {
#if PROFILE_SPINS
                using var profileScope = BurstSpinLockReadWrite.SpinProfilerMarker_WriteAgainstRead.Auto();
#endif
                if (VTime.intraFrameTime > exitLockTime)
                {
#if DEADLOCK_DEBUG
                    // Lock should be held at this line, clear the ID safely
                    lockData.DeadlockDebug_SetWriteLockID(0, false);
#endif
                    // We timed out, release write lock
                    Interlocked.Exchange(ref exclusiveVar, 0);
#if DEADLOCK_DEBUG
                    Debug.LogError($"Failed to get exclusive lock, read lock is held by {readersVar} readers");
#endif
                    return false;
                }
                Common.Pause();
            }

            // We have the lock
#if DEADLOCK_DEBUG
            // Mark hold
            lockData.DeadlockDebug_SetWriteLockID(writeLockID, false);
#endif
            return true;
        }
        
        /// <summary> Lock Exclusive. Will block if cannot lock immediately. <br/>
        /// Orderless: Will only block new readers for a very short time. This allows for recursive read locks to avoid circular deadlocks. </summary>
        /// <returns> True if lock acquired, false if not. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEnterExclusiveBlocking_Orderless(ref BurstSpinLockReadWrite.Data lockData, float timeoutSeconds
#if DEADLOCK_DEBUG
            , int writeLockID
#endif
        )
        {
            ref long exclusiveVar = ref lockData.ExclusiveLockValue;
            ref long readersVar = ref lockData.ReadersLockValue;
            
            var exitLockTime = VTime.intraFrameTime + timeoutSeconds;

            // This used to be set by special unity code to the thread ID under the hood for recursive lock checking, but idk how they're accessing 'BaseLib'
            // If checks are needed, I'll have to figure something out
            var threadId = 1;

            while (true)
            {
                // Contend for the exclusive lock, take it if possible
                while (Interlocked.CompareExchange(ref exclusiveVar, threadId, 0) != 0) // If != 0, the lock exclusive lock is held elsewhere and we must wait
                {
#if PROFILE_SPINS
                    using var profileScope = BurstSpinLockReadWrite.SpinProfilerMarker_WriteAgainstWrite.Auto();
#endif
                    if (VTime.intraFrameTime > exitLockTime)
                    {
#if DEADLOCK_DEBUG
                        Debug.LogError($"Failed to get exclusive lock, exclusive lock is already held [Holder ID: {lockData.WriteLockID}, TempHold: {lockData.TempHold}]");
#endif
                        return false; // Timeout
                    }

                    Common.Pause();
                }
            
#if DEADLOCK_DEBUG
                // Mark hold
                lockData.DeadlockDebug_SetWriteLockID(writeLockID, true);
#endif
            
                // We have the write lock held at this point, spin against read locks.
                int readSpinBudget = 128;
                while (--readSpinBudget >= 0)
                {
#if PROFILE_SPINS
                    using var profileScope = BurstSpinLockReadWrite.SpinProfilerMarker_WriteAgainstRead.Auto();
#endif
                    // Check for no readers
                    if (Interlocked.Read(ref readersVar) == 0)
                    {
                        // Success, we have the lock
#if DEADLOCK_DEBUG
                        // Mark hold
                        lockData.DeadlockDebug_SetWriteLockID(writeLockID, false);
#endif
                        return true;
                    }

                    // At least one reader active, check for timeout and keep spinning
                    
                    if (VTime.intraFrameTime > exitLockTime)
                    {
#if DEADLOCK_DEBUG
                        // Lock should be held at this line, clear the ID safely
                        lockData.DeadlockDebug_SetWriteLockID(0, false);
#endif
                        // We timed out, release write lock
                        Interlocked.Exchange(ref exclusiveVar, 0);
#if DEADLOCK_DEBUG
                        Debug.LogError($"Failed to get exclusive lock (in orderless mode!), read lock is held by {readersVar} readers. \n Orderless mode write locks may fail if many overlapping read locks are taking without gaps for a considerable amount of time. (the timeout!)");
#endif
                        return false;
                    }

                    Common.Pause();
                }
                
                // Readers are still blocking, release the exclusive lock and try again
#if DEADLOCK_DEBUG
                lockData.DeadlockDebug_SetWriteLockID(0, false);
#endif
                Interlocked.Exchange(ref exclusiveVar, 0);
                Common.Pause();
            }
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

        /// <summary> Unlock </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExitExclusive(ref BurstSpinLockReadWrite.Data lockData)
        {
            BurstSpinLockCheckFunctions.CheckWeCanExit(ref lockData.ExclusiveLockValue);
#if DEADLOCK_DEBUG
            // Lock should be held at this line, clear the ID safely
            lockData.DeadlockDebug_SetWriteLockID(0, false);
#endif
            Interlocked.Exchange(ref lockData.ExclusiveLockValue, 0);
        }

        /// <summary> Lock for Read. Will block if exclusive is locked. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEnterReadBlocking(ref BurstSpinLockReadWrite.Data lockData, float timeoutSeconds
#if RECURSIVE_READ_DEBUG
            , ulong readLockID
#endif
        )
        {
            ref long lockVar = ref lockData.ExclusiveLockValue;
            ref long readersVar = ref lockData.ReadersLockValue;
            
            // Loop until we get lock or time out
            while (true)
            {
                // Take read lock
                Interlocked.Increment(ref readersVar);

                // if not exclusive locked, we're all set
                if (Interlocked.Read(ref lockVar) == 0)
                {
#if RECURSIVE_READ_DEBUG
                    lockData.AddRecursiveReadLockDebugThreadID(readLockID);
#endif
                    return true;
                }

                // Exclusive lock is held, release read lock
                Interlocked.Decrement(ref readersVar);
                
                var exitLockTime = VTime.intraFrameTime + timeoutSeconds;
                
                // while it is locked - spin
                while (Interlocked.Read(ref lockVar) != 0)
                {
#if PROFILE_SPINS
                    using var profileScope = BurstSpinLockReadWrite.SpinProfilerMarker_ReadAgainstWrite.Auto();
#endif
                    if (VTime.intraFrameTime > exitLockTime)
                    {
#if DEADLOCK_DEBUG
                        // Determine the type of deadlock, a deadlock during a tempHold is critical information.
                        if (!lockData.TempHold)
                            Debug.LogError($"Failed to get read lock, exclusive lock is held by [Holder ID: {lockData.WriteLockID}, TempHold: {lockData.TempHold}]");
                        else
                            Debug.LogError($"READ-WRITE-READ DEADLOCK DETECTED: Failed to get read lock, exclusive lock is TEMPORARILY held by ID: {lockData.WriteLockID} who is trying to get an exclusive lock but is waiting for the read lock to release!");
#endif
                        return false; // Timed out
                    }

                    Common.Pause();
                    
                    // Don't have access for some reason, thanks Unity
                    //Baselib.LowLevel.Binding.Baselib_Thread_YieldExecution();
                }
            }
        }

        /// <summary> Exit read lock. EnterRead must be called before this call by the same thread </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExitRead(ref BurstSpinLockReadWrite.Data lockData
#if RECURSIVE_READ_DEBUG
            , ulong readLockID
#endif
        )
        {
#if RECURSIVE_READ_DEBUG
            lockData.RemoveRecursiveReadLockThreadID(readLockID);
#endif
            var decrementedReadLockValue = Interlocked.Decrement(ref lockData.ReadersLockValue);
            // Check the interlocked decremented read lock value to ensure we throw on the correct stack
            CheckForNegativeReaders(decrementedReadLockValue);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CheckForNegativeReaders(long readers)
        {
            if (readers < 0)
                throw new Exception("Reader count cannot be negative!");
        }
    }
}