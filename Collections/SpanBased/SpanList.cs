using System;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using VLib;

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
    }
    
    public static class SpanListExtensions
    {
        /// <summary> Be warned that stack allocations MUST be quite small! </summary>
        public static SpanList<T> AsList<T>(in this Span<T> span) where T : unmanaged => new(span);

        public static long MemoryUseBytes<T>(in this SpanList<T> list) where T : unmanaged => list.span.Length * UnsafeUtility.SizeOf<T>();
    }
}