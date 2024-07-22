using System;
using Unity.Collections;
using Unity.Mathematics;

namespace VLib
{
    public struct NativeRenderData : IDisposable
    {
        public NativeList<float4x4> matrices;
        public NativeList<int> freeIndices;

        public readonly int InstanceCount => matrices.Length;

        public NativeRenderData(int initCapacity = 16)
        {
            matrices = new NativeList<float4x4>(initCapacity, Allocator.Persistent);
            freeIndices = new NativeList<int>(initCapacity, Allocator.Persistent);
        }

        public int Add(float4x4 matrix)
        {
            int idx = -1;
            if (freeIndices.Length > 0)
            {
                idx = freeIndices.PopUnsafe();
                matrices[idx] = matrix;
            }
            else
            {
                idx = matrices.Length;
                matrices.Add(matrix);
            }

            return idx;
        }

        public void RemoveAt(int index)
        {
            matrices[index] = float4x4.zero;
            freeIndices.Add(index);
        }

        public void Clear()
        {
            matrices.Clear();
            freeIndices.Clear();
        }

        public void Dispose()
        {
            matrices.Dispose();
            freeIndices.Dispose();
        }
    }
}