using System;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace VLib
{
    /// <summary> Until this structure is disposed, the allocated memory cannot move. <br/>
    /// The allocations internally are done in chunks to stop elements from moving in memory. <br/>
    /// Elements are recycled when returned to retain memory compactness. <br/>
    /// This struct is NOT copy-safe. </summary>
    public struct VUnsafeParallelPinnedMemory<T>
        where T : unmanaged
    {
        readonly int subListSize;
        readonly int maximumElementCount;
        readonly int maximumIndex;

        int nextIndex;
        
        /// <summary> This list can move. Lists inside cannot! </summary>
        UnsafeList<UnsafeList<T>> lists;
        VUnsafeRef<int> listsLock;
        
        /// <summary> This list can also move. </summary>
        UnsafeList<int> unusedIndices;
        VUnsafeRef<int> unusedIndicesLock;
        
        public bool IsCreated => lists.IsCreated && unusedIndices.IsCreated && listsLock.IsCreated && unusedIndicesLock.IsCreated;
        
        public VUnsafeParallelPinnedMemory(int maximumListCount, int subListSize)
        {
            this.subListSize = subListSize;

            long maxCountL = subListSize * (long) maximumListCount;
            maxCountL.CheckValueInRange(1, int.MaxValue);
            
            maximumElementCount = subListSize * maximumListCount;
            maximumIndex = maximumElementCount - 1;
            
            lists = new UnsafeList<UnsafeList<T>>(maximumListCount, Allocator.Persistent);
            unusedIndices = new UnsafeList<int>(maximumListCount, Allocator.Persistent);

            nextIndex = -1;
            
            listsLock = new VUnsafeRef<int>(0, Allocator.Persistent);
            unusedIndicesLock = new VUnsafeRef<int>(0, Allocator.Persistent);
        }

        public void Dispose()
        {
            using (listsLock.ScopedAtomicLock())
            {
                if (lists.IsCreated)
                {
                    for (var index = 0; index < lists.Length; index++)
                    {
                        var list = lists[index];
                        if (list.IsCreated)
                            list.Dispose();
                        lists[index] = default;
                    }
                    lists.DisposeRefToDefault();
                }
            }
            using (unusedIndicesLock.ScopedAtomicLock())
                unusedIndices.DisposeRefToDefault();
            
            if (listsLock.IsCreated)
                listsLock.Dispose();
            listsLock = default;
            if (unusedIndicesLock.IsCreated)
                unusedIndicesLock.Dispose();
            unusedIndicesLock = default;
        }

        /// <summary> Fully concurrent safe. </summary>
        public PinnedMemoryElement<T> GetPinnedAddress()
        {
            int index = FetchIndex();
            return GetPinnedMemoryAtIndex(index);
        }

        /// <summary> Fully concurrent safe. </summary>
        public void ReturnAddress(PinnedMemoryElement<T> address)
        {
            if (!address.IsCreated)
                return;
            address.Value = default;
            ReturnIndex(address.ListIndex);
        }

        int FetchIndex()
        {
            // Try get unused
            if (!unusedIndices.IsEmpty)
            {
                using var lockHold = unusedIndicesLock.ScopedAtomicLock();
                // Protect against empty again now that we're in the lock scope
                if (unusedIndices.TryPop(out var unusedIndex, false))
                    return unusedIndex;
            }
            
            // Try get new
            if (nextIndex >= maximumIndex)
                throw new InvalidOperationException($"Maximum index '{maximumIndex}' reached!");
            
            return Interlocked.Increment(ref nextIndex);
        }
        
        void ReturnIndex(int unusedIndex)
        {
            using var lockHold = unusedIndicesLock.ScopedAtomicLock();
            unusedIndices.Add(unusedIndex);
        }

        unsafe PinnedMemoryElement<T> GetPinnedMemoryAtIndex(int globalIndex)
        {
            VCollectionUtils.ConditionalCheckIndexValid(globalIndex, maximumElementCount);
            
            var subListIndex = globalIndex / subListSize;
            var subListSubIndex = globalIndex % subListSize;

            // Ensure sub list exists
            if (subListIndex >= lists.Length)
            {
                using var lockHold = listsLock.ScopedAtomicLock();
                // Check again within the lock
                while (subListIndex >= lists.Length)
                {
                    // Create new list
                    var newList = new UnsafeList<T>(subListSize, Allocator.Persistent);
                    lists.AddNoResize(newList);
                }
            }
            
            ref var subListRef = ref lists.ElementAt(subListIndex);
            
            // Ensure sublist index is valid
            if (subListRef.Length <= subListSubIndex)
            {
                using var lockHold = listsLock.ScopedAtomicLock();
                // Check again within the lock
                while (subListRef.Length <= subListSubIndex)
                    subListRef.AddNoResize(default);
            }

            return new PinnedMemoryElement<T>(globalIndex, subListRef.GetListElementPtr(subListSubIndex));
        }
    }
}