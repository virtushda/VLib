using System;
using System.Diagnostics;
using Unity.Burst.CompilerServices;
using Unity.Collections;

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
        }
        
        public void Dispose()
        {
            buffer.Dispose();
            listStartLengths.Dispose();
        }

        /// <summary> Creates a new sublist and sets it as the write target. </summary>
        public void AddList()
        {
            var noLists = listStartLengths.Length < 1;
            if (Hint.Unlikely(noLists))
            {
                listStartLengths.Add(new StartLength {start = 0, length = 0});
            }
            else
            {
                var lastStartLength = listStartLengths[EndListIndex];
                listStartLengths.Add(new StartLength {start = lastStartLength.start + lastStartLength.length, length = 0});
            }
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

        public bool TryGetListSliceAtIndex(int i, out Span<T> bufferSlice)
        {
            // Try get list
            if (!listStartLengths.TryGetValue(i, out var startLength))
            {
                bufferSlice = default;
                return false;
            }
            // See if list has any allocation
            if (startLength.length < 1)
            {
                bufferSlice = default;
                return false;
            }

            bufferSlice = buffer.ListData.LengthAsSpan().Slice(startLength.start, startLength.length);
            return true;
        }

        public bool ListIndexInRange(int listIndex) => listIndex >= 0 && listIndex < listStartLengths.Length;

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        void CheckEndListIndex()
        {
            if (EndListIndex < 0)
                throw new System.InvalidOperationException($"No lists have been added. You must call {nameof(AddList)} before calling {nameof(AddElementToCurrent)}.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        void CheckListIndex(int index)
        {
            if (!ListIndexInRange(index))
                throw new System.ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range. Must be between 0 and {listStartLengths.Length - 1}.");
        }
    }
}