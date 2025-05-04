#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define SAFETY
//#define FULLBUFFERCOPYVERIFICATION
#endif

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using static Unity.Mathematics.math;

namespace VLib
{
    /// <summary> A CPU buffer and a GPU buffer bound together to pipeline data to the GPU.
    /// Warning: Must be disposed when done! </summary>
    public unsafe class VectorBuffer<T> : IVectorBuffer
        where T : unmanaged, IEquatable<T>
    {
        protected RefStruct<InternalNativeUnsafe> native;
        protected ComputeBuffer gpuBuffer;
        protected string shaderPropertyName = "SET THIS!";
        
        public virtual string ShaderPropertyName => shaderPropertyName;
        public int CapacityCPU => native.ValueRef.cpuBufferUnsafe.Capacity;
        public int CapacityGPU => gpuBuffer?.count ?? 0;
        public bool IsDirty => any(native.ValueRef.dirtyRange > -1);

        public RefStruct<InternalNativeUnsafe> Native => native;
        public ComputeBuffer GPUBuffer => gpuBuffer;
        /*public T DefaultValue
        {
            get => native->defaultValue;
            set => native->defaultValue = value;
        }*/
        
        #region Structures

        /// <summary> Must be accessed using a reference structure like NativeReference or VUnsafeRef </summary>
        [GenerateTestsForBurstCompatibility]
        public struct InternalNativeUnsafe : IDisposable
        {
            public UnsafeList<T> cpuBufferUnsafe;
            /// <summary>X: Inclusive, Y:Exclusive</summary>
            public int2 dirtyRange;
            public T defaultValue;

            public InternalNativeUnsafe(T defaultValue, int initialCapacity = 8) : this()
            {
                cpuBufferUnsafe = new UnsafeList<T>(initialCapacity, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                dirtyRange = -1;
                this.defaultValue = defaultValue;
            }

            public void Dispose()
            {
                cpuBufferUnsafe.DisposeRefToDefault();
            }

            public T Read(int index) => cpuBufferUnsafe[index];

            public void Write(int index, T value)
            {
                BurstAssert.True(index >= 0);
                // EnsureCap checks for null
                if (!EnsureCapacity(index + 1))
                    return;
                WriteNoResize(index, value);
                Dirty(index);
            }

            public void WriteNoResize(int index, T value)
            {
                BurstAssert.True(index >= 0);
                var requiredLength = index + 1;
                if (cpuBufferUnsafe.Capacity < requiredLength)
                    throw new ArgumentOutOfRangeException($"VectorBuffer CPU buffer capacity is too small! Required: {requiredLength}, Current: {cpuBufferUnsafe.Capacity}");
                cpuBufferUnsafe.Length = max(cpuBufferUnsafe.Length, requiredLength);
                cpuBufferUnsafe[index] = value;
            }

            public bool EnsureCapacity(int requiredCapacity)
            {
                if (cpuBufferUnsafe.Capacity < requiredCapacity)
                    ResizeCPU(requiredCapacity);
                return true;
            }
            
            /// <summary> Does resizing logic, but cannot resize the GPU buffer, trips a flag for that. </summary>
            public void ResizeCPU(int newCapacity)
            {
                int oldCapacity = cpuBufferUnsafe.Capacity;
            
                //CPU
                cpuBufferUnsafe.Capacity = newCapacity;
                //GPU buffer needs to be resized later at the managed level

                //New Default Values
                ResetRangeToDefaultNoDirty(oldCapacity, -1);

                DirtyAll();
            }

            /// <summary> Sets all values within the specified range to the default value. </summary>
            /// <param name="start">First index to be reset.</param>
            /// <param name="count">How many indices to reset. Less than one will reset all indices from start to the end of the buffer.</param>
            public void ResetRangeToDefaultNoDirty(int start, int count)
            {
                BurstAssert.True((uint)start < cpuBufferUnsafe.Capacity); // Handle negatives and check under capacity
                
                // If unspecified count, reset all values from start to the end of the buffer
                if (count < 1)
                    count = cpuBufferUnsafe.Capacity - start;
                int endBefore = start + count;
                endBefore = min(endBefore, cpuBufferUnsafe.Capacity);

                // Modify length temporarily to allow for default value writes
                var prevLength = cpuBufferUnsafe.Length;
                cpuBufferUnsafe.Length = endBefore;
                
                // Write default values
                for (int i = start; i < endBefore; ++i) 
                    cpuBufferUnsafe[i] = defaultValue;
                
                // Restore length
                cpuBufferUnsafe.Length = prevLength;
            }

            public void Dirty(int index)
            {
                if (index < 0)
                    return;
                if (dirtyRange.x < 0)
                    dirtyRange.x = index;
                dirtyRange.x = min(dirtyRange.x, index);
                dirtyRange.y = max(dirtyRange.y, index + 1); //Exclusive
            }

            public void DirtyAll()
            {
                Dirty(0);
                Dirty(cpuBufferUnsafe.Length - 1); //Exclusive
            }
        }
            
        #endregion

        public VectorBuffer(T defaultValue, int initialCapacity = 8)
        {
            native = RefStruct<InternalNativeUnsafe>.Create(new InternalNativeUnsafe(defaultValue, initialCapacity));
            gpuBuffer = new ComputeBuffer(initialCapacity,  UnsafeUtility.SizeOf<T>(), ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);
            
            //Reset all values
            ResetRangeToDefaultNoDirty(0, -1);
        }

        #if UNITY_EDITOR
        private bool isDisposed;
        #endif
        
        public void Dispose()
        {
            // CPU
            if (native.IsCreated)
            {
                native.ValueRef.DisposeRefToDefault();
                native.DisposeRefToDefault();
            }
            
            // GPU
            gpuBuffer?.Release();
            gpuBuffer = null;
        
        #if UNITY_EDITOR
            isDisposed = true;
        #endif
        }
        
        #if UNITY_EDITOR
        ~VectorBuffer()
        {
            if (!isDisposed)
                Debug.LogError($"VectorBuffer<{typeof(T)}> was not disposed! You must call Dispose() when done with the buffer.");
        }
        #endif
        
        public T this[int index]
        {
            get
            {
#if SAFETY
                if (!native.IsCreated)
                    throw new NullReferenceException("VectorBuffer native ptr is null!");
#endif
                return native.ValueRef.Read(index);
            }
            set
            {
#if SAFETY
                if (!native.IsCreated)
                    throw new NullReferenceException("VectorBuffer native ptr is null!");
#endif
                native.ValueRef.Write(index, value);
            }
        }
        
        public bool TryGetValue(int index, out T value)
        {
            native.ConditionalCheckValid();
            ref var nativeRef = ref native.ValueRef;
            nativeRef.cpuBufferUnsafe.ConditionalCheckIsCreated();
            VCollectionUtils.ConditionalCheckIndexValid(index, nativeRef.cpuBufferUnsafe.Length);
            
            return nativeRef.cpuBufferUnsafe.TryGet(index, out value);
        }

        //public void WriteNoResize(int index, T value) => native->WriteNoResize(index, value);

        public bool TrySetValueFromDeclaration<TDeclaration>(int index, TDeclaration declaration) 
            where TDeclaration : IVectorBufferDeclaration
        {
            if (declaration is IVectorBufferDeclaration<T> declarationTyped)
            {
                this[index] = declarationTyped.DefaultValue;
                return true;
            }

            return false;
        }

        public void EnsureCapacity(int requiredCapacity)
        {
            native.ValueRef.EnsureCapacity(requiredCapacity);
            EnsureGPUBufferFitsCPUBuffer(true);
        }

        /// <summary>Any .SetBuffer calls will need to be reexecuted!</summary>
        public void Resize(int newCapacity)
        {
            native.ValueRef.ResizeCPU(newCapacity);
            EnsureGPUBufferFitsCPUBuffer(false);
        }

        public void EnsureGPUBufferFitsCPUBuffer(bool dirty)
        {
            if (CapacityCPU == CapacityGPU)
                return;
            
            if (gpuBuffer != null) 
                gpuBuffer.Dispose();
            gpuBuffer = new ComputeBuffer(CapacityCPU, UnsafeUtility.SizeOf<T>(), ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);

            if (dirty)
                DirtyAll();
        }

        /// <summary> Sets all values within the specified range to the default value. </summary>
        /// <param name="start">First index to be reset.</param>
        /// <param name="count">How many indices to reset. Less than one will reset all indices from start to the end of the buffer.</param>
        public void ResetRangeToDefaultNoDirty(int start, int count) => native.ValueRef.ResetRangeToDefaultNoDirty(start, count);

        public void Dirty(int index) => native.ValueRef.Dirty(index);

        public void DirtyAll() => native.ValueRef.DirtyAll();

        /// <summary> Call this to update the internal computebuffer, otherwise the GPU will not get the updated data!</summary>
        public void UpdateGPUBuffer()
        {
            Profiler.BeginSample("VectorBuffer-UpdateGPUBuffer");
            
            //If buffer is modified on the native-end, we need to ensure the GPUBuffer still fits.
            EnsureGPUBufferFitsCPUBuffer(true);

            ref var nativeRef = ref native.ValueRef;
            
            // Absolutely ensure that the dirty range is within the bounds of the CPU buffer
            nativeRef.dirtyRange.x = max(nativeRef.dirtyRange.x, 0);
            nativeRef.dirtyRange.y = min(nativeRef.dirtyRange.y, CapacityCPU);
            if (nativeRef.dirtyRange.x >= nativeRef.dirtyRange.y || nativeRef.cpuBufferUnsafe.Length < 1)
            {
                // No need to update the GPU buffer if the dirty range is empty
                if (nativeRef.dirtyRange.x > nativeRef.dirtyRange.y)
                    Debug.LogError($"Dirty range is invalid! X:{nativeRef.dirtyRange.x} Y:{nativeRef.dirtyRange.y}");
                Profiler.EndSample();
                return;
            }
            int count = nativeRef.dirtyRange.y - nativeRef.dirtyRange.x;
            
            var writeBuffer = gpuBuffer.BeginWrite<T>(nativeRef.dirtyRange.x, count);
            
            // Get write view
            var writeBufferAsUnsafeList = writeBuffer.AsUnsafeList_UNSAFE(NativeSafety.ReadWrite);
            // Get read view
            VCollectionUtils.ConditionalCheckRangeValid(nativeRef.dirtyRange.x, count, nativeRef.cpuBufferUnsafe.Length);
            var readBuffer = new UnsafeList<T>(nativeRef.cpuBufferUnsafe.Ptr + nativeRef.dirtyRange.x, count);
            //Copy between buffers fast
            writeBufferAsUnsafeList.CopyFrom(readBuffer);
            
            gpuBuffer.EndWrite<T>(count);
            
#if FULLBUFFERCOPYVERIFICATION
            
            // Ensure that the cpu and gpu buffer are FULLY in sync
            var cpuBuffer = CPUBuffer;
            var gpuData = new T[gpuBuffer.count];
            gpuBuffer.GetData(gpuData);

            for (int i = 0; i < cpuBuffer->m_length; i++)
            {
                var cpuValue = (*cpuBuffer)[i];
                var gpuValue = gpuData[i];
                
                if (!cpuValue.Equals(gpuValue))
                    Debug.LogError($"VectorBuffer{typeof(T)} data mismatch! CPU:{cpuValue} -- GPU:{gpuValue}");
            }
            if (cpuBuffer->m_capacity != gpuData.Length)
                Debug.LogError("");
#endif

            //Reset Dirty
            nativeRef.dirtyRange = -1;
            
#if UNITY_EDITOR
            if (!native.ValueRef.dirtyRange.Equals(new int2(-1)))
                Debug.LogError("Dirty range ref broke!");
#endif
            Profiler.EndSample();
        }

        public void UpdateGPUBufferIfDirty()
        {
            if (IsDirty)
                UpdateGPUBuffer();
        }
    }
}