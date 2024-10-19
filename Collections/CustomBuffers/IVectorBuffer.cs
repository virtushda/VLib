using System;
using UnityEngine;

namespace VLib
{
    public interface IVectorBuffer
    {
        public string ShaderPropertyName { get; }
        
        public int CapacityCPU { get; }
        public ComputeBuffer GPUBuffer { get; }
        public bool IsDirty { get; }

        public bool TrySetValueFromDeclaration<T>(int index, T declaration) where T : IVectorBufferDeclaration;
        public void EnsureCapacity(int requiredCapacity);
        public void Resize(int newCapacity);
        public void UpdateGPUBuffer();
        public void Dispose();

        public bool TryWriteAs<T>(int index, T value)
            where T : unmanaged, IEquatable<T>
        {
            if (this is VectorBuffer<T> castBuffer)
            {
                castBuffer[index] = value;
                return true;
            }
            return false;
        }
    }
}