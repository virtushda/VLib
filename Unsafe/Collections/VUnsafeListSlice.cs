using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace VLib.UnsafeListSlicing
{
    /// <summary> Lets you read/write a portion of a list with no copy. This must be used with caution, the main list safety will be checked.
    /// This acts like a list with a limited capacity as it constrains all operations within the slice. </summary>
    public struct VUnsafeListSlice<T> : IVLibUnsafeContainer, IReadOnlyList<T>, IDisposable
        where T : unmanaged
    {
        VUnsafeList<T> mainList;
        VUnsafeRef<int> lengthMemory;
        public readonly int sliceStartIndex;
        /// <summary> This can be faster than <see cref="Length"/> when you are using an alloc-free slice. </summary>
        public readonly int sliceLength;
        
        /// <summary> Usable without length allocation. </summary>
        public bool IsCreated => mainList.IsCreated;

        /// <summary> A number of functions are unusable without an allocation to track internal slice length reliably. This is a perf tradeoff. <br/>
        /// If this slice does not use a length allocation, <see cref="Length"/> will be equal to <see cref="Capacity"/>, the latter being faster to call. </summary>
        public bool LengthMutable => lengthMemory.IsCreated;
        
        /// <summary> Usable without length allocation. </summary>
        public VUnsafeList<T> MainList => mainList;
        
        /// <summary> Usable without length allocation. </summary>
        public int Length
        {
            get => lengthMemory.IsCreated ? lengthMemory.Value : sliceLength;
            set
            {
                CheckLengthModifiable();
                CheckInsideCapacity(value - 1);
                lengthMemory.Value = value;
            }
        }

        ref int LengthInternalRef
        {
            get
            {
                CheckLengthModifiable();
                return ref lengthMemory.ValueRef;
            }
        }

        /// <summary> Usable without length allocation. </summary>
        public int Capacity
        {
            readonly get => sliceLength;
            set => throw new NotImplementedException("It is not safe to change the capacity of a slice. Far safer to simply reslice the original container. " +
                                                     "If you are using a method which MAY change the capacity, ensure you are staying within capacity.");
        }

        public readonly unsafe void* GetUnsafePtr() => ((T*) mainList.GetUnsafePtr()) + sliceStartIndex;

        /// <summary> Get AND Set are usable without length allocation. </summary>
        public T this[int index]
        {
            readonly get
            {
                CheckSliceIndex(index);
                return mainList[sliceStartIndex + index];
            }
            set
            {
                CheckSliceIndex(index);
                mainList[sliceStartIndex + index] = value;
            }
        }

        /// <summary> Passing None/Invalid write allocator will make this slice read-only. </summary>
        public VUnsafeListSlice(VUnsafeList<T> mainList, int start, int length, Allocator lengthAllocator)
        {
            this.mainList = mainList;
            sliceStartIndex = start;
            sliceLength = length;
            this.lengthMemory = GetLengthMemoryFor(lengthAllocator, length);
            CheckMainList();
            CheckStartLength(start, length);
        }

        /*/// <summary> Create a slice with manually provided memory for length tracking. Moving the provided memory will cause dangerous behaviour. </summary>
        public VUnsafeListSlice(VUnsafeList<T> mainList, int start, int length, VUnsafeRef<int> externalLengthMemory)
        {
            this.mainList = mainList;
            sliceStartIndex = start;
            sliceLength = length;
            lengthMemory = externalLengthMemory;
            CheckMainList();
            CheckStartLength(start, length);
        }*/
            
        /// <summary> Create from another slice. <br/>
        /// Passing None/Invalid write allocator will make this slice read-only.</summary>
        public VUnsafeListSlice(VUnsafeListSlice<T> slice, int startRelative, int length, Allocator lengthAllocator)
        {
            mainList = slice.mainList;
            sliceStartIndex = slice.sliceStartIndex + startRelative;
            sliceLength = length;
            lengthMemory = GetLengthMemoryFor(lengthAllocator, length);
            CheckMainList();
            CheckStartLengthSliceOfSlice(slice, startRelative, length);
        }
        
        static VUnsafeRef<int> GetLengthMemoryFor(Allocator allocator, int initValue) => allocator is not (Allocator.Invalid or Allocator.None) ? new VUnsafeRef<int>(initValue, allocator) : default;

        public void Dispose()
        {
            if (lengthMemory.IsCreated)
                lengthMemory.Dispose();
        }

        /// <summary> Usable without length allocation. </summary>
        public ref T ElementAt(int index)
        {
            CheckSliceIndex(index);
            return ref mainList.ElementAt(sliceStartIndex + index);
        }

        // Can bring this back if needed...
        /*/// <summary> Value at last index will be discarded if capacity is reached. </summary>
        public void InsertDiscardLast(int index, T value)
        {
            CheckLengthModifiable();
            CheckSliceIndex(index);
            // Decrement length if at capacity, first to avoid pushing last values outside the slice
            if (lengthMemory.ValueRef >= Capacity)
                --lengthMemory.ValueRef;
            
            // Insert, discarding last value
            mainList
            sublist.Insert(index, value);
        }*/
        
        /// <summary> Requires length allocation. </summary>
        public bool TryAddNoResize(in T value)
        {
            CheckLengthModifiable();
            if (LengthInternalRef >= Capacity)
                return false;
            mainList[sliceStartIndex + LengthInternalRef++] = value;
            return true;
        }

        /// <summary> Requires length allocation. </summary>
        public bool TryAddRangeNoResize(UnsafeList<T> values, int count)
        {
            CheckLengthModifiable();
            ref var lengthRef = ref LengthInternalRef;
            if (count > (Capacity - lengthRef))
                return false;

            var start = sliceStartIndex + lengthRef;

            unsafe
            {
                UnsafeUtility.MemCpy(mainList.ListData.Ptr + start, values.Ptr, count * sizeof(T)); // Implicitly checks IsCreated
            }

            lengthRef += count;
            return true;
        }

        /// <summary> Requires length allocation. <br/>
        /// Shifts all memory beyond the index, toward zero by one index. Overwrites the value at 'index'. </summary>
        public void RemoveAt(int index)
        {
            CheckLengthModifiable();
            CheckSliceIndex(index);
            var start = sliceStartIndex + index;
            var end = sliceStartIndex + LengthInternalRef;
            unsafe
            {
                UnsafeUtility.MemMove(mainList.ListData.Ptr + start, mainList.ListData.Ptr + start + 1, (end - start - 1) * sizeof(T));
            }
            LengthInternalRef--;
        }

        /*/// <summary> Shifts all memory within the slice that is beyond the index, toward zero by one index. Overwrites the value at 'index'. <br/>
        /// Cannot affect length, length will be invalid. <br/>
        /// This method is intended to facilitate 'RemoveAt' behaviour when the length is tracked externally. </summary>
        public unsafe void RemoveAtMemShiftUnsafe(int index)
        {
            CheckSliceIndex(index);
            var start = sliceStartIndex + index;
            var end = sliceStartIndex + Capacity;
            UnsafeUtility.MemMove(mainList.listData->Ptr + start, mainList.listData->Ptr + start + 1, (end - start - 1) * sizeof(T));
        }*/

        /// <summary> Requires length allocation. </summary>
        public void ClearFast()
        {
            CheckLengthModifiable();
            LengthInternalRef = 0;
        }

        /// <summary> Requires length allocation. </summary>
        public void ClearAllToDefault(T clearValue = default)
        {
            CheckMainList();
            CheckLengthModifiable();
            mainList.WriteValueToRange(clearValue, sliceStartIndex, Capacity);
            LengthInternalRef = 0;
        }

        /// <summary> Requires length allocation. </summary>
        public void WriteToUnusedCapacity(T value)
        {
            CheckLengthModifiable();
            var unusedStartIndex = sliceStartIndex + Length;
            var unusedEnd = sliceStartIndex + Capacity; 
            for (int i = unusedStartIndex; i < unusedEnd; i++)
                mainList[i] = value;
        }

        /// <summary> Slices this slice, to obtain a slice outside the bounds of this slice, get a new slice from <see cref="mainList"/> <br/>
        /// Providing a <see cref="lengthAllocator"/> allows the slice to track its own internal length and gives it enhanced capabilities at a small perf cost.
        /// Pass <see cref="Allocator.None"/> for a lightweight slice. </summary>
        public readonly VUnsafeListSlice<T> Slice(int start, int length, Allocator lengthAllocator) => new(this, sliceStartIndex + start, length, lengthAllocator);
        
        #region Copying
        
        public NativeArray<T> ToNativeArray(Allocator allocator) => VCollectionUtils.ToArray<VUnsafeListSlice<T>, T>(this, allocator);
        public T[] ToManagedArray() => VCollectionUtils.ToManagedArray<VUnsafeListSlice<T>, T>(this);
        public void CopyFrom(UnsafeList<T> list) => VCollectionUtils.CopyFromTo(list, this);
        
        #endregion
        
        #region ReadOnly

        public ReadOnly AsReadOnly() => new(this);
        
        public readonly struct ReadOnly : IReadOnlyList<T>
        {
            readonly VUnsafeListSlice<T> slice;
            
            public ReadOnly(VUnsafeListSlice<T> slice) => this.slice = slice;

            /// <summary> <inheritdoc cref="VUnsafeListSlice{T}.LengthMutable"/> </summary>
            public bool LengthMutable => slice.LengthMutable;
            public int Length => slice.Length;
            public int Capacity => slice.Capacity;
            
            public T this[int index] => slice[index];
            
            #region IReadOnlyList Support

            public int Count => Length;
            
            public ReadOnlyEnumerator GetEnumerator() => new(slice);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

            public struct ReadOnlyEnumerator : IEnumerator<T>
            {
                VUnsafeListSlice<T> slice;
                int index;
                int endCached;

                public ReadOnlyEnumerator(VUnsafeListSlice<T> slice)
                {
                    this.slice = slice;
                    index = slice.sliceStartIndex - 1;
                    endCached = slice.sliceStartIndex + slice.Length;
                }

                public bool MoveNext() => ++index < endCached;

                public void Reset() => index = slice.sliceStartIndex - 1;
                public T Current => slice.mainList[index];
                object IEnumerator.Current => Current;
                public void Dispose() => slice = default;
            }

            #endregion
        }
        
        #endregion
        
        #region IReadOnlyList Support

        public Enumerator GetEnumerator() => new(this);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        
        public int Count => Length;
        
        // Enumerator struct
        public struct Enumerator : IEnumerator<T>
        {
            VUnsafeListSlice<T> slice;
            int index;
            int endCached;

            public Enumerator(VUnsafeListSlice<T> slice)
            {
                this.slice = slice;
                index = slice.sliceStartIndex - 1;
                endCached = slice.sliceStartIndex + slice.Length;
            }

            public bool MoveNext() => ++index < endCached;

            public void Reset() => index = slice.sliceStartIndex - 1;
            public T Current => slice.mainList[index];
            object IEnumerator.Current => Current;
            public void Dispose() => slice = default;
        }

        #endregion

        #region Checks

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        readonly void CheckLengthModifiable()
        {
            if (!lengthMemory.IsCreated)
                throw new InvalidOperationException("Slice is not write capable, it was initialized as readonly, without the ability to store it's own length.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        readonly void CheckMainList()
        {
            if (!mainList.IsCreated)
                throw new InvalidOperationException("Main list is not created");
        }

        /// <summary> This method implicitly will throw also if the main list is not created. </summary>
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        readonly void CheckSliceIndex(int index)
        {
            index += sliceStartIndex;
            if (!mainList.IsIndexValid(index))
                throw new IndexOutOfRangeException($"Index {index} is out of range for mainlist of length {mainList.Length}");
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        readonly void CheckStartLength(int start, int length)
        {
            if (start < 0)
                throw new ArgumentOutOfRangeException($"Slice cannot start at negative index {start}");
            if (length < 0)
                throw new ArgumentOutOfRangeException($"Slice cannot have a negative length {length}");
            if (start + length > mainList.Length)
                throw new ArgumentOutOfRangeException($"Slice start {start} and length {length} are out of range for slice of length {Length}");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void CheckStartLengthSliceOfSlice(VUnsafeListSlice<T> slice, int startRelative, int length)
        {
            if (startRelative < 0)
                throw new ArgumentOutOfRangeException($"Slice of a slice cannot start before the source slice.");
            if (length < 0)
                throw new ArgumentOutOfRangeException($"Slice cannot have a negative length {length}");
            if (startRelative + length > slice.Length)
                throw new ArgumentOutOfRangeException($"Slice start {startRelative} and length {length} are out of range for slice of length {slice.Length}");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        readonly void CheckInsideCapacity(int index)
        {
            if (index >= Capacity)
                throw new ArgumentOutOfRangeException($"Index {index} is out of range for slice of capacity {Capacity}");
            if (index < 0)
                throw new ArgumentOutOfRangeException($"Index {index} is negative");
        }

        #endregion

        [BurstDiscard]
        public override string ToString() => $"Start:{sliceStartIndex} | Length:{Length} | Capacity:{Capacity} | Type:{typeof(T)}";
    }

    public static class VUnsafeListSliceExt
    {
        // Bring this back if needed...
        /*/// <summary> Could be accelerated with a binary search if needed... </summary>
        public static bool InsertSorted<T>(this VUnsafeListSlice<T> slice, T value)
            where T : unmanaged, System.IComparable<T>
        {
            for (int i = 0; i < slice.Length; i++)
            {
                if (value.CompareTo(slice[i]) < 0)
                {
                    slice.InsertDiscardLast(i, value);
                    return true;
                }
            }
            return false;
        }*/
    }
}