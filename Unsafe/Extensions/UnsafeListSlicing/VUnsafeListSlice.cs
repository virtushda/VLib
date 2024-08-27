using System.Diagnostics;
using Unity.Collections.LowLevel.Unsafe;

namespace VLib.UnsafeListSlicing
{
    /// <summary> Lets you read/write a portion of a list with no copy. This must be used with caution, the main list safety will be checked. No disposal needed. </summary>
    public unsafe struct VUnsafeListSlice<T>
        where T : unmanaged
    {
        readonly VUnsafeList<T> mainList;
        UnsafeList<T> sublist;
        readonly int sliceStartIndex;
        
        public VUnsafeListSlice(VUnsafeList<T> mainList, int start, int length)
        {
            this.mainList = mainList;
            sliceStartIndex = start;
            sublist = new UnsafeList<T>(mainList.listData->Ptr + start, length);
            CheckMainList();
        }
        
        public bool IsCreated => mainList.IsCreated;
        
        public int Length => sublist.Length;
        
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

        /// <summary> Value at last index will be discarded. </summary>
        public void InsertDiscardLast(int index, T value)
        {
            CheckIndex(index);
            // Decrement length first to avoid pushing last values outside the slice
            --sublist.Length;
            // Insert, discarding last value
            sublist.Insert(index, value);
        }
        
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