using System.Diagnostics;
using Unity.Collections;
using VLib.UnsafeListSlicing;

namespace VLib.Libraries.VLib.Collections
{
    /// <summary> A list of lists, backed by a single contiguous buffer. <br/>
    /// For performance, the usage pattern is to add to the current list index, then increment it and add. <br/>
    /// Designed to be linearly constructed on a single thread. </summary>
    public struct UnsafeCompactListSet<T>
        where T : unmanaged
    {
        VUnsafeList<T> buffer;
        VUnsafeList<StartLength> listStartLengths;

        public int BufferLength => buffer.Length;
        public int ListCount => listStartLengths.Length;
        public int EndListIndex => listStartLengths.Length - 1;
        public bool IsCreated => buffer.IsCreated && listStartLengths.IsCreated;

        struct StartLength
        {
            public int start;
            public int length;
        }
        
        public UnsafeCompactListSet(int initialCapacity, Allocator allocator)
        {
            buffer = new VUnsafeList<T>(initialCapacity, allocator);
            listStartLengths = new VUnsafeList<StartLength>(initialCapacity / 4, allocator);
            // Create initial list
            listStartLengths.Add(new StartLength {start = 0, length = 0});
        }
        
        public void Dispose()
        {
            buffer.Dispose();
            listStartLengths.Dispose();
        }
        
        public VUnsafeListSlice<T> this[int index] => GetListSliceAtIndex(index);

        /// <summary> Creates a new sublist and sets it as the write target. </summary>
        public void AddList()
        {
            var lastStartLength = listStartLengths[EndListIndex];
            listStartLengths.Add(new StartLength {start = lastStartLength.start + lastStartLength.length, length = 0});
        }

        /// <summary> Adds to the list at index <see cref="EndListIndex"/> </summary>
        public void AddElementToCurrent(in T value)
        {
            CheckEndListIndex();
            var listIndex = EndListIndex;
            ref var startLength = ref listStartLengths.ElementAt(listIndex);
            buffer.Add(value);
            ++startLength.length;
        }

        public void ClearAll()
        {
            buffer.Clear();
            listStartLengths.Clear();
        }
        
        public VUnsafeListSlice<T> GetListSliceAtIndex(int i)
        {
            CheckListIndex(i);
            var startLength = listStartLengths[i];
            return buffer.Slice(startLength.start, startLength.length);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckEndListIndex()
        {
            if (EndListIndex < 0)
                throw new System.InvalidOperationException($"No lists have been added. You must call {nameof(AddList)} before calling {nameof(AddElementToCurrent)}.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckListIndex(int index)
        {
            if (index < 0 || index >= listStartLengths.Length)
                throw new System.ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range. Must be between 0 and {listStartLengths.Length - 1}.");
        }
    }
}