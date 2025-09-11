using System;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using VLib.SyncPrimitives.IntRef;

namespace VLib
{
    /// <summary> A general solution for providing indices that can be recycled. <br/>
    /// Inherently resistant to double returns. <br/>
    /// Concurrent-safe. <br/>
    /// NOT copy safe.</summary>
    public struct VPackedIndexProvider : IAllocating
    {
        VUnsafeRef<int> locker;
        UnsafeHashSet<int> recyclables;
        int nextIndex;

        int maxIndex;
        public int MaxIndex
        {
            get => maxIndex;
            set
            {
                using var lockHold = locker.ScopedAtomicLock(5f);
                if (value < nextIndex)
                    throw new InvalidOperationException("Cannot set a smaller max index.");
                maxIndex = value;
            }
        }
        readonly bool reportAllNotDisposed;

        public bool IsCreated => locker.IsCreated;

        VPackedIndexProvider(int maxIndex, Allocator allocator, bool reportAllNotDisposed)
        {
            recyclables = new UnsafeHashSet<int>(64, allocator);
            locker = new VUnsafeRef<int>(0, allocator);
            nextIndex = 0;
            BurstAssert.True(maxIndex > 0);
            this.maxIndex = maxIndex;
            this.reportAllNotDisposed = reportAllNotDisposed;
        }

        /// <summary> Create a new VPackedIndexProvider. </summary>
        /// <param name="maxIndex"> The maximum index that can be provided. </param>
        /// <param name="allocator"> The allocator to use for the internal data. </param>
        /// <param name="reportAllNotDisposed"> If true, will report all indices that were not returned when disposed. </param>
        public static VPackedIndexProvider Create(int maxIndex = int.MaxValue, Allocator allocator = Allocator.Persistent, bool reportAllNotDisposed = true)
        {
            return new VPackedIndexProvider(maxIndex, allocator, reportAllNotDisposed);
        }

        public void Dispose()
        {
            if (!recyclables.IsCreated)
                Debug.LogError("Recyclables not created when disposing VPackedIndexProvider");
            if (locker.ValueRef > 0)
                Debug.LogError($"Recycle lock held when disposing VPackedIndexProvider");
            if (reportAllNotDisposed)
                ReportAllOutstanding();
            
            locker.Dispose();
            recyclables.Dispose();
        }

        void ReportAllOutstanding() //[CallerMemberName] string callerName = default, [CallerLineNumber] int callerLine = -1)
        {
            var recyclableCount = recyclables.Count;
            var takenIndices = nextIndex - recyclableCount;
            if (takenIndices > 0)
            {
                Debug.LogError($"VPackedIndexProvider Outstanding Report: {takenIndices} taken indices were not returned! \n" +
                               $" Taken: {takenIndices}, Unused: {recyclableCount}, NextIndex: {nextIndex}");
            }
        }

        public int TakenCount()
        {
            this.ConditionalCheckIsCreated();
            using var lockHold = locker.ScopedAtomicLock();
            return nextIndex - recyclables.Count;
        }

        /// <summary> Take a recycled index, or the next index. </summary>
        public int FetchIndex()
        {
            this.ConditionalCheckIsCreated();
            
            // Can improve this with a more lock-free approach if needed
            using var lockHold = locker.ScopedAtomicLock();
            
            // Try recycle
            /*if (!recyclables.IsEmpty)
            {
                using var lockHold = locker.ScopedAtomicLock();*/
                // Protect against empty again now that we're in the lock scope
                if (recyclables.TryGetFirst(out var unusedIndex))
                {
                    recyclables.Remove(unusedIndex);
                    return unusedIndex;
                }
            //}
            
            // Try get new
            if (nextIndex > MaxIndex)
                throw new InvalidOperationException($"Maximum index '{MaxIndex}' reached!");

            return Interlocked.Increment(ref nextIndex) - 1;
        }

        public void ReturnIndex(int unusedIndex, bool logErrors = true)
        {
            this.ConditionalCheckIsCreated();
            using var lockHold = locker.ScopedAtomicLock();
            if (!recyclables.Add(unusedIndex) && logErrors)
                Debug.LogError("Attempted to return index that is already recyclable.");
        }

        public bool IsTaken(int index)
        {
            this.ConditionalCheckIsCreated();
            if (index < 0 || index >= nextIndex)
                return false;
            using var lockHold = locker.ScopedAtomicLock();
            return !recyclables.Contains(index);
        }

        /// <summary> Not guaranteed to be the next index past this call in a multi-threaded scenario. </summary>
        public int PeekNext()
        {
            this.ConditionalCheckIsCreated();
            using var lockHold = locker.ScopedAtomicLock();
            if (recyclables.TryGetFirst(out var unusedIndex))
                return unusedIndex;
            return Volatile.Read(ref nextIndex);
        }

        public bool TryClaimIndex(int index)
        {
            this.ConditionalCheckIsCreated();

            using var lockHold = locker.ScopedAtomicLock();
            
            bool insideRecycleRange = index < nextIndex;
            if (insideRecycleRange)
                return recyclables.Remove(index);
            
            PushNextIndexUntilBeyond(index);
            return true;
        }

        /// <summary> Increments and recycles unused indices until the 'nextIndex' value is pushed one beyond the input index. </summary>
        void PushNextIndexUntilBeyond(int index)
        {
            this.ConditionalCheckIsCreated();
            BurstAssert.True(locker.Value > 0);
            while (nextIndex < index)
            {
                // Populate the recycle buffer with all the indices between the current next index and the claimed index
                recyclables.Add(nextIndex);
                ++nextIndex;
            }
            // Push once more past the claimed index
            if (nextIndex <= index)
                ++nextIndex;
        }

        public void ResetClear()
        {
            this.ConditionalCheckIsCreated();
            using var lockHold = locker.ScopedAtomicLock();
            recyclables.Clear();
            nextIndex = 0;
        }
    }
}