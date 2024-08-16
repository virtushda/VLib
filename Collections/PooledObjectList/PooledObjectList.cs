using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace VLib.Libraries.VLib.Collections
{
    /// <summary> Very similar to <see cref="List{T}"/> except that it's backed by a pooled array, so you cannot add beyond capacity! </summary>
    public readonly struct PooledObjectList<T> : IDisposable, IReadOnlyList<T>
        where T : class
    {
        internal readonly PooledObjectListObj pooledList;
        internal readonly long id;

        public bool IsValid => pooledList != null && id == pooledList.id;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PooledObjectList(PooledObjectListObj pooledList, long id)
        {
            this.pooledList = pooledList;
            this.id = id;
        }

        public void Dispose()
        {
            if (IsValid)
                PooledObjectListPool.Return(this);
            else
                UnityEngine.Debug.LogError("Trying to dispose a SharedObjectList with wrong ID!");
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
                pooledList.count = (ushort) value;
            }
        }

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

        /*/// <summary> Thread-safe way to add </summary>
        public void AddParallel(T obj)
        {
            var index = Interlocked.Increment(ref pooledList.count) - 1;
            AssertValidCount();
            typedArray[index] = obj;
        }
        
        public void RemoveAtSwapback(int index)
        {
            AssertIndex(index);
            --pooledList.count;
            var lastIndex = pooledList.count;
            if (index >= lastIndex)
            {
                pooledList.objects[index] = null;
            }
            else
            {
                pooledList.objects[index] = pooledList.objects[lastIndex];
                pooledList.objects[lastIndex] = null;
            }
        }*/
        
        public void Clear()
        {
            AssertValidID();
            for (var i = 0; i < pooledList.count; i++)
                pooledList.objects[i] = null;
            pooledList.count = 0;
        }

        /// <summary>
        /// Returns an enumerator over the elements of this list.
        /// </summary>
        /// <returns>An enumerator over the elements of this list.</returns>
        public Enumerator GetEnumerator()
        {
            AssertValidID();
            return new Enumerator(this);
        }

        /// <summary>
        /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
        /// </summary>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();

        /// <summary>
        /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
        /// </summary>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw new NotImplementedException();
        
        public struct Enumerator
        {
            readonly PooledObjectList<T> list;
            int index;

            public Enumerator(PooledObjectList<T> list)
            {
                this.list = list;
                index = -1;
            }

            public T Current => list[index];

            public bool MoveNext() => ++index < list.Count;
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
            if (index < 0 || index >= Count)
                throw new IndexOutOfRangeException($"Index {index} is out of range for {Count}!");
        }
        
        [Conditional("UNITY_EDITOR"), MethodImpl(MethodImplOptions.AggressiveInlining)]
        void AssertValidID()
        {
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