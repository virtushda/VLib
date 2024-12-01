// NOTE: Disabled for now, don't need this yet and it's complex and heavy.

/*using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace VLib
{
    /// <summary> A variant of <see cref="VUnsafeBufferArray{T}"/> that uses a chunking approach to allow for collection growth. <br/>
    /// This type does NOT support SafePtr renting however, as safety cannot be guaranteed nicely. (this could change) </summary>
    public struct VUnsafeBufferList<T> : IAllocating, INativeList<T>, IEnumerable<T> // Used by collection initializers.
        where T : unmanaged
    {
        struct Data
        {
            byte signature;
            const byte CorrectCreatedSignature = 0b10101010;
            
            int chunkSize;
            UnsafeList<VUnsafeBufferArray<T>> arrays;
            bool logStillActiveOnDispose;

            int totalCount;
            /// <summary> A cached hint value to help find an open chunk as fast as possible. </summary>
            int openChunkIndex;
            
            public bool IsCreated => signature == CorrectCreatedSignature;
            
            public int Count => totalCount;

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
            public void ConditionalCheckCreated()
            {
                if (!IsCreated)
                    throw new InvalidOperationException("Data is not created.");
            }
            
            public Data(int initialCapacity, Allocator allocator, int chunkSize, bool logStillActiveOnDispose)
            {
                BurstAssert.TrueCheap(initialCapacity > 0);
                this.chunkSize = chunkSize;
                this.logStillActiveOnDispose = logStillActiveOnDispose;
                arrays = new UnsafeList<VUnsafeBufferArray<T>>(chunkSize, allocator);
                
                totalCount = 0;
                openChunkIndex = 0;
                
                signature = CorrectCreatedSignature;
            }

            /// <summary> Prefer Dispose by reference! </summary>
            public void Dispose()
            {
                if (!IsCreated)
                    return;
                
                foreach (var array in arrays)
                    array.Dispose();
                arrays.Dispose();
                signature = 0;
            }

            public int AddCompact(in T value)
            {
                ConditionalCheckCreated();
                var chunk = GetOpenChunk();
                var elementIndex = chunk.AddCompact(value);
                ++totalCount;
                return elementIndex;
            }

            public bool TryRemoveAt(int index, out T removedValue)
            {
                ConditionalCheckCreated();
                
                var chunkIndex = ElementIndexToChunkIndex(index);
                if (!TryGetChunk(chunkIndex, out var chunk))
                {
                    removedValue = default;
                    return false;
                }
                
                var chunkLocalIndex = ElementIndexToChunkLocal(index);
                if (!chunk.TryGetValue(chunkLocalIndex, out var elementValue))
                {
                    removedValue = default;
                    return false;
                }
                
                removedValue = elementValue;
                chunk.RemoveAtClear(chunkLocalIndex);

                // Update open chunk cache upon successful remove
                if (chunkIndex < openChunkIndex)
                    openChunkIndex = chunkIndex;
                --totalCount;
                
                return true;
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            int ElementIndexToChunkIndex(int elementIndex) => elementIndex / chunkSize;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            int ElementIndexToChunkLocal(int elementIndex) => elementIndex % chunkSize;

            bool TryGetChunk(int chunkIndex, out VUnsafeBufferArray<T> outArray)
            {
                if (chunkIndex < 0 || chunkIndex >= arrays.Length)
                {
                    outArray = default;
                    return false;
                }
                outArray = arrays[chunkIndex];
                return true;
            }

            VUnsafeBufferArray<T> GetOpenChunk()
            {
                // Try to use cached, fastest
                var openChunk = arrays[openChunkIndex];
                if (!openChunk.IsFull)
                    return openChunk;
                
                // Linear chunk search, slower, but helps pack data toward the front
                if (TrySearchForOpenChunk(out var array, out var index))
                {
                    openChunkIndex = index;
                    return array;
                }
                
                // No open chunks, create new
                var newArray = new VUnsafeBufferArray<T>(chunkSize, false, Allocator.Persistent, logStillActiveOnDispose);
                arrays.Add(newArray);
                openChunkIndex = arrays.Length - 1;
                return newArray;
            }

            bool TrySearchForOpenChunk(out VUnsafeBufferArray<T> outArray, out int arrayIndex)
            {
                for (var i = 0; i < arrays.Length; i++)
                {
                    var array = arrays[i];
                    if (!array.IsFull)
                    {
                        outArray = array;
                        arrayIndex = i;
                        return true;
                    }
                }

                outArray = default;
                arrayIndex = -1;
                return false;
            }
        }

        VUnsafeRef<Data> data;
        ref Data DataRef => ref data.ValueRef;

        public bool IsCreated => data.IsCreated;
        
        public VUnsafeBufferList(int initialCapacity, Allocator allocator, int chunkSize = -1, bool logStillActiveOnDispose = false)
        {
            if (chunkSize < 1)
                chunkSize = initialCapacity;
            
            data = new VUnsafeRef<Data>(new (initialCapacity, allocator, chunkSize, logStillActiveOnDispose), allocator);
        }
        
        public void Dispose()
        {
            if (!IsCreated)
                return;
            DataRef.Dispose();
        }
    }
}*/