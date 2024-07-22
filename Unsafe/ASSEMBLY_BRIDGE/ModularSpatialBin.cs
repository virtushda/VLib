using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using VLib;

namespace PrehistoricKingdom.Modular
{
    /// <summary>
    /// This struct is meant to store IDs and Positions for each 'grid bin', and has a buffer per-modular-dataID.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public unsafe struct ModularSpatialBin : INativeDisposable
    {
        int initSize;
        RectNative worldRectXZ;

        public UnsafeList<ModularSpatialStack>* spatialStacks;
        public UnsafeParallelHashMap<long, int> dataIDToBufferIndexMap;

        public int StackCount => spatialStacks->Length;
        /// <summary> Cumulative count of all internal spatial 'stack's. </summary>
        public int ElementCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < spatialStacks->Length; i++)
                    count += (*spatialStacks)[i].ElementCount;
                
                return count;
            }
        }

        public RectNative WorldRectXZ => worldRectXZ;

    #region Init

        public ModularSpatialBin(RectNative worldRectXZ, int initSize = 8)
        {
            this.initSize = initSize;
            this.worldRectXZ = worldRectXZ;

            spatialStacks = UnsafeList<ModularSpatialStack>.Create(initSize, Allocator.Persistent);
            dataIDToBufferIndexMap = new UnsafeParallelHashMap<long, int>(initSize, Allocator.Persistent);
        }

        public void Dispose()
        {
            if (spatialStacks->IsCreated && spatialStacks->Length > 0)
                for (int i = 0; i < spatialStacks->Length; i++)
                    this[i].DisposeRefToDefault();

            spatialStacks->DisposeRefToDefault();
            dataIDToBufferIndexMap.DisposeRefToDefault();
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            JobHandle jobHandle = default;
            if (spatialStacks->IsCreated && spatialStacks->Length > 0)
                for (int i = 0; i < spatialStacks->Length; i++)
                    jobHandle = JobHandle.CombineDependencies(jobHandle, this[i].Dispose(inputDeps));

            jobHandle = JobHandle.CombineDependencies(jobHandle, spatialStacks->Dispose(inputDeps));
            return JobHandle.CombineDependencies(jobHandle, dataIDToBufferIndexMap.Dispose(inputDeps));
        }

    #endregion

        bool IndexValid(int i) => i > -1 && i < spatialStacks->Length;

        public ref ModularSpatialStack this[int index]
        {
            get
            {
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
                if (IndexValid(index))
#endif
                    return ref UnsafeUtility.ArrayElementAsRef<ModularSpatialStack>(spatialStacks->Ptr, index);

#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
                throw new IndexOutOfRangeException($"Buffer index '{index}' is not valid in range [0 - {spatialStacks->Length - 1}]!");
#endif
            }
        }

        public bool AddSet(long dataID, uint pieceID, float3 position)
        {
            if (!worldRectXZ.Contains(position.xz))
                return false;

            ref var stack = ref GetStack(dataID);
            stack.AddSet(pieceID, position);

            return true;
        }

        public bool AddSetUnsafe(long dataID, uint pieceID, float3 position)
        {
            var stack = GetStack(dataID);

            stack.AddSet(pieceID, position);

            return true;
        }

        public void Remove(long dataID, uint pieceID)
        {
            //if no matching stack
            if (!dataIDToBufferIndexMap.TryGetValue(dataID, out int bufferIndex))
                return;

            //Remove Element from it's stack
            if (spatialStacks == null || bufferIndex < 0 || bufferIndex >= spatialStacks->Length)
            {
                Debug.LogError("spatialStacks error!");
                return;
            }
            
            ref var spatialStack = ref UnsafeUtility.ArrayElementAsRef<ModularSpatialStack>(spatialStacks->Ptr, bufferIndex);
            spatialStack.Remove(pieceID);
                
            //Remove stack itself if empty
            if (spatialStack.ElementCount < 1)
                RemoveStack(dataID, ref spatialStack, bufferIndex);
        }

        public void Clear()
        {
            for (int i = 0; i < spatialStacks->Length; i++)
            {
                var spatialStack = (*spatialStacks)[i];
                spatialStack.Dispose();
            }
            spatialStacks->Clear();
            dataIDToBufferIndexMap.Clear();
        }

        ref ModularSpatialStack GetStack(long dataID)
        {
            if (!dataIDToBufferIndexMap.IsCreated)
                Debug.LogError("dataIDToBufferIndexMap doesn't exist!");
            if (!dataIDToBufferIndexMap.TryGetValue(dataID, out var bufferIndex))
                bufferIndex = AddNewStack(dataID);

            return ref this[bufferIndex];
        }

        int AddNewStack(long dataID)
        {
            int index = spatialStacks->Length;
            dataIDToBufferIndexMap.Add(dataID, index);
            spatialStacks->Add(new ModularSpatialStack(dataID));
            return index;
        }

        void RemoveStack(long dataID, ref ModularSpatialStack spatialStack, int stackIndex)
        {
            spatialStack.Dispose();

            int lastStackIndex = spatialStacks->Length - 1;
            
            //RemoveAt with Swapback, then change mapping for that last index, so it's correct
            if (lastStackIndex >= 1)
            {
                //Get last stack and UpdateMapping
                var lastStack = (*spatialStacks)[lastStackIndex]; //UnsafeUtility.ArrayElementAsRef<ModularSpatialStack>(spatialStacks->Ptr, lastStackIndex);
                dataIDToBufferIndexMap[lastStack.dataID] = stackIndex;
                
                //Replace target stack with last stack
#if UNITY_EDITOR
                if (!spatialStacks->IsIndexValid(stackIndex))
                    Debug.LogError($"stackIndex '{stackIndex}' is not valid in range [0 - {spatialStacks->Length - 1}]!");
#endif
                (*spatialStacks)[stackIndex] = lastStack;
                //UnsafeUtility.WriteArrayElement(spatialStacks->Ptr, stackIndex, lastStack);
                spatialStacks->RemoveAt(lastStackIndex);
                dataIDToBufferIndexMap.Remove(dataID);
            }
            else
            {
                spatialStacks->Clear();
                dataIDToBufferIndexMap.Clear();
            }
        }
    }
}