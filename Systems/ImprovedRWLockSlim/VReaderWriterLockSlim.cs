using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace VLib
{
    /// <summary> Contains a <see cref="ReaderWriterLockSlim"/> but is capable of handing out safely scoped structs that lock. </summary>
    public class VReaderWriterLockSlim
    {
        const int ThreadSafeArrayLength = JobsUtility.MaxJobThreadCount * 2;

        public readonly ReaderWriterLockSlim internalLock;
        
        long wrapperIDSource;
        ConcurrentQueue<ushort> unusedIDIndices;
        long[] validKeyArray;

        public VReaderWriterLockSlim(LockRecursionPolicy supportsRecursion = LockRecursionPolicy.SupportsRecursion)
        {
            wrapperIDSource = 0;
            
            // Make sure we have enough indices for all possible threads
            var count = ThreadSafeArrayLength;
            
            // Create Objects
            internalLock = new(supportsRecursion);
            unusedIDIndices = new();
            validKeyArray = new long[count];
            
            // Enqueue all indices
            Assert.IsTrue(ushort.MaxValue >= count);
            for (ushort i = 0; i < count; i++)
                unusedIDIndices.Enqueue(i);
        }

        public VRWLockHoldScoped ScopedReadLock(int timeoutMS = 2000)
        {
            // Get lock
            if (timeoutMS < 0)
                internalLock.EnterReadLock();
            else if (!internalLock.TryEnterReadLock(timeoutMS))
                throw new TimeoutException($"Failed to acquire read lock within {timeoutMS}ms!");
            
            SetupValidation(out var wrapperID, out var idStoreIndex);
            
            return new VRWLockHoldScoped(this, wrapperID, idStoreIndex, false);
        }
        
        public VRWLockHoldScoped ScopedExclusiveLock(int timeoutMS = 2000)
        {
            // Get lock
            if (timeoutMS < 0)
                internalLock.EnterWriteLock();
            else if (!internalLock.TryEnterWriteLock(timeoutMS))
                throw new TimeoutException($"Failed to acquire write lock within {timeoutMS}ms!");
            
            SetupValidation(out var wrapperID, out var idStoreIndex);
            
            return new VRWLockHoldScoped(this, wrapperID, idStoreIndex, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void SetupValidation(out long wrapperID, out ushort idStoreIndex)
        {
            // Grab an unused ID
            if (!unusedIDIndices.TryDequeue(out idStoreIndex))
                throw new InvalidOperationException("Failed to acquire a reader index!");
            wrapperID = Interlocked.Increment(ref wrapperIDSource);
            // Store the ID (thread safe because we've taken this index)
            validKeyArray[idStoreIndex] = wrapperID;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool InvalidateScoped(in VRWLockHoldScoped scoped)
        {
            if (validKeyArray[scoped.idArrayIndex] == scoped.wrapperID)
            {
                // Return the index
                validKeyArray[scoped.idArrayIndex] = default;
                unusedIDIndices.Enqueue(scoped.idArrayIndex);
                
                // Release the lock
                if (scoped.isWrite)
                    internalLock.ExitWriteLock();
                else
                    internalLock.ExitReadLock();
                
                return true;
            }
            return false;
        }

        /// <summary> Fast, thread-safe way to validate  </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsValid(in VRWLockHoldScoped scoped) => scoped.wrapperID > 0 && validKeyArray[scoped.idArrayIndex] == scoped.wrapperID;
    }

    /*public static class ReaderWriterLockSlimExt
    {
        static ConcurrentQueue<RWLockSlimReadScoped> readLockHolderPool;
        static ConcurrentQueue<RWLockSlimWriteScoped> writeLockHolderPool;*/
        
        /*// TODO: Move this tactic into a discrete class for reuse
        static long nextWrapperID;
        internal static ConcurrentQueue<byte> unusedReaderIndices;
        internal static long[] validKeyArray;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Initialize()
        {
            nextWrapperID = 0;
            
            // Make sure we have enough indices for all possible threads
            var count = math.min(JobsUtility.MaxJobThreadCount * 2, byte.MaxValue + 1);
            unusedReaderIndices = new();
            validKeyArray = new long[count];

            for (byte i = 0; i < count; i++)
                unusedReaderIndices.Enqueue(i);
            
            /*readLockHolderPool = new();
            writeLockHolderPool = new();#1#
        }*/
        
        /*///<summary> Returns a wrapper struct where the constructor locks (for concurrent READ) and the dispose call unlocks, use with 'using' keyword. </summary>
        public static RWLockSlimReadScoped ScopedReadLock(this ReaderWriterLockSlim rwLock, int timeoutMS = 2000)
        {
            var readLockHolder = readLockHolderPool.TryDequeue(out var holder) ? holder : new();
            readLockHolder.Initialize(rwLock, timeoutMS);
            return readLockHolder;
        }

        ///<summary> Returns a wrapper struct where the constructor locks (EXCLUSIVE) and the dispose call unlocks, use with 'using' keyword. </summary>
        public static RWLockSlimWriteScoped ScopedExclusiveLock(this ReaderWriterLockSlim rwLock, int timeoutMS = 2000)
        {
            var writeLockHolder = writeLockHolderPool.TryDequeue(out var holder) ? holder : new();
            writeLockHolder.Initialize(rwLock, timeoutMS);
            return writeLockHolder;
        }
        
        internal static void Return(this RWLockSlimReadScoped readLockHolder) => readLockHolderPool.Enqueue(readLockHolder);

        internal static void Return(this RWLockSlimWriteScoped writeLockHolder) => writeLockHolderPool.Enqueue(writeLockHolder);
    }*/

    /*/// <summary> DO NOT TRY TO CACHE THIS. Using within correct scope only. To enforce absolute safety I would need to add more overhead. </summary>
    public class RWLockSlimReadScoped : IDisposable
    {
        ReaderWriterLockSlim rwLock;
        bool enteredLock;
        
        public bool IsHoldingLock => enteredLock;

        ///<summary> Automatically takes the read lock with an optional timeout... </summary>
        ///<param name="rwLock"> The ReaderWriterLockSlim to take the read lock on </param>
        ///<param name="timeoutMS"> The timeout in milliseconds to wait for the lock, or -1 to wait indefinitely </param>
        internal void Initialize(ReaderWriterLockSlim rwLock, int timeoutMS = 2000)
        {
            // Ensure we're initializing on a scoped object that is ready to lock
            if (enteredLock)
                throw new InvalidOperationException("Already entered lock!");
            
            this.rwLock = rwLock;
            
            if (timeoutMS < 0)
                this.rwLock.EnterReadLock();
            else if (!rwLock.TryEnterReadLock(timeoutMS))
                throw new TimeoutException($"Failed to acquire read lock within {timeoutMS}ms!");
        }

        public void Dispose()
        {
            if (enteredLock)
            {
                enteredLock = false;
                rwLock.ExitReadLock();
                this.Return();
            }
        }
    }
    
    /// <summary> DO NOT TRY TO CACHE THIS. Using within correct scope only. To enforce absolute safety I would need to add more overhead. </summary>
    public class RWLockSlimWriteScoped : IDisposable
    { 
        ReaderWriterLockSlim rwLock;
        bool enteredLock;

        public bool IsHoldingLock => enteredLock;
        
        ///<summary> Automatically takes the write lock with an optional timeout... </summary>
        ///<param name="rwLock"> The ReaderWriterLockSlim to take the write lock on </param>
        ///<param name="timeoutMS"> The timeout in milliseconds to wait for the lock, or -1 to wait indefinitely </param>
        internal void Initialize(ReaderWriterLockSlim rwLock, int timeoutMS = 2000)
        {
            // Ensure we're initializing on a scoped object that is ready to lock
            if (enteredLock)
                throw new InvalidOperationException("Already entered lock!");
            
            this.rwLock = rwLock;
            
            if (timeoutMS < 0)
                this.rwLock.EnterWriteLock();
            else if (!rwLock.TryEnterWriteLock(timeoutMS))
                throw new TimeoutException($"Failed to acquire write lock within {timeoutMS}ms!");
            
            enteredLock = true;
        }

        public void Dispose()
        {
            if (enteredLock)
            {
                enteredLock = false;
                rwLock.ExitWriteLock();
                this.Return();
            }
        }
    }*/
    
    /*public readonly struct RWLockSlimReadScoped : IDisposable
    {
        private readonly ReaderWriterLockSlim rwLock;
        public bool IsValid => 

        ///<summary> Automatically takes the read lock with an optional timeout... </summary>
        ///<param name="rwLock"> The ReaderWriterLockSlim to take the read lock on </param>
        ///<param name="timeoutMS"> The timeout in milliseconds to wait for the lock, or -1 to wait indefinitely </param>
        public RWLockSlimReadScoped(ReaderWriterLockSlim rwLock, int timeoutMS)
        {
            this.rwLock = rwLock;
            
            if (timeoutMS < 0)
                this.rwLock.EnterReadLock();
            else if (!rwLock.TryEnterReadLock(timeoutMS))
                throw new TimeoutException($"Failed to acquire read lock within {timeoutMS}ms!");
        }

        public void Dispose()
        {
            if (rwLock is {IsReadLockHeld: true})
                rwLock.ExitReadLock();
        }
    }*/
}