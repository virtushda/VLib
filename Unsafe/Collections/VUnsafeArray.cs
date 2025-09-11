#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define EXTRA_SAFETY
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace VLib
{
    /// <summary> A simple array-like type that acts like a NativeArray, without the safety handle restrictions. <br/>
    /// The indexer returns a ref, like 'Array' type. <br/>
    /// Same performance (high) and copy-safety(low) as <see cref="UnsafeList{T}"/> </summary>
    public struct VUnsafeArray<T> : IDisposable, IEnumerable<T>
        where T : unmanaged
    {
        UnsafeList<T> data;
        
        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (!data.IsCreated)
                    return false;
#if EXTRA_SAFETY
                if (data.Length != data.Capacity)
                {
                    UnityEngine.Debug.LogError("Length != Capacity!");
                    return false;
                }
#endif
                return true;
            }
        }

        public readonly int Length => data.Capacity;
        
        public VUnsafeArray(int capacity, Allocator allocator)
        {
            data = new UnsafeList<T>(capacity, allocator);
            data.Resize(capacity, NativeArrayOptions.ClearMemory);
        }

        public void Dispose() => data.Dispose();

        public ref T this[int index] => ref data.ElementAt(index);
        
        public Enumerator GetEnumerator() => new(this);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        public struct Enumerator : IEnumerator<T>
        {
            VUnsafeArray<T> array;
            int index;

            public Enumerator(VUnsafeArray<T> array)
            {
                this.array = array;
                index = -1;
            }
            
            public void Dispose() => array = default;
            
            public bool MoveNext() => ++index < array.Length;

            public void Reset() => index = -1;

            public T Current => array[index];
            public ref T CurrentRef => ref array[index];
            object IEnumerator.Current => Current;
        }

        // If we need these sorts of methods, should look at implementing IVLibUnsafeContainer to get the extensions
        //public T[] ToArray()
    }
}