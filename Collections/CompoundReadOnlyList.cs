using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace VLib
{
    public class CompoundReadOnlyList<T> : IEnumerable<T>
    {
        IReadOnlyList<IReadOnlyList<object>> lists;

        public CompoundReadOnlyList(IReadOnlyList<IReadOnlyList<object>> listOfLists)
        {
            lists = listOfLists;
        }
        
        public CompoundReadOnlyList(params IReadOnlyList<object>[] listOfLists)
        {
            lists = listOfLists;
        }

        public int Count
        {
            get
            {
                int count = 0;
                for (int i = 0; i < lists.Count; i++)
                    count += lists[i].Count;
                return count;
            }
        }

        public T this[int index]
        {
            get
            {
                //Step to each list and check the range
                int startIndex = 0;
                for (int i = 0; i < lists.Count; i++)
                {
                    var list = lists[i];
                    var countRelative = startIndex + list.Count;
                    if (index >= countRelative)
                    {
                        startIndex += list.Count;
                        continue;
                    }

                    return (T)list[index - startIndex];
                }

                throw new IndexOutOfRangeException($"Index '{index}' is out of valid range [0 - {Count - 1}]");
            }
        }

        public Enumerator GetEnumerator() => new Enumerator { list = this };
        
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw new NotImplementedException();
        
        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
        
        // TODO: Optimize this by allowing it to walk through the list enumerators or something, each index fetch is slower than needbe...
        public struct Enumerator : IEnumerator<T>
        {
            internal CompoundReadOnlyList<T> list;
            internal int index;

            /// <summary> Does nothing. </summary>
            public void Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next element of the list.
            /// </summary>
            /// <remarks>
            /// The first `MoveNext` call advances the enumerator to the first element of the list. Before this call, `Current` is not valid to read.
            /// </remarks>
            /// <returns>True if `Current` is valid to read after the call.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++index < list.Count;

            /// <summary>
            /// Resets the enumerator to its initial state.
            /// </summary>
            public void Reset() => index = -1;

            /// <summary>
            /// The current element.
            /// </summary>
            /// <value>The current element.</value>
            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => list[index];
            }

            object IEnumerator.Current => Current;
        }
    }
}