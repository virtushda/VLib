#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define SAFETY
#define LOCK_CHECKS
//#define LOG_BUFFER_SHRINKS
//#define CLAIM_TRACKING // Reports the method that calls ClaimBuffer
//#define SUPER_CLAIM_TRACKING // Reports the stack trace of the method that calls ClaimBuffer
//#define SUPER_DUPER_CLAIM_TRACKING // Not a thing, but if I need this it should track the stack trace of whoever calls ClaimBuffer and keep track of which buffers are released and report ones which aren't...
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

namespace VLib
{
    /// <summary> A large pool of custom memory buffers, on retrieval these buffers can be 'typed' to any unmanaged type! </summary>
    public unsafe struct UnsafeBufferPool : IDisposable, IMemoryReporter
    {
        // Per processing pass, how many buffers to process
        public const int BufferCheckCount = 2048;
        public const int BufferShrinkCount = 64;

        // RootBuffer -> UnsafeBuffers
        [NativeDisableUnsafePtrRestriction] VUnsafeBufferedRef<UnsafeBufferPoolData> dataBufferPtr;
        public readonly UnsafeBufferPoolData* Data => dataBufferPtr.TPtr;

        public readonly bool IsCreated => dataBufferPtr.IsValid;

        public UnsafeBufferPool(int initialBufferCount, GlobalBurstTimer burstTimer, ushort rootBufferSize = 8192, ushort unsafeBufferSize = 64, bool allowLogging = true) : this()
        {
            // Use new safe stuff instead of raw memory calls
            dataBufferPtr = new VUnsafeBufferedRef<UnsafeBufferPoolData>(new UnsafeBufferPoolData(rootBufferSize, unsafeBufferSize, burstTimer), Allocator.Persistent);
            
            //data = (UnsafeBufferPoolData*) UnsafeUtility.Malloc(UnsafeUtility.SizeOf<UnsafeBufferPoolData>(), UnsafeUtility.AlignOf<UnsafeBufferPoolData>(), Allocator.Persistent);
            //var dataStruct = new UnsafeBufferPoolData(rootBufferSize, unsafeBufferSize);
            //UnsafeUtility.CopyStructureToPtr(ref dataStruct, data);

            // Create initial buffers
            using (Data->spinLock.Scoped())
                EnsureCapacity(initialBufferCount);

            /*Debug.Log($"UnsafeBufferPool Maximum Memory Footprint: " +
                      $"{(rootBufferSize * subBufferSize * (unsafeBufferSize + UnsafeUtility.SizeOf<UnsafeBuffer>())).AsDataSizeInBytesToReadableString()}");
            Debug.Log($"UnsafeBufferPool Init Footprint: {ReportBytes().AsDataSizeInBytesToReadableString()}");*/
        }

        /// <summary> Disposes ALL buffers, claimed buffers merely have exclusive access, they don't "remove" the buffers themselves from the pool. </summary>
        public void Dispose()
        {
            Profiler.BeginSample("UnsafeBufferPool.Dispose");
            if (Data->processJobLaunched)
                Data->processHandle.Complete();

            Data->Dispose();
            dataBufferPtr.DisposeRefToDefault(); // Should be ok...
            //UnsafeUtility.Free(Data, Allocator.Persistent);
            Profiler.EndSample();
        }

        public void OnUpdate()
        {
            CompleteManagementJob();
            
            // Launch new
            if (Data->FreeIndices->Length > 0)
            {
                var newProcessJob = new MemoryManageJob(Data);
                Data->processHandle = newProcessJob.Schedule();
                Data->processJobLaunched = true;
            }
        }

        public void OnLateUpdate()
        {
            CompleteManagementJob();
        }

        public void CompleteManagementJob()
        {
            if (Data->processJobLaunched)
            {
                Data->processJobLaunched = false;
                Data->processHandle.Complete();
            }
        }

        /// <summary> Thread-safe! </summary> 
        public readonly UnsafeBufferClaim<T> ClaimBuffer<T>(
#if CLAIM_TRACKING
            [CallerMemberName] string callerName = ""
#endif
            )
            where T : unmanaged
        {
            int claimIndex = 0;
            UnsafeList<byte>* bufferPtr = (UnsafeList<byte>*) IntPtr.Zero;
            bool error = true;

            using (Data->spinLock.Scoped())
            {
                Data->DemandIsLocked();
                
                try
                {
                    // Try get free index
                    if (!Data->FreeIndices->IsEmpty)
                    {
                        int indexOfIndex = Data->FreeIndices->Length - 1;
                        claimIndex = (*Data->FreeIndices)[indexOfIndex];
                        Data->FreeIndices->RemoveAt(indexOfIndex);
                    }
                    // Else claim the next pool alloc index
                    else
                    {
                        claimIndex = Data->poolAllocIndex;
                        Interlocked.Increment(ref Data->poolAllocIndex);
                    }
                    
                    Data->DemandIsLocked();

                    if (!Data->claimedBuffers.Add(claimIndex))
                        Debug.LogError($"Buffer {claimIndex} already claimed!");
                
                    //TODO: This could be managed by a separate read-write lock for more speed if needed.
                    EnsureCapacity(claimIndex + 1);
                
                    // Retrieve actual buffer (this should be inside lock to stop it from reading when rootbuffer capacity is being increased!)
                    bufferPtr = Data->GetBuffer(claimIndex);

                    error = false;
                }
                finally
                {
                }
            }

            if (error) // Is reachable!
                Debug.LogError("ClaimBuffer EXCEPTION caught in try/catch! Unable to log exception from burst...");

//#if UNITY_EDITOR // Look for issue in builds too!
            if (bufferPtr is null || bufferPtr == (UnsafeList<byte>*) IntPtr.Zero)
                Debug.LogError("Buffer pointer is null or zero!");
            if (bufferPtr->Length > 0)
                Debug.LogError("Buffer length over zero after clear!");
            // Strange issue where sometimes buffers can be retrieved with corrupt lengths...
            bufferPtr->Clear();
//#endif

#if CLAIM_TRACKING
            Debug.Log($"{callerName} claimed buffer");
#endif
#if SUPER_CLAIM_TRACKING
            Debug.Log($"Buffer claim super track: " + StackTraceUtility.ExtractStackTrace());
#endif
            // Return in wrapper
            return new UnsafeBufferClaim<T>(bufferPtr, Data, claimIndex);
        }

        /// <summary> Thread-safe! </summary> 
        public void ReleaseBuffer<T>(UnsafeBufferClaim<T> bufferClaim, bool logIfBufferAlreadyDisposed = true)
            where T : unmanaged
        {
            // If the data is not created
            if (IsCreated)
                ReleaseBuffer(Data, ref bufferClaim, logIfBufferAlreadyDisposed);
            else if (logIfBufferAlreadyDisposed)
                Debug.LogWarning("BufferPool not created, release call is superfluous!");
        }

        public static void ReleaseBuffer<T>(UnsafeBufferPoolData* bufferPoolData, ref UnsafeBufferClaim<T> bufferClaim, bool logIfBufferAlreadyDisposed = true)
            where T : unmanaged
        {
            bool dataExists = bufferPoolData != null && bufferPoolData->IsFullyCreated;
            
            // Ensure is valid buffer
            if (!bufferClaim.IsCreated)
            {
                if (logIfBufferAlreadyDisposed && dataExists)
                    Debug.LogError("Disposed buffer returned to pool!");
                return;
            }

            if (!dataExists)
                return;
                
            // Clear data fast
            bufferClaim.Clear();

            // Return index
            using (bufferPoolData->spinLock.Scoped(5f))
            {
                bufferPoolData->DemandIsLocked();
                
                try
                {
                    DisclaimPoolAddressNoLock(bufferPoolData, bufferClaim.poolAddress);
                   
                    /*if (bufferPoolData->claimedBuffers.Remove(bufferClaim.poolAddress))
                        bufferPoolData->FreeIndices->Add(bufferClaim.poolAddress);
                    else
                        Debug.LogError($"Buffer {bufferClaim.poolAddress} not claimed!");*/
                }
                finally
                {
                }
            }
        }

        /// <summary> Very dangerous method, bypasses internal thread lock! Can be used for fast batch operations with data->Lock and data->Unlock. </summary>
        public void ReleaseBufferNoLock<T>(UnsafeBufferClaim<T> bufferClaim)
            where T : unmanaged
        {
            Data->DemandIsLocked();
            
            // Clear data fast
            if (bufferClaim.guardedBuffer.TryGetTPtr(out var bufferPtr))
                bufferPtr->Clear();
            else
                Debug.LogError("Buffer pointer is null, zero or corrupt!");
            
            DisclaimPoolAddressNoLock(bufferClaim.poolAddress);
            
            Data->DemandIsLocked();
            
            /*// Release claim
            if (!Data->claimedBuffers.Remove(bufferClaim.poolAddress))
                Debug.LogError($"Buffer {bufferClaim.poolAddress} not claimed!");
            // Return index
            Data->FreeIndices->Add(bufferClaim.poolAddress);*/
        }
        
        void DisclaimPoolAddressNoLock(int poolAddress) => DisclaimPoolAddressNoLock(Data, poolAddress);
        
        static void DisclaimPoolAddressNoLock(UnsafeBufferPoolData* data, int poolAddress)
        {
            data->DemandIsLocked();
            
            if (!data->claimedBuffers.Remove(poolAddress))
                Debug.LogError($"Buffer {poolAddress} not claimed!");
            data->FreeIndices->Add(poolAddress);
            
            data->DemandIsLocked();
        }

        /// <summary> Recommend to Lock before calling this!!!!!!!!!!!!!!!!!!!!!!!!!!!! </summary> 
        readonly void EnsureCapacity(int capacity)
        {
            Data->DemandIsLocked();
            if (capacity <= Data->RootBuffer->Length)
                return;
            
            Data->DemandIsLocked();

            if (Data->RootBuffer->Capacity < capacity)
                Data->RootBuffer->Capacity = capacity;
            
            Data->DemandIsLocked();
            
            while (Data->RootBuffer->Length < capacity)
            {
                Data->RootBuffer->AddNoResize((IntPtr)UnsafeList<byte>.Create(Data->unsafeBufferInitialSize, Allocator.Persistent));
            }
            
            Data->DemandIsLocked();
        }

        public long ReportBytes()
        {
            long size = UnsafeUtility.SizeOf<UnsafeBufferPoolData>();
            size += (*Data->RootBuffer).MemoryFootprintBytes();
            size += Data->claimedBuffers.Capacity * sizeof(int);

            for (int i = 0; i < Data->RootBuffer->Length; i++)
            {
                var buffer = Data->RootBuffer->Ptr[i];
                size += (*(UnsafeList<byte>*) buffer.ToPointer()).MemoryFootprintBytes();
            }

            size += (*Data->FreeIndices).MemoryFootprintBytes();
            return size;
        }
    }

    /// <summary> This is intended for internal use, very dangerous stuff in here. </summary>
    public unsafe struct UnsafeBufferPoolData
    {
        /// <summary>The root buffer of subbuffers, used to expand memory use without destroying prior allocations and shared ptrs.</summary>
        public readonly ushort rootBufferSize;
        /// <summary>The actual buffer sizes Pooled buffers must be less-than-or-equals this limit, or they will be reallocated! </summary>
        public readonly ushort unsafeBufferInitialSize;
        
        public BurstSpinLock spinLock;

        /// <summary>Current highest known free index of the pool.</summary>
        public volatile int poolAllocIndex;

        [NativeDisableUnsafePtrRestriction] public VUnsafeBufferedRef<UnsafeList<IntPtr>> rootBuffer;
        [NativeDisableUnsafePtrRestriction] public VUnsafeBufferedRef<UnsafeList<int>> freeIndices;
        public UnsafeParallelHashSet<int> claimedBuffers;

        public int freeIndexProcessMarker;
        public bool processJobLaunched;
        public JobHandle processHandle;

        /// <summary> A buffer of unsafelist-byte ptrs stored as IntPtrs. Each list ptr has its own ptr and thus maintains a separate buffer from the root buffer entirely. </summary>
        public readonly UnsafeList<IntPtr>* RootBuffer => rootBuffer.TPtr;
        public readonly UnsafeList<int>* FreeIndices => freeIndices.TPtr;

        public bool IsFullyCreated => rootBuffer.IsValid && freeIndices.IsValid;/* RootBuffer != null && RootBuffer->IsCreated &&
                                      FreeIndices != null && FreeIndices->IsCreated;*/

        public UnsafeBufferPoolData(ushort rootBufferSize, ushort unsafeBufferInitialSize, GlobalBurstTimer burstTimer) : this()
        {
            this.rootBufferSize = rootBufferSize;
            this.unsafeBufferInitialSize = (ushort) math.max(unsafeBufferInitialSize, 64);

            spinLock = new BurstSpinLock(Allocator.Persistent, burstTimer);
            poolAllocIndex = 0;

            var rootBufferList = UnsafeList<IntPtr>.Create(rootBufferSize, Allocator.Persistent);
            var freeIndicesList = UnsafeList<int>.Create(1024, Allocator.Persistent);
            
            rootBuffer = new VUnsafeBufferedRef<UnsafeList<IntPtr>>(rootBufferList);
            freeIndices = new VUnsafeBufferedRef<UnsafeList<int>>(freeIndicesList);
            
            freeIndexProcessMarker = 0;
            claimedBuffers = new(1024, Allocator.Persistent);
        }

        public void Dispose()
        {
            using (spinLock.Scoped())
            {
                // Dispose All Buffers
                for (int i = 0; i < RootBuffer->Length; i++)
                {
                    var unsafeList = (UnsafeList<byte>*) (*RootBuffer)[i];
                    if (unsafeList->IsCreated)
                        UnsafeList<byte>.Destroy(unsafeList);
                }

                UnsafeList<IntPtr>.Destroy(RootBuffer);
                rootBuffer = default;
                UnsafeList<int>.Destroy(FreeIndices);
                freeIndices = default;
                claimedBuffers.DisposeSafe();
            }
            spinLock.DisposeRefToDefault();
        }

        /// <summary> This should ONLY be called while the structure is locked! </summary>
        public readonly UnsafeList<byte>* GetBuffer(int allocIndex)
        {
            DemandIsLocked();
            
            if (allocIndex < 0 || allocIndex >= RootBuffer->Length)
                Debug.LogError($"Index {allocIndex} invalid. List range: {RootBuffer->Length}");
            
            DemandIsLocked();
            
            // Verbose on purpose
            // Resizing of the root buffer is protected by the buffer holding only list ptrs, not the actual list buffers themselves.
            // Get list ptr element (not a ptr to it!)
            IntPtr listPtr = (*RootBuffer)[allocIndex];
            
            DemandIsLocked();
            
            // Cast to UnsafeList<byte> ptr
            return (UnsafeList<byte>*) listPtr;
            //return (UnsafeList<byte>*) ((*RootBuffer)[allocIndex]);
        }

        /// <summary> Logs an error if the buffer pool data is accessed without a lock.
        /// Not a super tight check, but should catch any egregious use. </summary>
        [Conditional("LOCK_CHECKS")]
        public readonly void DemandIsLocked()
        {
            // Verify lock! (not a super tight check, but should catch any egregious use.
            if (!spinLock.Locked)
                Debug.LogError("Buffer pool data accessed without lock!");
        }
    }

    /// <summary> Buffer wrapper that enables typed functionality and contains allocation data.
    /// NOTE: This may be slower than necessary due to safety checks, it's not a big dealio, safety checks can be rolled back or bypassed when appropriate. </summary>
    public unsafe struct UnsafeBufferClaim<T>
        where T : unmanaged
    {
        // Byte buffer, reinterpreted on the fly as a generic buffer
        [NativeDisableUnsafePtrRestriction] public VUnsafeBufferedRef<UnsafeList<byte>> guardedBuffer;
        public readonly int poolAddress;
        [NativeDisableUnsafePtrRestriction] readonly UnsafeBufferPoolData* bufferPoolData;
        readonly int sizeofT;

        public UnsafeBufferClaim(UnsafeList<byte>* buffer, UnsafeBufferPoolData* bufferPoolData, int poolAddress)
        {
            // Wrapper, not allocated
            this.guardedBuffer = new VUnsafeBufferedRef<UnsafeList<byte>>(buffer);
            this.poolAddress = poolAddress;
            this.bufferPoolData = bufferPoolData;
            this.sizeofT = sizeof(T);
        }

        public bool IsCreated
        {
            get
            {
                if (!guardedBuffer.TryGetTPtr(out var listPtr))
                    return false;
                if (!listPtr->IsCreated)
                    return false;
                return listPtr->Ptr != null;
            }
        }

        public int Length
        {
            get => ByteToTypeIndex(Buffer->Length);
            set => Buffer->Length = TypeToByteIndex(value);
        }

        public int Capacity
        {
            get => ByteToTypeIndex(Buffer->Capacity, true); // Suppresses warnings capacity is not a multiple of sizeof(T)
            set => Buffer->Capacity = TypeToByteIndex(value);
        }

        UnsafeList<byte>* Buffer
        {
            get
            {
#if SAFETY
                if (!IsCreated)
                    throw new InvalidOperationException("Buffer is not created or is otherwise invalid!");
#endif
                return guardedBuffer.TPtr;
            }
        }

        public byte* DataPtr => Buffer->Ptr;

        public T* TPtr => (T*) Buffer->Ptr;

        ///<summary> Don't play with this list, very dangerous! </summary>
        public UnsafeList<T> AsReadOnly => new((T*)DataPtr, Length);

        ///<summary> Use ReadUnsafe to bypass getter safety. </summary>
        public T this[int index]
        {
            get
            {
#if SAFETY
                if (index < 0 || index >= Length)
                {
                    Debug.LogError($"Invalid index '{index}' outside valid range [0-{Length - 1}");
                    return default;
                }
#endif
                return ReadUnsafe(index); //*(TPtr + index);
                //return *(T*) ((IntPtr) buffer->Ptr + index * sizeofT);
            }
            //(*buffer).ReinterpretLoadUnsafe<byte, T>(index * sizeofT);
            set
            {
#if SAFETY
                if (index < 0 || index >= Length)
                {
                    Debug.LogError($"Invalid index '{index}' outside valid range [0-{Length - 1}");
                    return;
                }
#endif
                TPtr[index] = value;
                //(*buffer).ReinterpretStore(index * sizeofT, value);
            }
        }

        /// <summary> Not concurrent! </summary> 
        public void EnsureCapacity(int capacityTypeStride)
        {
            if (Capacity < capacityTypeStride)
                Capacity = capacityTypeStride;
        }

        ///<summary> Read. (YOU MUST CHECK SAFETY YOURSELF) </summary>
        T ReadUnsafe(int index) => TPtr[index];

        /// <summary> Not concurrent! </summary> 
        public void Add(T value)
        {
            // Take end of buffer as write dest target
            //var writeByteIndex = buffer->Length;
            
            // Take length as target
            var writeTypeIndex = Length;
            // Increase byte buffer length
            Buffer->Length += sizeofT;
            // Write
            this[writeTypeIndex] = value;
            
            //(*buffer).ReinterpretStore(writeByteIndex, value);
        }

        /// <summary> Not concurrent! </summary> 
        public void Insert(int index, T value)
        {
#if SAFETY
            if (index < 0 || index > Length)
            {
                Debug.LogError($"Invalid index '{index}' outside valid range [0-{Length}");
                return;
            }
#endif
            // Shift values over to make a space, then write
            int byteIndex = TypeToByteIndex(index);
            int end = byteIndex + sizeofT;
            // Push length - DON'T DO THIS, INSERT RANGE DOES IT
            //buffer->Length = math.max(buffer->Length, end);
            Buffer->InsertRangeWithBeginEnd(byteIndex, end);
            this[index] = value;
        }

        /// <summary> Not concurrent! </summary> 
        public void RemoveAt(int index)
        {
            int indexByte = TypeToByteIndex(index);
            Buffer->RemoveRange(indexByte, sizeofT);
        }

        /// <summary> Not concurrent! </summary> 
        public void RemoveAtSwapback(int index)
        {
            int indexByte = TypeToByteIndex(index);
            Buffer->RemoveRangeSwapBack(indexByte, sizeofT);
        }
        
        /// <summary> Not concurrent! </summary> 
        public void Clear() => Buffer->Clear();

        /// <summary> Not concurrent! </summary> 
        public bool TryGetValue(int index, out T value, bool loggingOnFail = true)
        {
            value = default;
            if (!IsCreated)
            {
                if (loggingOnFail)
                    Debug.LogError("Buffer is not created!");
                return false;
            }
            if (index < 0 || index >= Length)
            {
                if (loggingOnFail)
                    Debug.LogError($"Invalid index '{index}' outside valid range [0-{Length - 1}");
                return false;
            }
            //value = this[index];
            // Already checked safety, bypass
            value = ReadUnsafe(index);
            return true;
        }

        /// <summary> Will resize the buffer to fit the count if the buffer is too small, if the buffer is bigger it will copy the entire results of the array without shortening itself. </summary>
        public void CopyFrom(NativeArray<T> array, int count = -1)
        {
            // Auto-detect count
            if (count < 0)
                count = array.Length;
            // Resize
            if (Length < count)
                Length = count;
            // Copy
            UnsafeUtility.MemCpy(TPtr, array.GetUnsafeReadOnlyPtr(), UnsafeUtility.SizeOf<T>() * count);
        }

        ///<summary> Internal call that doesn't reset underlying reference memory! </summary>
        internal void InternalOnlyCall_Release(bool logIfBufferAlreadyDisposed = true)
        {
            if (!IsCreated)
            {
                if (logIfBufferAlreadyDisposed)
                    Debug.LogError("Buffer is not created!");
                return;
            }
            UnsafeBufferPool.ReleaseBuffer(bufferPoolData, ref this, logIfBufferAlreadyDisposed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int ByteToTypeIndex(int byteIndex, bool suppressSensitiveCheck = false)
        {
            var typeIndex = byteIndex / sizeofT;
             
#if UNITY_EDITOR
            if (!suppressSensitiveCheck)
            {
                // Do an extra sensitive float check to see if we're in the middle of a float
                float indexF = byteIndex / (float) sizeofT;
                if (!indexF.Equals(math.round(indexF)))
                    Debug.LogError($"Byte index {byteIndex} is not a multiple of {sizeofT}!");
            }
#endif
            return typeIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int TypeToByteIndex(int typeIndex)
        {
            // No sensitive float check here, because we're always going to be a multiple of sizeofT, an int
            return typeIndex * sizeofT;
        }
    }

    public static class UnsafeBufferClaimExt
    {
        public static void Release<T>(this ref UnsafeBufferClaim<T> buffer, bool logIfBufferAlreadyDisposed = true)
            where T : unmanaged
        {
            buffer.InternalOnlyCall_Release(logIfBufferAlreadyDisposed);
            buffer = default;
        }
        
        /// <summary> Returns false if the buffer already contains the value. Not concurrent-write safe! </summary>
        public static bool AddUnique<T>(this ref UnsafeBufferClaim<T> buffer, T value)
            where T : unmanaged, IEquatable<T>
        {
            if (IndexOf(buffer, value) >= 0)
                return false;
            
            buffer.Add(value);
            return true;
        }
        
        // Extension that gets the index of a value in an UnsafeBufferClaim<T>
        public static int IndexOf<T>(this in UnsafeBufferClaim<T> buffer, T value)
            where T : unmanaged, IEquatable<T>
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].Equals(value))
                    return i;
            }
            return -1;
        }
        
        /// <summary> Returns true if the buffer contains the value. Not concurrent-write safe! </summary>
        public static bool Contains<T>(this in UnsafeBufferClaim<T> buffer, T value)
            where T : unmanaged, IEquatable<T>
        {
            return IndexOf(buffer, value) >= 0;
        }
        
        // Extension that uses IndexOf extension to call RemoveAt on an UnsafeBufferClaim<T>
        public static void Remove<T>(this ref UnsafeBufferClaim<T> buffer, T value)
            where T : unmanaged, IEquatable<T>
        {
            int index = buffer.IndexOf(value);
            if (index >= 0)
                buffer.RemoveAt(index);
        }
    }

    /// <summary> Sweep over unused buffer lists and shrink them if they are big, we don't need to keep tons of memory allocated.
    /// Bigly allocating systems should manage their own memory, UnsafeBufferPool system is optimized for lots of small buffers. </summary>
    [BurstCompile]
    unsafe struct MemoryManageJob : IJob
    {
        [NativeDisableUnsafePtrRestriction] UnsafeBufferPoolData* data;

        public MemoryManageJob(UnsafeBufferPoolData* data) => this.data = data;

        public void Execute()
        {
            int processed = 0;
            int shrunk = 0;

            using var lockHandle = data->spinLock.Scoped();

            data->DemandIsLocked();
            
            bool error = true;
            try
            {
                while (processed < UnsafeBufferPool.BufferCheckCount &&
                       shrunk < UnsafeBufferPool.BufferShrinkCount &&
                       data->freeIndexProcessMarker < data->FreeIndices->Length)
                {
                    processed++;

                    var freeIndex = (*data->FreeIndices)[data->freeIndexProcessMarker];
                    var buffer = data->GetBuffer(freeIndex);
                    
                    // Ensure Exists
                    if (!buffer->IsCreated)
                    {
                        Debug.LogError("Error while managing UnsafeBufferPool memory: Free buffer is NOT CREATED!!!");
                        continue;
                    }
                    
                    // Shrink
                    if (buffer->Capacity > data->unsafeBufferInitialSize)
                    {
                        data->DemandIsLocked();
                        // Shrinking buffer should always reallocate and copy unless size is very small or the same
                        buffer->Capacity = data->unsafeBufferInitialSize;
                        shrunk++;
                    }

                    data->freeIndexProcessMarker++;
                }

                if (data->freeIndexProcessMarker >= data->FreeIndices->Length)
                    data->freeIndexProcessMarker = 0;

                error = false;
            }
            finally { }
            if (error)
                Debug.LogError("There was an error while running MemoryManageJob.Execute!");
            
#if LOG_BUFFER_SHRINKS
            if (shrunk > 0)
                Debug.Log($"Buffers Shrunk: {shrunk}");
#endif
        }
    }
}