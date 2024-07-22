#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define SAFETY
#endif

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using VLib;
using VLib.Utility;

namespace PrehistoricKingdom.Modular
{
    public unsafe struct ModularSpatialStack : INativeDisposable
    {
        public long dataID;

        public UnsafeList<uint>* pieceIDs;
        public UnsafeList<float3>* positions;

        public UnsafeParallelHashMap<uint, int> pieceIDsToIndexMap;
        public UnsafeParallelMultiHashMap<float3, int> positionsToIndexMap;

        public int ElementCount => positions != null ? positions->Length : 0;

    #region Init

        public ModularSpatialStack(long dataID) : this()
        {
            this.dataID = dataID;

            pieceIDs = UnsafeList<uint>.Create(8, Allocator.Persistent);
            positions = UnsafeList<float3>.Create(8, Allocator.Persistent);

            pieceIDsToIndexMap = new UnsafeParallelHashMap<uint, int>(8, Allocator.Persistent);
            positionsToIndexMap = new UnsafeParallelMultiHashMap<float3, int>(8, Allocator.Persistent);
        }

        public void Dispose()
        {
            UnsafeList<uint>.Destroy(pieceIDs);
            pieceIDs = null;
            UnsafeList<float3>.Destroy(positions);
            positions = null;
            
            pieceIDsToIndexMap.DisposeSafe();
            if (positionsToIndexMap.IsCreated)
                positionsToIndexMap.Dispose();
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            var handleA = new UnsafeListUtil.UnsafeListPtrDisposalJob<uint>(pieceIDs).Schedule(inputDeps);
            var handleB = new UnsafeListUtil.UnsafeListPtrDisposalJob<float3>(positions).Schedule(inputDeps);
            var handleC = pieceIDsToIndexMap.Dispose(inputDeps);
            var handleD = positionsToIndexMap.Dispose(inputDeps);

            return JobHandle.CombineDependencies(JobHandle.CombineDependencies(handleA, handleB, handleC), handleD);
        }

    #endregion

        public void AddSet(uint pieceID, float3 position)
        {
            if (pieceIDsToIndexMap.TryGetValue(pieceID, out int pieceIndex))
                Set(pieceIndex, pieceID, position);
            else
                Add(pieceID, position);
        }

        public void Remove(uint pieceID)
        {
            //If piece not registered
            if (!pieceIDsToIndexMap.TryGetValue(pieceID, out int pieceIndex))
                return;

            //Remove from hashmaps
            pieceIDsToIndexMap.Remove(pieceID);
            float3 pos = UnsafeUtility.ReadArrayElement<float3>(positions->Ptr, pieceIndex);
            positionsToIndexMap.Remove(pos, pieceIndex);

            int lastPieceIndex = pieceIDs->Length - 1;

            if (pieceIndex == lastPieceIndex)
            {
                pieceIDs->RemoveAt(lastPieceIndex);
                positions->RemoveAt(lastPieceIndex);
            }
            //Remove Swapback
            else if (pieceIDs->Length > 1)
            {
                #if SAFETY
                if (!pieceIDs->IsIndexValid(lastPieceIndex))
                    Debug.LogError("Piece index above pieceIDs length!");
                if (!positions->IsIndexValid(lastPieceIndex))
                    Debug.LogError("Piece index above positions length!");
                #endif
                // Grab last index data
                uint lastPieceID = UnsafeUtility.ReadArrayElement<uint>(pieceIDs->Ptr, lastPieceIndex);
                float3 lastPiecePos = UnsafeUtility.ReadArrayElement<float3>(positions->Ptr, lastPieceIndex);

                //Replace indices for last entry in hashmaps, last index will move to fill this index
                if (!pieceIDsToIndexMap.ContainsKey(lastPieceID))
                    Debug.LogError("Piece ID Not Present.");
                pieceIDsToIndexMap[lastPieceID] = pieceIndex;
                positionsToIndexMap.Remove(lastPiecePos, lastPieceIndex);
                positionsToIndexMap.Add(lastPiecePos, pieceIndex);

                //Remove, and displace last element to fill the gap.
                pieceIDs->RemoveAtSwapBack(pieceIndex);
                positions->RemoveAtSwapBack(pieceIndex);
            }
            //EZPZ Remove
            else if (pieceIDs->Length == 1)
            {
                pieceIDs->Clear();
                positions->Clear();
                pieceIDsToIndexMap.Clear();
                positionsToIndexMap.Clear();
            }
        }

        public void Clear()
        {
            pieceIDs->Clear();
            positions->Clear();
            pieceIDsToIndexMap.Clear();
            positionsToIndexMap.Clear();
        }

        void Add(uint pieceID, float3 position)
        {
#if SAFETY
            if (pieceIDs == null || positions == null)
            {
                Debug.LogError("MSS.ADD null error!");
                return;
            }
#endif
            
            pieceIDsToIndexMap.Add(pieceID, pieceIDs->Length);
            positionsToIndexMap.Add(position, pieceIDs->Length);

            pieceIDs->Add(pieceID);
            positions->Add(position);
        }

        void Set(int pieceIndex, uint pieceID, float3 newPos)
        {
            //Update Pos -> Index Mapping
#if SAFETY
            if (pieceIndex >= positions->Length)
            {
                
                Debug.LogError($"Piece index above positions length of '{positions->Length}'!");
                return;
            }
#endif
            float3 oldPos = (*positions)[pieceIndex];
            positionsToIndexMap.Remove(oldPos, pieceIndex);
            positionsToIndexMap.Add(newPos, pieceIndex);
            
            //Update Position
            (*positions)[pieceIndex] = newPos;
        }

        public uint ReadPieceID(int index) => (*pieceIDs)[index];
        public float3 ReadPosition(int index) => (*positions)[index];
    }
}