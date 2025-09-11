using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Profiling;
using VLib.SyncPrimitives.IntRef;

namespace VLib
{
    /// <summary> Until this structure is disposed, the allocated memory cannot move. <br/>
    /// The allocations internally are done in chunks to stop elements from moving in memory. <br/>
    /// Elements are recycled when returned to retain memory compactness. <br/>
    /// This struct is NOT copy-safe. </summary>
    public struct VUnsafeParallelPinnedMemory<T> : IAllocating
        where T : unmanaged
    {
        public readonly int subListSize;
        public readonly int maximumElementCount;
        
        /// <summary> This list can move. Lists inside cannot! </summary>
        UnsafeList<UnsafeList<T>> lists;
        VUnsafeRef<int> listsLock;

        /// <summary> This list can also move. </summary>
        VPackedIndexProvider packedIndices;
        
        public bool IsCreated => /*lists.IsCreated && unusedIndices.IsCreated && */listsLock.IsCreated/* && unusedIndicesLock.IsCreated*/;
        
        public VUnsafeParallelPinnedMemory(int maximumListCount, int subListSize)
        {
            this.subListSize = subListSize;

            long maxCountL = subListSize * (long) maximumListCount;
            maxCountL.CheckValueInRange(1, int.MaxValue);
            
            maximumElementCount = subListSize * maximumListCount;
            var maximumIndex = maximumElementCount - 1;
            
            lists = new UnsafeList<UnsafeList<T>>(maximumListCount, Allocator.Persistent);
            listsLock = new VUnsafeRef<int>(0, Allocator.Persistent);

            packedIndices = VPackedIndexProvider.Create(maximumIndex, Allocator.Persistent);
        }

        public void Dispose()
        {
            const string profilerMessage = "VUnsafeParallelPinnedMemory.Dispose";
            Profiler.BeginSample(profilerMessage);
            
            if (!listsLock.IsCreated)
                return;
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
            
            packedIndices.Dispose();

            if (listsLock.IsCreated)
                listsLock.Dispose();
            listsLock = default;
            
            Profiler.EndSample();
        }

        /// <summary> Fully concurrent safe. </summary>
        public PinnedMemoryElement<T> GetPinnedAddress()
        {
            this.ConditionalCheckIsCreated();
            int index = FetchIndex();
            return GetPinnedMemoryAtIndex(index);
        }

        /// <summary> Fully concurrent safe. <br/>
        /// Note on race condition potential: <br/>
        /// This method itself is safe, but if you rely on the value in the memory being non-default,
        /// and check it BEFORE passing it in here where it's defaulted, you may produce a race condition!
        /// </summary>
        public bool ReturnAddress(PinnedMemoryElement<T> address)
        {
            this.ConditionalCheckIsCreated();
            if (!address.IsCreated)
                return false;
            address.Value = default;
            ReturnIndex(address.ListIndex);
            return true;
        }

        int FetchIndex() => packedIndices.FetchIndex();
        
        void ReturnIndex(int unusedIndex) => packedIndices.ReturnIndex(unusedIndex);

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

        public int TakenCount()
        {
            this.ConditionalCheckIsCreated();
            return packedIndices.TakenCount();
        }
    }
}