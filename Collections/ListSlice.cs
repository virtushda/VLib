using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;

namespace VLib
{
    public class ListSlice<T> : IReadOnlyList<T>, IListSlice
    {
        IReadOnlyList<T> list;
        int startIndex;
        int sliceLength;

        public IReadOnlyList<T> List => list;

        public int StartIndex
        {
            get => startIndex;
            set => startIndex = math.clamp(value, 0, list.Count - 1);
        }
        
        public int EndIndex
        {
            get => this.GetEndIndex();
            set => this.SetEndIndex(value, list?.Count);
        }
        
        public int SliceLength
        {
            get => sliceLength;
            set
            {
                if (list is not {Count: > 0})
                {
                    sliceLength = 0;
                    return;
                }

                var endIndex = EndIndex;
                sliceLength = math.min(value, list.Count);
                EndIndex = endIndex;
            }
        }

        public float SliceSizeNormalized
        {
            get => this.GetSliceSizeNormalized(list?.Count);
            set => this.SetSliceSizeNormalized(value, list?.Count);
        }

        public float SlicePositionNormalized
        {
            get => this.GetSlicePositionNormalized(list?.Count);
            set => this.SetSlicePositionNormalized(value, list?.Count);
        }

        public ListSlice()
        {
            
        }

        public ListSlice(IReadOnlyList<T> list, int startIndex = 0, int sliceLength = int.MaxValue)
        {
            if (list == null)
                throw new System.ArgumentNullException(nameof(list));
            if (startIndex < 0 || startIndex >= list.Count)
                throw new System.ArgumentOutOfRangeException(nameof(startIndex), "start must be within the bounds of the list.");
            if (sliceLength < 0 || startIndex + sliceLength > list.Count)
                throw new System.ArgumentOutOfRangeException(nameof(sliceLength), "count must be non-negative and within the bounds of the list.");

            this.list = list;
            this.startIndex = startIndex;
            this.sliceLength = sliceLength;
        }
        
        public int Count => list == null ? 0 : math.min(list.Count - startIndex, sliceLength);
        
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new System.ArgumentOutOfRangeException(nameof(index), "index must be within the bounds of the slice.");
                return list[startIndex + index];
            }
        }

        public void SetList(IReadOnlyList<T> newList, int newSliceLength)
        {
            list = newList ?? throw new System.ArgumentNullException(nameof(newList));
            startIndex = 0;
            SliceLength = newSliceLength;
        }
        
        #region Enumeration

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public Enumerator GetEnumerator() => new(this);

        public struct Enumerator : IEnumerator<T>
        {
            readonly ListSlice<T> listSlice;
            int _index;
            public Enumerator(ListSlice<T> listSlice)
            {
                this.listSlice = listSlice;
                _index = -1;
            }
            public bool MoveNext() => ++_index < listSlice.Count;
            public void Reset() => _index = -1;
            public T Current => _index >= 0 && _index < listSlice.Count
                ? listSlice[_index]
                : default;
            object IEnumerator.Current => Current;
            public void Dispose() { }
        }
        
        #endregion
    }
}