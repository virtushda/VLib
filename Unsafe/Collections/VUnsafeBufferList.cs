using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using VLib.Unsafe.Utility;

namespace VLib
{
    /// <summary> Low-level list type. Compacts new additions, doesn't move memory around on remove. <br/>
    /// NOT copy safe! </summary>
    public struct VUnsafeBufferList<T> : IAllocating, INativeList<T>, IEnumerable<T> // Used by collection initializers.
        where T : unmanaged
    {
        UnsafeList<T> listData;
        VPackedIndexProvider packedIndices;
        UnsafeList<bool> indicesActive;
        int count;

        public readonly UnsafeList<T> ListDataUnsafe
        {
            get
            {
                this.ConditionalCheckIsCreated();
                return listData;
            }
        }

        public readonly bool IsCreated => listData.IsCreated;

        /// <summary> Number of actual active indices. </summary>
        public readonly int Count => count;

        /// <summary> Buffer range within which all active indices reside. </summary>
        public int Length
        {
            readonly get
            {
                this.ConditionalCheckIsCreated();
                return listData.Length;
            }
            set => Resize(value);
        }

        /// <summary> Currently available memory. </summary>
        public int Capacity
        {
            readonly get
            {
                this.ConditionalCheckIsCreated();
                return listData.Capacity;
            }
            set => Resize(value);
        }
        
        public bool IsEmpty => count == 0;
        public bool IsFull => count == Capacity;

        public VUnsafeBufferList(int capacity, Allocator allocator, bool logStillActiveOnDispose = false)
        {
            listData = new UnsafeList<T>(capacity, allocator, NativeArrayOptions.ClearMemory);
            packedIndices = VPackedIndexProvider.Create(reportAllNotDisposed: logStillActiveOnDispose);
            indicesActive = new(capacity, allocator);
            indicesActive.Resize(capacity, NativeArrayOptions.ClearMemory); // Ensure memory is all initialized to false
            count = 0;
        }

        public void Dispose()
        {
            if (!IsCreated)
                return;
            packedIndices.Dispose();
            listData.Dispose();
            indicesActive.Dispose();
            count = 0;
        }
        
        /// <summary> Tells you whether the index is inside the buffer range or not, without care to whether the index is considered 'active'. </summary>
        public readonly bool IndexInBufferRange(int index)
        {
            return index < Length && index >= 0; // Length property checks IsCreated
        }

        /// <summary> Tells you whether the index is inside the buffer range and is considered 'active'. </summary>
        public readonly bool IndexActive(int index)
        {
            if (!IndexInBufferRange(index)) // Rely on conditional check inside this call
                return false;
            return indicesActive[index];
        }
        
        /// <summary> The element at a given index. </summary>
        /// <param name="index">An index into this list.</param>
        /// <value>The value to store at the `index`.</value>
        /// <exception cref="IndexOutOfRangeException">Thrown if `index` is out of bounds.</exception>
        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                this.ConditionalCheckIsCreated();
                ConditionalCheckIndexActive(index);
                return listData[index];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                this.ConditionalCheckIsCreated();
                ConditionalCheckIndexActive(index);
                listData[index] = value;
            }
        }

        /// <summary> Returns a reference to the element at an index. </summary>
        /// <param name="index">An index.</param>
        /// <returns>A reference to the element at the index.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if index is out of bounds.</exception>
        public ref T ElementAt(int index)
        {
            ConditionalCheckIndexActive(index); // Checks isCreated
            return ref listData.ElementAt(index);
        }

        /// <summary> Accesses an index whether it's active or not. Use carefully. </summary>
        public ref T ElementAtUnsafe(int index)
        {
            this.ConditionalCheckIsCreated();
            ConditionalCheckIndexValid(index);
            return ref listData.ElementAt(index);
        }

        public readonly bool TryGetValue(int index, out T value)
        {
            if (IndexActive(index))
            {
                value = this[index];
                return true;
            }
            value = default;
            return false;
        }

        /// <summary> Can access inactive indices. </summary>
        public readonly bool TryGetValueUnsafe(int index, out T value)
        {
            if (IndexInBufferRange(index))
            {
                value = listData[index];
                return true;
            }
            value = default;
            return false;
        }

        public ref T TryGetRef(int index, out bool hasRef)
        {
            if (IndexActive(index))
            {
                hasRef = true;
                return ref listData.ElementAt(index);
            }
            hasRef = false;
            return ref VUnsafeUtil.NullRef<T>();
        }

        /// <summary> Can access inactive indices. </summary>
        public ref T TryGetRefUnsafe(int index, out bool hasRef)
        {
            if (IndexInBufferRange(index))
            {
                hasRef = true;
                return ref listData.ElementAt(index);
            }
            hasRef = false;
            return ref VUnsafeUtil.NullRef<T>();
        }

        public ref readonly T TryGetRefReadonly(int index, out bool hasRef) => ref TryGetRef(index, out hasRef);

        /// <summary> Can access inactive indices. </summary>
        public ref readonly T TryGetRefReadonlyUnsafe(int index, out bool hasRef) => ref TryGetRefUnsafe(index, out hasRef);

        public int PeekUnusedIndex()
        {
            this.ConditionalCheckIsCreated();
            return packedIndices.PeekNext();
        }

        /// <summary> Add a value, using a free index inside the list's range, if possible, otherwise expanding the list </summary>
        /// <returns>The index of the added value.</returns>
        public int AddCompact(in T value)
        {
            this.ConditionalCheckIsCreated();
            return AddCompactInternal(value);
        }

        /// <summary> Tries to add if there is currently space, but will not resize. </summary>
        public bool TryAddCompactNoResize(in T value, out int index)
        {
            if (IsFull) // This line checks IsCreated
            {
                index = -1;
                return false;
            }
            index = AddCompactInternal(value);
            return true;
        }

        int AddCompactInternal(in T value)
        {
            int index = ClaimNextIndex();
            listData[index] = value;
            return index;
        }

        /// <summary> Will expand list to fit incoming index. </summary>
        public bool TryAddAtIndex(int index, T value, bool allowWriteToActive = true)
        {
            this.ConditionalCheckIsCreated();
            if (!allowWriteToActive && IndexActive(index))
                return false;

            EnsureClaimedAndActive(index);
            // Write value
            listData[index] = value;
            return true;
        }

        public int ClaimNextIndex()
        {
            this.ConditionalCheckIsCreated();
            var index = packedIndices.FetchIndex();
            EnsureMinLength(index + 1);
            SetActive(index, true);
            return index;
        }

        /// <returns>True if index claimed by this method (increases count). False if already was claimed.</returns>
        public bool EnsureClaimedAndActive(int index)
        {
            this.ConditionalCheckIsCreated();
            // Ensure list capacity and length
            if (Length <= index)
                Resize(index + 1);
            bool indexTaken = packedIndices.TryClaimIndex(index);
            SetActive(index, true);
            return indexTaken;
        }

        /// <summary> RemoveAt, but doesn't shift memory in any way. <br/>
        /// Disables the index, writes default to it and returns it to the pool. </summary>
        public void RemoveAtClear(int index, in T defaultValue = default)
        {
            ConditionalCheckIndexActive(index); // Relies on internall IsCreated check
            
            SetActive(index, false);
            listData[index] = defaultValue;
            
            // If we removed the last active element, we need to correct the length to fit the active range
            if (listData.Length == index + 1)
            {
                // If the first index, no backscan
                if (index == 0)
                    Length = 0;
                else
                {
                    // Backscan
                    bool found = false;
                    for (int i = index - 1; i >= 0; i--)
                    {
                        if (indicesActive[i])
                        {
                            Length = i + 1;
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        Length = 0;
                }
            }

            packedIndices.ReturnIndex(index);
        }

        /// <returns>True if the index active state was changed, false if it was already set to the desired state.</returns>
        bool SetActive(int index, bool active)
        {
            ref var indexRef = ref indicesActive.ElementAt(index);
            if (indexRef == active)
                return false;
            indexRef = active;
            if (active)
            {
                ++count;
                BurstAssert.False(count > Length);
            }
            else
            {
                --count;
                BurstAssert.False(count < 0);
            }
            return true;
        }

        public void EnsureMinLength(int newLength)
        {
            this.ConditionalCheckIsCreated();
            if (Length < newLength)
                Resize(newLength);
        }

        public void Resize(int newLength)
        {
            this.ConditionalCheckIsCreated();
            
            // If shrinking, need to maintain count
            if (newLength < Length)
                DisableRange(newLength, Length);

            listData.Resize(newLength, NativeArrayOptions.ClearMemory);
            indicesActive.Resize(newLength, NativeArrayOptions.ClearMemory);
        }

        void DisableRange(int start, int end)
        {
            for (int i = start; i < end; i++)
                SetActive(i, false);
        }

        public void Clear()
        {
            this.ConditionalCheckIsCreated();
            // Return all indices
            for (int i = 0; i < indicesActive.Length; i++)
                indicesActive[i] = false;
            packedIndices.ResetClear();

            // Clear data
            listData.Clear();
            count = 0;
        }
        
        #region Enumeration
        
        /// <summary> Returns an enumerator over the elements of this list. </summary>
        /// <returns>An enumerator over the elements of this list.</returns>
        public Enumerator GetEnumerator() => new(this);

        /// <summary> This method is not implemented. Use <see cref="GetEnumerator"/> instead. </summary>
        /// <returns>Throws NotImplementedException.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary> This method is not implemented. Use <see cref="GetEnumerator"/> instead. </summary>
        /// <returns>Throws NotImplementedException.</returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<T>
        {
            VUnsafeBufferList<T> list;
            int index;

            public Enumerator(VUnsafeBufferList<T> list)
            {
                BurstAssert.True(list.IsCreated);
                this.list = list;
                index = -1;
            }
            
            public int CurrentIndex => index;

            /// <summary> The current ACTIVE element in the buffer. </summary>
            public T Current => list[index];
            public ref T CurrentRef => ref list.ElementAt(index);

            object IEnumerator.Current => Current;

            public void Dispose() { }

            public bool MoveNext()
            {
                // Find the next active index
                for (int i = index + 1; i < list.Length; ++i)
                {
                    if (list.IndexActive(i))
                    {
                        index = i;
                        return true;
                    }
                }
                return false;
            }

            public void Reset() => index = -1;
        }
        
        #endregion
        
        /// <summary> Checks: Created, Index in Range </summary>
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public readonly void ConditionalCheckIndexValid(int index)
        {
            if (!IndexInBufferRange(index)) // Rely on conditional IsCreated check inside this call
                throw new IndexOutOfRangeException($"Index {index} is out of range in VUnsafeBufferList of '{Length}' Length.");
        }

        /// <summary> Checks: Created, Index in Range, Index Active </summary>
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public readonly void ConditionalCheckIndexActive(int index)
        {
            if (!IndexActive(index)) // Rely on conditional IsCreated check inside this call
                throw new IndexOutOfRangeException($"Index {index} is not active in VUnsafeBufferList.");
        }
    }
}