using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections.LowLevel.Unsafe;

namespace VLib.UnsafeListSlicing
{
    /// <summary> Lets you read/write a portion of a list with no copy. This must be used with caution, the main list safety will be checked. No disposal needed.
    /// This acts like a list with a limited capacity as it constrains all operations within the slice. </summary>
    public unsafe struct VUnsafeListSlice<T> : IEnumerable<T>
        where T : unmanaged
    {
        VUnsafeList<T> mainList;
        UnsafeList<T> sublist;
        readonly int sliceStartIndex;
        readonly int sliceLength;
        
        public VUnsafeListSlice(VUnsafeList<T> mainList, int start, int length)
        {
            this.mainList = mainList;
            sliceStartIndex = start;
            sliceLength = length;
            sublist = new UnsafeList<T>(mainList.listData->Ptr + start, length);
            CheckMainList();
            CheckStartLength(start, length);
        }
        
        public VUnsafeListSlice(VUnsafeListSlice<T> slice, int startRelative, int length)
        {
            mainList = slice.mainList;
            sliceStartIndex = slice.sliceStartIndex + startRelative;
            sliceLength = length;
            sublist = new UnsafeList<T>(mainList.listData->Ptr + sliceStartIndex, length);
            CheckMainList();
            CheckStartLengthSliceOfSlice(startRelative, length);
        }
        
        public bool IsCreated => mainList.IsCreated;
        public VUnsafeList<T> MainList => mainList;
        
        public int Length => sublist.Length;
        public int Capacity => sliceLength;
        
        public T this[int index]
        {
            get
            {
                CheckIndex(index);
                return sublist[index];
            }
            set
            {
                CheckIndex(index);
                sublist[index] = value;
            }
        }
        
        public ref T ElementAt(int index)
        {
            CheckIndex(index);
            return ref sublist.ElementAt(index);
        }

        /// <summary> Value at last index will be discarded if capacity is reached. </summary>
        public void InsertDiscardLast(int index, T value)
        {
            CheckIndex(index);
            // Decrement length if at capacity, first to avoid pushing last values outside the slice
            if (Length >= Capacity)
                --sublist.Length;
            // Insert, discarding last value
            sublist.Insert(index, value);
        }
        
        public bool TryAddNoResize(in T value)
        {
            if (Length >= Capacity)
                return false;
            sublist.AddNoResize(value);
            return true;
        }

        public bool TryAddRangeNoResize(UnsafeList<T> values, int count)
        {
            if (count > (Capacity - Length))
                return false;
            sublist.AddRangeNoResize(values.Ptr, count);
            return true;
        }

        public void ClearFast() => sublist.Clear();
        
        public void ClearToDefault(T clearValue = default)
        {
            CheckMainList();
            for (int i = 0; i < sublist.Length; i++)
                sublist[i] = clearValue;
            sublist.Clear();
        }

        public void WriteToUnusedCapacity(T value)
        {
            var unusedStartIndex = sliceStartIndex + Length;
            var end = sliceStartIndex + Capacity; 
            for (int i = sliceStartIndex + unusedStartIndex; i < end; i++)
                mainList[i] = value;
        }

        /// <summary> Slices this slice, to obtain a slice outside the bounds of this slice, get a new slice from <see cref="mainList"/> </summary>
        public VUnsafeListSlice<T> Slice(int start, int length) => new(this, sliceStartIndex + start, length);
        
        // Burst compatible, non-boxing enumeration
        UnsafeList<T>.Enumerator GetEnumerator() => sublist.GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw new NotImplementedException("NO BOXING");
        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException("NO BOXING");

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckMainList()
        {
            if (!mainList.IsCreated)
                throw new System.InvalidOperationException("Main list is not created");
        }

        /// <summary> This method implicitly will throw also if the main list is not created. </summary>
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckIndex(int index)
        {
            index += sliceStartIndex;
            if (!mainList.IsIndexValid(index))
                throw new System.IndexOutOfRangeException($"Index {index} is out of range for mainlist of length {mainList.Length}");
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckStartLength(int start, int length)
        {
            if (start < 0)
                throw new System.ArgumentOutOfRangeException($"Slice cannot start at negative index {start}");
            if (length < 0)
                throw new System.ArgumentOutOfRangeException($"Slice cannot have a negative length {length}");
            if (start + length > mainList.Length)
                throw new System.ArgumentOutOfRangeException($"Slice start {start} and length {length} are out of range for slice of length {Length}");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckStartLengthSliceOfSlice(int startRelative, int length)
        {
            if (startRelative < sliceStartIndex)
                throw new System.ArgumentOutOfRangeException($"Slice of a slice cannot start before the source slice.");
            if (length < 0)
                throw new System.ArgumentOutOfRangeException($"Slice cannot have a negative length {length}");
            if (startRelative + length > Length)
                throw new System.ArgumentOutOfRangeException($"Slice start {startRelative} and length {length} are out of range for slice of length {Length}");
        }
    }

    public static class VUnsafeListSliceExt
    {
        /// <summary> Could be accelerated with a binary search if needed... </summary>
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
        }
    }
}