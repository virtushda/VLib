using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace VLib
{
    public class CircularBufferArray<T> : IList<T>, IReadOnlyList<T>
    {
        T[] buffer;
        int head;
        int count;

        public CircularBufferArray(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            buffer = new T[capacity];
        }

        public int Capacity
        {
            get => buffer.Length;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value));
                if (value == Capacity)
                    return;

                var newBuffer = new T[value];
                var itemsToCopy = Math.Min(count, value);
                for (var i = 0; i < itemsToCopy; i++)
                    newBuffer[i] = this[i];

                buffer = newBuffer;
                head = 0;
                count = itemsToCopy;
            }
        }

        public int Count => count;
        public bool IsReadOnly => false;
        public int LastIndex => count - 1;

        public T this[int index]
        {
            get
            {
                if ((uint)index >= count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return buffer[(head + index) % Capacity];
            }
            set
            {
                if ((uint)index >= count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                buffer[(head + index) % Capacity] = value;
            }
        }
        
        public ref T ElementAt(int index)
        {
            if ((uint)index >= count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return ref buffer[(head + index) % Capacity];
        }

        public void Add(T item)
        {
            if (count == Capacity)
            {
                buffer[head] = item;
                head = (head + 1) % Capacity;
            }
            else
            {
                buffer[(head + count) % Capacity] = item;
                count++;
            }
        }

        public void Clear()
        {
            Array.Clear(buffer, 0, Capacity);
            head = 0;
            count = 0;
        }

        public bool Contains(T item)
        {
            return IndexOf(item) != -1;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            for (var i = 0; i < count; i++)
                array[arrayIndex + i] = this[i];
        }

        public int IndexOf(T item)
        {
            var comparer = EqualityComparer<T>.Default;
            for (var i = 0; i < count; i++)
            {
                if (comparer.Equals(this[i], item))
                    return i;
            }
            return -1;
        }

        public void Insert(int index, T item)
        {
            throw new NotSupportedException("Insert is not supported in a circular buffer.");
        }

        public bool Remove(T item)
        {
            var index = IndexOf(item);
            if (index == -1)
                return false;
            RemoveAt(index);
            return true;
        }

        public void RemoveAt(int index)
        {
            if ((uint)index >= count)
                throw new ArgumentOutOfRangeException(nameof(index));
            for (var i = index; i < count - 1; i++)
                this[i] = this[i + 1];
            this[count - 1] = default!;
            count--;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(T item) => Add(item);

        public T Dequeue()
        {
            if (count == 0)
                throw new InvalidOperationException("The buffer is empty.");
            var item = buffer[head];
            head = (head + 1) % Capacity;
            --count;
            return item;
        }
        
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);
        public Enumerator GetEnumerator() => new(this);
        
        public ReverseEnumerator GetReverseEnumerator() => new(this);
        
        public struct Enumerator : IEnumerator<T>
        {
            readonly CircularBufferArray<T> buffer;
            readonly int capacity;
            readonly int endIndex;
            int logicalIndex;
            int physicalIndex;
            
            internal Enumerator(CircularBufferArray<T> buffer)
            {
                this.buffer = buffer;
                capacity = buffer.Capacity;
                endIndex = buffer.count;
                logicalIndex = -1;
                physicalIndex = -1;
            }
            
            public T Current => buffer.buffer[physicalIndex];
            object IEnumerator.Current => Current!;
            
            public ref T CurrentRef => ref buffer.buffer[physicalIndex];
            
            public bool MoveNext()
            {
                logicalIndex++;
                if (logicalIndex >= endIndex)
                    return false;
                
                physicalIndex = buffer.head + logicalIndex;
                if (physicalIndex >= capacity)
                    physicalIndex -= capacity;
                
                return true;
            }
            
            public void Reset()
            {
                logicalIndex = -1;
                physicalIndex = -1;
            }
            
            public void Dispose() { }
        }
        
        public struct ReverseEnumerator : IEnumerator<T>
        {
            readonly CircularBufferArray<T> buffer;
            readonly int capacity;
            int logicalIndex;
            int physicalIndex;
            
            internal ReverseEnumerator(CircularBufferArray<T> buffer)
            {
                this.buffer = buffer;
                capacity = buffer.Capacity;
                logicalIndex = buffer.count;
                physicalIndex = -1;
            }
            
            public T Current => buffer.buffer[physicalIndex];
            object IEnumerator.Current => Current!;
            
            public ref T CurrentRef => ref buffer.buffer[physicalIndex];
            
            public bool MoveNext()
            {
                logicalIndex--;
                if (logicalIndex < 0)
                    return false;
                
                physicalIndex = buffer.head + logicalIndex;
                if (physicalIndex >= capacity)
                    physicalIndex -= capacity;
                
                return true;
            }
            
            public void Reset()
            {
                logicalIndex = buffer.count;
                physicalIndex = -1;
            }
            
            public void Dispose() { }
        }
    }
}