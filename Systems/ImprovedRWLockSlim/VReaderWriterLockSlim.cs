#if UNITY_EDITOR
//#define SAFETY_TRACKING
#endif

using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;

// NOTE: Deprecated but powerful thread-safe safety tracking pattern:
// Use an infinite ID system Interlocked + long value
// Back the IDs against an array
// Store unused array indices in a concurrent queue
// When you need an ID, dequeue an index, increment the ID, store the ID in the array
// When you want to invalid an ID, simply zero the array slot and reenqueue the index
        
// Overly advanced safety system, too much memory overhead, but otherwise a very strong system
/*long[] validKeyArray;
// ID array cycling
ushort[] unusedIDIndices;
int unusedIDIndicesCount;*/

namespace VLib
{
    /// <summary> Contains a <see cref="ReaderWriterLockSlim"/> but is capable of handing out safely scoped structs that lock and are protected against double disposal.
    /// Scoping structs support up to <see cref="ThreadSafeArrayLength"/> simultaneous locks, if you need something that can breach this, use a different lock. </summary>
    public class VReaderWriterLockSlim
    {
        //const int ThreadSafeArrayLength = JobsUtility.MaxJobThreadCount * 2;

        public ReaderWriterLockSlim internalLock;
        
        long wrapperIDSource;
#if SAFETY_TRACKING
        internal ConcurrentDictionary<long, bool> activeIDTracking = new();
#endif

        public VReaderWriterLockSlim(LockRecursionPolicy supportsRecursion = LockRecursionPolicy.SupportsRecursion)
        {
            wrapperIDSource = 0;
            
            // Create Objects
            internalLock = new(supportsRecursion);
        }
        
        public void Dispose()
        {
            internalLock?.Dispose();
            internalLock = null;
        }

        public VRWLockHoldScoped ScopedReadLock(int timeoutMS = 2000)
        {
            // Get lock
            if (!internalLock.TryEnterReadLock(timeoutMS))
                throw new TimeoutException($"Failed to acquire read lock within {timeoutMS}ms!");
            
#if SAFETY_TRACKING
            SetupValidation(out var wrapperID/*, out var idStoreIndex*/);
            if (!activeIDTracking.TryAdd(wrapperID, true))
                throw new InvalidOperationException($"Failed to add ID {wrapperID} to tracking dictionary!");
            return new VRWLockHoldScoped(this, false, wrapperID);
#else
            return new VRWLockHoldScoped(this, false);
#endif
        }
        
        public VRWLockHoldScoped ScopedExclusiveLock(int timeoutMS = 2000)
        {
            // Get lock
            if (!internalLock.TryEnterWriteLock(timeoutMS))
                throw new TimeoutException($"Failed to acquire write lock within {timeoutMS}ms!");
            
#if SAFETY_TRACKING
            SetupValidation(out var wrapperID);
            if (!activeIDTracking.TryAdd(wrapperID, true))
                throw new InvalidOperationException($"Failed to add ID {wrapperID} to tracking dictionary!");
            return new VRWLockHoldScoped(this, true, wrapperID);
#else
            return new VRWLockHoldScoped(this, true);
#endif
        }

#if SAFETY_TRACKING
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void SetupValidation(out long wrapperID) => wrapperID = Interlocked.Increment(ref wrapperIDSource);
#endif
    }
}