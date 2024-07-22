#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define SAFETY
//#define FULLBUFFERCOPYVERIFICATION
#endif

using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace VLib
{
    /// <summary> A CPU buffer and a GPU buffer bound together to pipeline data to the GPU.
    /// Warning: Must be disposed when done! </summary>
    public unsafe class VectorBuffer<T> : IVectorBuffer
        where T : unmanaged, IEquatable<T>
    {
        protected VUnsafeRef<InternalNativeUnsafe> nativeRef;
        protected InternalNativeUnsafe* native;
        protected ComputeBuffer gpuBuffer;
        protected string shaderPropertyName = "SET THIS!";
        
        public virtual string ShaderPropertyName => shaderPropertyName;
        public int Capacity => native->cpuBufferUnsafe->m_capacity;
        public int CapacityGPU => gpuBuffer == null ? 0 : gpuBuffer.count;
        public bool IsDirty => any(native->dirtyRange.Value > -1);

        public InternalNativeUnsafe* Native => native;
        public UnsafeList<T>* CPUBuffer => native->cpuBufferUnsafe;
        public ComputeBuffer GPUBuffer => gpuBuffer;
        public T DefaultValue
        {
            get => native->defaultValue;
            set => native->defaultValue = value;
        }
        
        #region Structures

        /// <summary> Must be accessed using a reference structure like NativeReference or VUnsafeRef </summary>
        [GenerateTestsForBurstCompatibility]
        public unsafe struct InternalNativeUnsafe : IDisposable
        {
            public UnsafeList<T>* cpuBufferUnsafe;
            /// <summary>X: Inclusive, Y:Exclusive</summary>
            public VUnsafeRef<int2> dirtyRange;
            public T defaultValue;
            public bool bufferSizeMismatch;

            public InternalNativeUnsafe(T defaultValue, int initialCapacity = 8) : this()
            {
                cpuBufferUnsafe = UnsafeList<T>.Create(initialCapacity, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                dirtyRange = new VUnsafeRef<int2>(-1, Allocator.Persistent);
                this.defaultValue = defaultValue;
            }

            public void Dispose()
            {
                UnsafeList<T>.Destroy(cpuBufferUnsafe);
                cpuBufferUnsafe = null;
                dirtyRange.DisposeRefToDefault();
            }

            public T Read(int index) => (*cpuBufferUnsafe)[index];

            public void Write(int index, T value)
            {
                // EnsureCap checks for null
                if (!EnsureCapacity(index + 1))
                    return;
                WriteNoResize(index, value);
                Dirty(index);
            }

            public void WriteNoResize(int index, T value)
            {
                // Don't know why I AM resizing here, but it's probably for a good reason
                cpuBufferUnsafe->m_length = max(cpuBufferUnsafe->m_length, index + 1);
                (*cpuBufferUnsafe)[index] = value;
            }

            public bool EnsureCapacity(int requiredCapacity)
            {
                if (cpuBufferUnsafe == null)
                {
                    Debug.LogError("EnsureCapacity called on null VectorBuffer!");
                    return false;
                }
                if (cpuBufferUnsafe->m_capacity < requiredCapacity)
                    ResizeCPU(requiredCapacity);
                return true;
            }
            
            /// <summary> Does resizing logic, but cannot resize the GPU buffer, trips a flag for that. </summary>
            public void ResizeCPU(int newCapacity)
            {
                int oldCapacity = cpuBufferUnsafe->m_capacity;
            
                //CPU
                cpuBufferUnsafe->Resize(newCapacity, NativeArrayOptions.UninitializedMemory);

                //GPU buffer needs to be resized later at the managed level
                bufferSizeMismatch = true;
            
                //New Default Values
                ResetRangeToDefaultNoDirty(oldCapacity, -1);

                DirtyAll();
            }

            /// <summary> Sets all values within the specified range to the default value. </summary>
            /// <param name="start">First index to be reset.</param>
            /// <param name="count">How many indices to reset. Less than one will reset all indices from start to the end of the buffer.</param>
            [GenerateTestsForBurstCompatibility]
            public void ResetRangeToDefaultNoDirty(int start, int count)
            {
                if (count < 1)
                    count = int.MaxValue;
                int endBefore = min(start + count, cpuBufferUnsafe->m_capacity);

                // Modify length temporarily to allow for default value writes
                var prevLength = cpuBufferUnsafe->m_length;
                cpuBufferUnsafe->Length = endBefore;
                // Write default values
                for (int i = start; i < endBefore; i++) 
                    (*cpuBufferUnsafe)[i] = defaultValue;
                cpuBufferUnsafe->Length = prevLength;
            }

            /*[GenerateTestsForBurstCompatibility]
            public U ReadAs<U>(int index)
                where U : struct
            {
                return cpuBuffer.ReinterpretLoad<U>(index * UnsafeUtility.SizeOf<T>());
            }

            [GenerateTestsForBurstCompatibility]
            public void WriteAsNoResize<U>(int index, U value)
                where U : struct
            {
                int byteIndex = index * UnsafeUtility.SizeOf<U>();
                cpuBuffer.ReinterpretStore(byteIndex, value);
                Dirty(byteIndex);
            }*/

            public unsafe void Dirty(int index)
            {
                if (index < 0)
                    return;
                
                int2* dirtyRangeRef = dirtyRange.TPtr;
                if (dirtyRangeRef->x < 0)
                    dirtyRangeRef->x = index;
                dirtyRangeRef->x = min(dirtyRangeRef->x, index);
                dirtyRangeRef->y = max(dirtyRangeRef->y, index + 1); //Exclusive
            }

            public void DirtyAll()
            {
                Dirty(0);
                Dirty(cpuBufferUnsafe->m_length - 1); //Exclusive
            }
        }
            
        #endregion

        public VectorBuffer(T defaultValue, int initialCapacity = 8)
        {
            nativeRef = new VUnsafeRef<InternalNativeUnsafe>(new InternalNativeUnsafe(defaultValue, initialCapacity), Allocator.Persistent);
            native = nativeRef.TPtr;
            gpuBuffer = new ComputeBuffer(initialCapacity,  UnsafeUtility.SizeOf<T>(), ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);
            
            //Reset all values
            ResetRangeToDefaultNoDirty(0, -1);
        }

        public void Dispose()
        {
            gpuBuffer?.Release();
            gpuBuffer = null;

            if (nativeRef.IsCreated)
                nativeRef.TPtr->Dispose();
            nativeRef.DisposeRefToDefault();
            native = null;
        }

        public T this[int index]
        {
            get
            {
#if SAFETY
                if (native == null)
                    throw new NullReferenceException("VectorBuffer native ptr is null!");
#endif
                return native->Read(index);
            }
            set
            {
#if SAFETY
                if (native == null)
                    throw new NullReferenceException("VectorBuffer native ptr is null!");
#endif
                native->Write(index, value);
            }
        }

        public void WriteNoResize(int index, T value) => native->WriteNoResize(index, value);

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
            native->EnsureCapacity(requiredCapacity);
            EnsureGPUBufferFitsCPUBuffer(true);
        }

        /// <summary>Any .SetBuffer calls will need to be reexecuted!</summary>
        public void Resize(int newCapacity)
        {
            native->ResizeCPU(newCapacity);
            EnsureGPUBufferFitsCPUBuffer(false);
        }

        public void EnsureGPUBufferFitsCPUBuffer(bool dirty)
        {
            if (Capacity != CapacityGPU)
            {
                if (gpuBuffer != null) 
                    gpuBuffer.Dispose();
                gpuBuffer = new ComputeBuffer(Capacity, UnsafeUtility.SizeOf<T>(), ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);

                if (dirty)
                    DirtyAll();
            }

            native->bufferSizeMismatch = false;
        }

        /// <summary> Sets all values within the specified range to the default value. </summary>
        /// <param name="start">First index to be reset.</param>
        /// <param name="count">How many indices to reset. Less than one will reset all indices from start to the end of the buffer.</param>
        public void ResetRangeToDefaultNoDirty(int start, int count) => native->ResetRangeToDefaultNoDirty(start, count);

        /*[GenerateTestsForBurstCompatibility]
        public U ReadAs<U>(int index)
            where U : struct
        {
            return cpuBuffer.ReinterpretLoad<U>(index * UnsafeUtility.SizeOf<T>());
        }

        [GenerateTestsForBurstCompatibility]
        public void WriteAsNoResize<U>(int index, U value)
            where U : struct
        {
            int byteIndex = index * UnsafeUtility.SizeOf<U>();
            cpuBuffer.ReinterpretStore(byteIndex, value);
            Dirty(byteIndex);
        }*/

        public unsafe void Dirty(int index) => native->Dirty(index);

        public void DirtyAll() => native->DirtyAll();

        /// <summary> Call this to update the internal computebuffer, otherwise the GPU will not get the updated data!</summary>
        public void UpdateGPUBuffer()
        {
            //If buffer is modified on the native-end, we need to ensure the GPUBuffer still fits.
            //if (native->bufferSizeMismatch)  //Check causes major issues if it's ever wrong and it's wrong rarely, just skip it since it's cheap
            EnsureGPUBufferFitsCPUBuffer(true);
            
            var dirtyRangeRef = native->dirtyRange.TPtr;
            int count = dirtyRangeRef->y - dirtyRangeRef->x;
            var writeBuffer = gpuBuffer.BeginWrite<T>(dirtyRangeRef->x, count);
            
            //Copy between buffers as fast as poosible
            UnsafeUtility.MemCpy(writeBuffer.GetUnsafePtr(),
                native->cpuBufferUnsafe->Ptr + dirtyRangeRef->x,
                sizeof(T) * count);
            //NativeArray<T>.Copy(native->cpuBufferUnsafe->Ptr, dirtyRangeRef.x, writeBuffer, 0, count);
            /*for (int i = 0; i < count; i++) 
                writeBuffer[i] = cpuBuffer[i + dirtyRange.x];*/
            
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
            *dirtyRangeRef = new int2(-1);
            
#if UNITY_EDITOR
            if (!native->dirtyRange.Value.Equals(new int2(-1)))
                Debug.LogError("Dirty range ref broke!");
#endif
        }

        public void UpdateGPUBufferIfDirty()
        {
            if (IsDirty)
                UpdateGPUBuffer();
        }
    }
}