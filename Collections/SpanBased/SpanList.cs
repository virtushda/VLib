using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace VLib
{
    public ref struct SpanList<T>
        where T : unmanaged
    {
        public Span<T> span;
        int count;

        public int Count
        {
            get => count;
            set
            {
                VCollectionUtils.ConditionalCheckLengthValid(value, span.Length);
                count = value;
            }
        }

        public int Capacity => span.Length;

        public ref T this[int index]
        {
            get
            {
                VCollectionUtils.ConditionalCheckIndexValid(index, count);
                return ref span[index];
            }
        }

        public SpanList(Span<T> span)
        {
            this.span = span;
            count = 0;
            
#if DEBUG
            // Complain if a stack alloc is over 1KB!
            var bytesUsed = this.MemoryUseBytes();
            if (bytesUsed > 1000)
            {
                bool logged = false;
                LogAllocationSizeError(bytesUsed, ref logged);
                if (!logged)
                    Debug.LogError($"Stack allocation of SpanList is over 1KB: {bytesUsed} bytes!");
            }
#endif
        }

        public int Add(in T item)
        {
            VCollectionUtils.ConditionalCheckIndexValid(count, span.Length);
            span[count] = item;
            return count++;
        }

        public void RemoveAt(int index)
        {
            VCollectionUtils.ConditionalCheckIndexValid(index, count);
            // If not last index, we need to shift memory
            if (index < count - 1)
            {
                var memoryPastIndex = span.Slice(index + 1, count - index - 1);
                memoryPastIndex.CopyTo(span.Slice(index));
            }
            --count;
        }
        
        public void Clear() => count = 0;
        
        [BurstDiscard]
        void LogAllocationSizeError(long bytesUsed, ref bool logged)
        {
            Debug.LogError($"Stack allocation of SpanList is over 1KB: {bytesUsed.AsDataSizeInBytesToReadableString()}");
            logged = true;
        }

        public ref struct Enumerator
        {
            private readonly Span<T> _span;
            private int _index;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(in SpanList<T> list)
            {
                _span = list.span.Slice(0, list.Count);
                _index = -1;
            }

            public bool MoveNext()
            {
                int index = _index + 1;
                if ((uint)index < (uint)_span.Length)
                {
                    _index = index;
                    return true;
                }
                return false;
            }

            public ref readonly T Current => ref _span[_index];

            public void Reset() => _index = -1;

            public void Dispose() { }
        }

        public Enumerator GetEnumerator() => new Enumerator(in this);
    }
    
    public static class SpanListExtensions
    {
        /// <summary> Be warned that stack allocations MUST be quite small! </summary>
        public static SpanList<T> AsList<T>(in this Span<T> span) where T : unmanaged => new(span);

        public static long MemoryUseBytes<T>(in this SpanList<T> list) where T : unmanaged => list.span.Length * UnsafeUtility.SizeOf<T>();
    }
}