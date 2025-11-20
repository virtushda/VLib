#if UNITY_EDITOR
#define INDEXER_PROFILING
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace VLib.Collections
{
    // NOTE: UNTESTED
    
    /// <summary> View of a set of array chunks as one contiguous array. Helps to avoid data duplication. </summary>
    public class CompoundArrayView<T> : IReadOnlyList<T>
    {
        public readonly T[][] chunks;

        /// <summary> The start index in the first chunk. </summary>
        public readonly int startIndex;
        /// <summary> The total number of elements in the view across all chunks. </summary>
        public readonly int totalCount;

        /*/// <summary> The length of the first chunk, accounting for <see cref="startIndex"/>. </summary>
        readonly int firstChunkLength;
        /// <summary> The length of the last chunk, accounting for <see cref="endOffset"/>. </summary>
        readonly int lastChunkLength;*/

        /// <summary> Create a view over multiple arrays. </summary>
        /// <param name="chunks"> The arrays to view. </param>
        /// <param name="startIndex"> The start index in the first chunk. </param>
        /// <param name="endTrimCount"> The number of elements to exclude from the end of the last chunk. </param>
        public CompoundArrayView(T[][] chunks, int startIndex, int endTrimCount)
        {
            if (chunks is not {Length: > 0})
                throw new ArgumentException("Chunks array is null or empty.", nameof(chunks));
            this.chunks = chunks;
            this.startIndex = startIndex;
            
            // Calculate total count
            totalCount = 0;
            for (var c = 0; c < chunks.Length; c++)
            {
                var chunk = chunks[c];
                if (chunk == null)
                    throw new ArgumentException($"Chunk is null at index '{c}'.", nameof(chunks));
                if (chunk.Length == 0)
                    throw new ArgumentException($"Chunk is empty at index '{c}'.", nameof(chunks));
                
                totalCount += chunk.Length;
            }

            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), $"Start index '{startIndex}' cannot be negative.");
            if (endTrimCount < 0)
                throw new ArgumentOutOfRangeException(nameof(endTrimCount), $"End offset '{endTrimCount}' cannot be negative.");
            if (startIndex >= chunks[0].Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex), $"Start index '{startIndex}' is greater than or equal to the length of the first chunk.");
            /*if (endTrimCount >= chunks[chunks.Length - 1].Length)
                throw new ArgumentOutOfRangeException(nameof(endTrimCount), $"End offset '{endTrimCount}' is beyond the length of the last chunk.");*/

            totalCount -= startIndex;
            totalCount -= endTrimCount;
            
            if (totalCount < 1)
            {
                throw new ArgumentOutOfRangeException($"MonthlyDataStreamer: CompoundArrayView created with no data. " +
                                                      $"Computed totalCount: {totalCount}" +
                                                      $"startIndex: {startIndex}, endTrimCount: {endTrimCount}, " +
                                                      $"firstChunkLength: {chunks[0].Length}, lastChunkLength: {chunks[chunks.Length - 1].Length} +" +
                                                      $"chunkCount: {chunks.Length}");
            }

            //firstChunkLength = chunks[0].Length - startIndex;
            //lastChunkLength = chunks[chunks.Length - 1].Length - endTrimCount;
        }

        public int Count => totalCount;

        // TODO: If not efficient enough, use caching mechanism or LUT
        public T this[int index] => ElementAtReadOnly(index);

        public ref readonly T ElementAtReadOnly(int index)
        {
#if INDEXER_PROFILING
            using var profileScope = ProfileScope.Auto();
#endif
            var chunk = GlobalIndexToChunkLocal(index, out var indexInChunk);
            if (chunk == null)
                throw new IndexOutOfRangeException($"Index '{index}' out of range: 0 - {totalCount - 1}");
            return ref chunk[indexInChunk];
        }

        /// <summary> Convert an index that expects [0] to match <see cref="startIndex"/>, into a chunk and chunk-local index. </summary>
        T[] GlobalIndexToChunkLocal(int globalIndex, out int chunkLocalIndex)
        {
#if INDEXER_PROFILING
            using var profileScope = ProfileScope.Auto();
#endif
            // Handle out of range before any offsets
            if (globalIndex < 0 || globalIndex >= totalCount)
            {
                chunkLocalIndex = -1;
                return null;
            }
            
            // Offset the global index to account for the startIndex
            var chunkAdjGlobalIndex = globalIndex + startIndex;
            
            // Walk chunks
            var chunksCount = chunks.Length;
            for (int i = 0; i < chunksCount; i++)
            {
                var chunk = chunks[i];
                if (chunkAdjGlobalIndex < chunk.Length)
                {
                    chunkLocalIndex = chunkAdjGlobalIndex;
                    return chunk;
                }
                chunkAdjGlobalIndex -= chunk.Length;
            }

            // Should never reach this point
            Debug.LogError("Failed to find chunk for index: " + globalIndex);
            chunkLocalIndex = -1;
            return null;
        }

        // Struct enumerator for allocation-free enumeration
        public struct Enumerator : IEnumerator<T>
        {
            readonly CompoundArrayView<T> compoundArrayView;
            int currentChunkIndex;
            int currentPositionInChunk;
            int totalItemsEnumerated;

            public Enumerator(CompoundArrayView<T> compoundArrayView)
            {
                this.compoundArrayView = compoundArrayView;
                currentChunkIndex = 0;
                currentPositionInChunk = compoundArrayView.startIndex - 1; // Start before first element
                totalItemsEnumerated = 0;
                Current = default;
            }

            public T Current { get; private set; }
            
            public ref T CurrentRef
            {
                get
                {
                    var currentChunk = compoundArrayView.chunks[currentChunkIndex];
                    return ref currentChunk[currentPositionInChunk];
                }
            }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
#if INDEXER_PROFILING
                using var profileScope = ProfileScope.Auto();
#endif
                var chunkCount = compoundArrayView.chunks.Length;
                Assert.IsFalse(chunkCount == 0);
                
                // Check if we've reached the total count
                if (totalItemsEnumerated >= compoundArrayView.totalCount ||
                    compoundArrayView.chunks == null)
                    return false;

                currentPositionInChunk++;

                // Find the next valid chunk and position
                while (currentChunkIndex < chunkCount)
                {
                    var currentChunk = compoundArrayView.chunks[currentChunkIndex];

                    // If current position is within this chunk, we found our element
                    if (currentPositionInChunk < currentChunk.Length)
                    {
                        Current = currentChunk[currentPositionInChunk];
                        totalItemsEnumerated++;
                        return true;
                    }

                    // Move to next chunk
                    currentChunkIndex++;
                    currentPositionInChunk = 0;
                }
                
                // Shouldn't reach this point due to totalCount check at top
                
                /*// Handle last chunk separately due to endOffset
                Assert.IsTrue(currentChunkIndex == chunkCountMinusOne);
                if (currentPositionInChunk < compoundArrayView.lastChunkLength)
                {
                    Current = compoundArrayView.chunks[currentChunkIndex][currentPositionInChunk];
                    ++totalItemsEnumerated;
                    return true;
                }*/

                return false;
            }

            public void Reset()
            {
                currentChunkIndex = 0;
                currentPositionInChunk = compoundArrayView.startIndex - 1;
                totalItemsEnumerated = 0;
                Current = default;
            }

            public void Dispose() { }
        }

        // Return struct enumerator directly for allocation-free enumeration
        public Enumerator GetEnumerator() => new(this);

        // Explicit interface implementation for IEnumerable<T>
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator(); // This will box the struct when used through interface

        // Explicit interface implementation for IEnumerable  
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator(); // This will box the struct when used through interface
    }
}