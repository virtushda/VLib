using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

namespace VLib.Collections
{
    /// <summary> Very similar to <see cref="List{T}"/> except that it's backed by a pooled array, so you cannot add beyond capacity! </summary>
    public readonly struct PooledObjectList<T> : IDisposable, IReadOnlyList<T>
        where T : class
    {
        internal readonly PooledObjectListObj pooledList;
        internal readonly long id;

        public bool IsValid => pooledList != null && id == pooledList.id;
        public static implicit operator bool(in PooledObjectList<T> list) => list.IsValid;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PooledObjectList(PooledObjectListObj pooledList, long id)
        {
            this.pooledList = pooledList;
            this.id = id;
        }

        [BurstDiscard]
        public void Dispose()
        {
            if (!PooledObjectListPool.Return(this))
            {
                // Only log if struct was initialized in the first place
                if (pooledList != null)
                    UnityEngine.Debug.LogError("Trying to dispose a SharedObjectList with wrong ID!");
            }
        }

        public T this[int index]
        {
            get
            {
                AssertValidIndex(index);
                var objectAtIndex = pooledList.objects[index];
                // Don't try to cast a null, just return it
                if (objectAtIndex == null)
                    return null;
                AssertTypeValid(objectAtIndex);
                // Faster cast, guarded with an editor-only check
                return UnsafeUtility.As<object, T>(ref objectAtIndex);
                //return (T)pooledList.Objects[index];
            }
            set
            {
                AssertValidIndex(index);
                pooledList.objects[index] = value;
            }
        }

        public int Count
        {
            get
            {
                AssertValidID();
                return pooledList.Count;
            }
            set
            {
                AssertValidID();
                if (value < 0 || value > pooledList.Objects.Length)
                    throw new ArgumentOutOfRangeException($"Count {value} is out of range for {pooledList.Objects.Length}!");
                pooledList.count = value;
            }
        }

        /// <summary> Gets the count of elements if valid, or zero if the list is not valid. </summary>
        public int CountSafe => IsValid ? Count : 0;

        public int Capacity
        {
            get
            {
                AssertValidID();
                return pooledList.objects.Length;
            }
        }

        public bool Add(T obj)
        {
            AssertValidID();
            if (Count >= Capacity)
                return false;
            pooledList.objects[pooledList.count++] = obj;
            return true;
        }
        
        public void RemoveAtSwapback(int index)
        {
            AssertValidIndex(index);
            var lastIndex = --pooledList.count;
            if (index >= lastIndex)
            {
                pooledList.objects[index] = null;
            }
            else
            {
                pooledList.objects[index] = pooledList.objects[lastIndex];
                pooledList.objects[lastIndex] = null;
            }
        }
        
        public void Clear()
        {
            AssertValidID();
            for (var i = 0; i < pooledList.count; i++)
                pooledList.objects[i] = null;
            pooledList.count = 0;
        }

        /// <summary> Safely attempt to get a typed value. </summary>
        /// <returns> True if the index is valid and the value is of the correct type. <br/>
        /// True also if the value was null. <br/>
        /// False if the index was out of range. <br/>
        /// False also if the value was of the wrong type. </returns>
        public bool TryGetValue(int index, out T value)
        {
            if ((uint)index >= Count)
            {
                value = default;
                return false;
            }
            
            object obj = pooledList.objects[index];
            
            if (obj != null)
            {
                if (obj is T objOfType)
                    value = objOfType;
                else
                {
                    // Found another type of object???
                    value = default;
                    return false;
                }
            }
            else
                value = null; // Storing null is legitimate, so don't try to cast it.

            return true;
        }

        /// <summary>
        /// Returns an enumerator over the elements of this list.
        /// </summary>
        /// <returns>An enumerator over the elements of this list.</returns>
        public Enumerator GetEnumerator() => new(this);
        /// <summary>
        /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
        /// </summary>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        /// <summary>
        /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
        /// </summary>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        
        public struct Enumerator : IEnumerator<T>
        {
            readonly PooledObjectList<T> list;
            int index;
            readonly bool valid;

            public Enumerator(PooledObjectList<T> list)
            {
                valid = list.IsValid;
                if (!list.IsValid)
                {
                    this = default;
                    return;
                }
                
                this.list = list;
                index = -1;
            }
            
            public void Dispose() { }

            public T Current => list[index];

            object IEnumerator.Current => Current;

            public bool MoveNext() => valid && ++index < list.Count;

            public void Reset() => index = -1;
        }

        /// <summary> Renting or Returning/Disposing is thread-safe. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PooledObjectList<T> Rent(int minimumLength) => PooledObjectListPool.Rent<T>(minimumLength);

        [Conditional("UNITY_EDITOR"), MethodImpl(MethodImplOptions.AggressiveInlining)]
        void AssertTypeValid(object obj)
        {
            if (obj != null && obj is not T)
                throw new ArgumentException($"Object is not of type {typeof(T)}!");
        }
        
        [Conditional("UNITY_EDITOR"), MethodImpl(MethodImplOptions.AggressiveInlining)]
        void AssertValidIndex(int index)
        {
            AssertValidID();
            if ((uint)index >= Count)
                throw new IndexOutOfRangeException($"Index {index} is out of range for {Count}!");
        }
        
        [Conditional("UNITY_EDITOR"), MethodImpl(MethodImplOptions.AggressiveInlining)]
        void AssertValidID()
        {
            if (pooledList == null)
                throw new InvalidOperationException("SharedObjectList is not initialized!");
            if (pooledList.id == 0)
                throw new InvalidOperationException("SharedObjectList has been disposed!");
        }

        [Conditional("UNITY_EDITOR"), MethodImpl(MethodImplOptions.AggressiveInlining)]
        void AssertValidCount()
        {
            if (Count > pooledList.Objects.Length)
                throw new InvalidOperationException($"Count {Count} is out of range for {pooledList.Objects.Length}!");
        }
    }
}