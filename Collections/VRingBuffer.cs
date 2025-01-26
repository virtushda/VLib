/*using System;
using System.Collections;
using System.Collections.Generic;

namespace VLib.Collections
{
    public class VRingBuffer<T>// : IList<T>
    {
        T[] internalArray;
        int head;
        int tail;
        readonly bool isManagedType;
        
        public T[] ArrayUnsafe => internalArray;
        public int Count => length;
        public bool TypeIsManaged => isManagedType;
        
        public bool IsReadOnly => false;

        public int Capacity
        {
            get => internalArray.Length;
            set
            {
                // If shrinking, trim length first to avoid out of bounds exceptions.
                if (value < length)
                    length = value;
                // We'll rely on resize to handle the rest.
                Array.Resize(ref internalArray, value);
            }
        }

        public T this[int index]
        {
            get
            {
                VCollectionUtils.ConditionalCheckIndexValid(index, length);
                return internalArray[index];
            }
            set
            {
                VCollectionUtils.ConditionalCheckIndexValid(index, length);
                internalArray[index] = value;
            }
        }

        public VList(int capacity = 4)
        {
            internalArray = new T[capacity];
            length = 0;
            isManagedType = !typeof(T).IsValueType;
        }

        public void Add(T element) => AddIndexed(element);
        
        public int AddIndexed(in T element)
        {
            EnsureLength(length + 1);
            internalArray[length] = element;
            return length++;
        }

        public int AddNoResize(in T element)
        {
            VCollectionUtils.ConditionalCheckIndexValid(length, internalArray.Length);
            internalArray[length] = element;
            return length++;
        }
        
        public void Insert(int index, T element)
        {
            VCollectionUtils.ConditionalCheckIndexValid(index, length + 1); // Allow inserting at the end.
            EnsureLength(length + 1);
            // Shift elements up to make room.
            if (index < length)
                Array.Copy(internalArray, index, internalArray, index + 1, length - index);
            ++length;
            internalArray[index] = element;
        }
        
        public bool Contains(T element) => IndexOf(element) != -1;

        public int IndexOf(T element)
        {
            for (int i = 0; i < length; i++)
            {
                if (internalArray[i].Equals(element))
                    return i;
            }
            return -1;
        }

        /// <summary> The slowest remove method because it must linearly search and potentially box elements to find the element to remove. </summary>
        public bool Remove(T element)
        {
            for (int i = 0; i < length; i++)
            {
                if (internalArray[i].Equals(element))
                {
                    RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary> Remove a given element and shift all following elements back in memory. If order is not important, prefer <see cref="RemoveAtSwapback"/>. </summary>
        public void RemoveAt(int index)
        {
            VCollectionUtils.ConditionalCheckIndexValid(index, length);
            // Shift elements down to fill the gap.
            // Array.Copy uses memmove under the hood, very fast.
            Array.Copy(internalArray, index + 1, internalArray, index, length - index - 1);
            --length;
        }

        /// <summary> The highest performance remove method, but alters list order. </summary>
        public void RemoveAtSwapback(int index)
        {
            VCollectionUtils.ConditionalCheckIndexValid(index, length);
            // Swap the last element into the gap.
            var lastElementIndex = length - 1;
            internalArray[index] = internalArray[lastElementIndex];
            // Clear the last element.
            internalArray[lastElementIndex] = default;
            --length;
        }
        
        public void EnsureLength(int newLength)
        {
            if (newLength > internalArray.Length)
                Array.Resize(ref internalArray, internalArray.Length * 2);
        }
        
        public void Clear()
        {
            var previousLength = length;
            length = 0;
            
            // If we're working with managed types, we need to clear references for the GC.
            if (isManagedType)
                Array.Clear(internalArray, 0, previousLength);
        }
        
        public T GetOrDefault(int index)
        {
            if (VCollectionUtils.IndexIsValid(index, length))
                return internalArray[index];
            return default;
        }
        
        public bool TryGet(int index, out T element)
        {
            if (VCollectionUtils.IndexIsValid(index, length))
            {
                element = internalArray[index];
                return true;
            }
            element = default;
            return false;
        }

        /// <summary> Be careful holding an element ref, any operation which causes the <see cref="VList{T}"/> to resize will move the underlying memory. </summary>
        public ref T ElementAtUnsafe(int index)
        {
            VCollectionUtils.ConditionalCheckIndexValid(index, length);
            return ref internalArray[index];
        }
        
        /// <summary> Be careful holding an element ref, any operation which causes the <see cref="VList{T}"/> to resize will move the underlying memory. </summary>
        public ref T TryGetRefUnsafe(int index, out bool success)
        {
            if (!VCollectionUtils.IndexIsValid(index, length))
            {
                success = false;
                return ref internalArray[0];
            }
            success = true;
            return ref internalArray[index];
        }
        
        /// <summary> Be careful holding an element ref, any operation which causes the <see cref="VList{T}"/> to resize will move the underlying memory. </summary>
        public ref readonly T TryGetReadOnlyRefUnsafe(int index, out bool success) => ref TryGetRefUnsafe(index, out success);
        
        public void CopyTo(T[] array, int arrayIndex) => Array.Copy(this.internalArray, 0, array, arrayIndex, length);

        #region Enumeration

        public Enumerator GetEnumerator() => new(this);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);
        
        public struct Enumerator : IEnumerator<T>
        {
            VList<T> list;
            int index;
            
            public Enumerator(VList<T> list)
            {
                this.list = list;
                index = -1;
            }
            
            public bool MoveNext() => ++index < list.length;

            public void Reset() => index = -1;

            public T Current => list.internalArray[index];
            public ref T CurrentRef => ref list.internalArray[index];
            object IEnumerator.Current => Current;
            
            public void Dispose()
            {
                list = null;
            }
        }

        #endregion
        
        // Problem with this is that every other method would also need interlocking behaviour to absolutely ensure that this scope is respected.
        /*public struct RefScope : IDisposable
        {
            VList<T> list;
            
            public RefScope(VList<T> list)
            {
                this.list = list;
                Interlocked.Increment(ref list.freezeCount);
                BurstAssert.TrueCheap(list.freezeCount > 0);
            }
            
            public void Dispose()
            {
                Interlocked.Decrement(ref list.freezeCount);
                BurstAssert.TrueCheap(list.freezeCount >= 0);
            }
            
            /// <summary> Be careful holding an element ref, any operation which causes the <see cref="VList{T}"/> to resize will move the underlying memory. </summary>
            public ref T ElementAtUnsafe(int index)
            {
                VCollectionUtils.ConditionalCheckIndexValid(index, length);
                return ref internalArray[index];
            }
        
            /// <summary> Be careful holding an element ref, any operation which causes the <see cref="VList{T}"/> to resize will move the underlying memory. </summary>
            public ref T TryGetRefUnsafe(int index, out bool success)
            {
                if (!VCollectionUtils.IndexIsValid(index, length))
                {
                    success = false;
                    return ref internalArray[0];
                }
                success = true;
                return ref internalArray[index];
            }
        
            /// <summary> Be careful holding an element ref, any operation which causes the <see cref="VList{T}"/> to resize will move the underlying memory. </summary>
            public ref readonly T TryGetReadOnlyRefUnsafe(int index, out bool success) => ref TryGetRefUnsafe(index, out success);
        }#1#
    }

    /*public static class VListExt
    {
        /// <summary> A version of IndexOf that expects IEquatable. This version can ensure zero-boxing. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfEquatable<T>(this VList<T> list, T element) 
            where T : System.IEquatable<T>
        {
            return list.IndexOf(element);
        }
        
        /// <summary> A version of Contains that expects IEquatable. This version can ensure zero-boxing. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsEquatable<T>(this VList<T> list, T element) 
            where T : System.IEquatable<T>
        {
            return list.Contains(element);
        }

        /// <summary> A wrap of <see cref="VList{T}.Remove"/> that enforces IEquatable. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemoveSlowEquatable<T>(this VList<T> list, T element) 
            where T : System.IEquatable<T>
        {
            return list.Remove(element);
        }
    }#1#
}*/