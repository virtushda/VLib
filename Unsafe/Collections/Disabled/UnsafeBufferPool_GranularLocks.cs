/*#if UNITY_EDITOR
//#define LOG_BUFFER_SHRINKS
#endif

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using JetBrains.Annotations;
using Sirenix.Serialization;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace VLib
{
    /// <summary> A large pool of custom memory buffers, on retrieval these buffers can be 'typed' to any unmanaged type! </summary>
    public readonly unsafe struct UnsafeBufferPool : IDisposable, IMemoryReporter
    {
        // Per processing pass, how many buffers to process
        public const int BufferCheckCount = 2048;
        public const int BufferShrinkCount = 64;

        // RootBuffer -> UnsafeBuffers
        [NativeDisableUnsafePtrRestriction] readonly VUnsafeBufferedRef<UnsafeBufferPoolData> dataBufferPtr;
        public readonly UnsafeBufferPoolData* Data => dataBufferPtr.TPtr;

        public bool IsCreated => dataBufferPtr.IsValid;

        public UnsafeBufferPool(GlobalBurstTimer burstTimer, int initialBufferCount, ushort rootBufferSize = 8192, ushort unsafeBufferSize = 64, bool allowLogging = true) : this()
        {
            // Use new safe stuff instead of raw memory calls
            dataBufferPtr = new VUnsafeBufferedRef<UnsafeBufferPoolData>(new UnsafeBufferPoolData(rootBufferSize, unsafeBufferSize, burstTimer), Allocator.Persistent);
            
            //data = (UnsafeBufferPoolData*) UnsafeUtility.Malloc(UnsafeUtility.SizeOf<UnsafeBufferPoolData>(), UnsafeUtility.AlignOf<UnsafeBufferPoolData>(), Allocator.Persistent);
            //var dataStruct = new UnsafeBufferPoolData(rootBufferSize, unsafeBufferSize);
            //UnsafeUtility.CopyStructureToPtr(ref dataStruct, data);

            // Create initial buffers
            EnsureCapacity(initialBufferCount);

            /*Debug.Log($"UnsafeBufferPool Maximum Memory Footprint: " +
                      $"{(rootBufferSize * subBufferSize * (unsafeBufferSize + UnsafeUtility.SizeOf<UnsafeBuffer>())).AsDataSizeInBytesToReadableString()}");
            Debug.Log($"UnsafeBufferPool Init Footprint: {ReportBytes().AsDataSizeInBytesToReadableString()}");#1#
        }

        /// <summary> Disposes ALL buffers, claimed buffers merely have exclusive access, they don't "remove" the buffers themselves from the pool. </summary>
        public void Dispose()
        {
            if (Data->processJobLaunched)
                Data->processHandle.Complete();

            Data->Dispose();
            dataBufferPtr.Dispose(); // Should be ok...
            //UnsafeUtility.Free(Data, Allocator.Persistent);
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
        public UnsafeBufferClaim<T> ClaimBuffer<T>()
            where T : unmanaged
        {
            var data = Data;
            return data->ClaimBuffer<T>(data);
        }

        /// <summary> Thread-safe! </summary> 
        public void ReleaseBuffer<T>(UnsafeBufferClaim<T> bufferClaim, bool logIfBufferAlreadyDisposed = true)
            where T : unmanaged
        {
            // If the data is not created
            if (IsCreated)
                UnsafeBufferPoolData.ReleaseBuffer(Data, bufferClaim, logIfBufferAlreadyDisposed);
            else if (logIfBufferAlreadyDisposed)
                Debug.LogWarning("BufferPool not created, release call is superfluous!");
        }

        /// <summary> Very dangerous method, bypasses internal thread lock! Can be used for fast batch operations with data->Lock and data->Unlock. </summary>
        /*public void ReleaseBufferNoLock<T>(UnsafeBufferClaim<T> bufferClaim)
            where T : unmanaged
        {
            // Clear data fast
            if (bufferClaim.guardedBuffer.TryGetTPtr(out var bufferPtr))
                bufferPtr->Clear();
            else
                Debug.LogError("Buffer pointer is null, zero or corrupt!");
            // Release claim
            Data->claimedBuffers.Remove(bufferClaim.poolAddress);
            // Return index
            Data->FreeIndices->Add(bufferClaim.poolAddress);
        }#1#

        /// <summary> Recommend to Lock before calling this!!!!!!!!!!!!!!!!!!!!!!!!!!!! </summary> 
        void EnsureCapacity(int capacity) => Data->EnsureCapacity(capacity);

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

        // LOCKS (multiple to try to increase throughput)
        /// <summary>Thread-lock value... no touchy! 0 == unlocked, 1 or non-zero == locked </summary>
        //public volatile int globalLock;
        public BurstSpinLockReadWrite rootBufferLock;
        public BurstSpinLockReadWrite freeIndicesLock;
        public BurstSpinLockReadWrite claimedBuffersLock;

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
                                      FreeIndices != null && FreeIndices->IsCreated;#1#

        public UnsafeBufferPoolData(ushort rootBufferSize, ushort unsafeBufferInitialSize, in GlobalBurstTimer burstTimer) : this()
        {
            this.rootBufferSize = rootBufferSize;
            this.unsafeBufferInitialSize = (ushort) math.max(unsafeBufferInitialSize, 64);

            rootBufferLock = new BurstSpinLockReadWrite(Allocator.Persistent, burstTimer);
            freeIndicesLock = new BurstSpinLockReadWrite(Allocator.Persistent, burstTimer);
            claimedBuffersLock = new BurstSpinLockReadWrite(Allocator.Persistent, burstTimer);
            
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
            LockAll();
            
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
            
            UnlockAll();
            
            rootBufferLock.Dispose();
            freeIndicesLock.Dispose();
            claimedBuffersLock.Dispose();
        }

        public void LockAll()
        {
            rootBufferLock.EnterExclusive(1f);
            freeIndicesLock.EnterExclusive(1f);
            claimedBuffersLock.EnterExclusive(1f);
        }
        
        public void UnlockAll()
        {
            rootBufferLock.ExitExclusive();
            freeIndicesLock.ExitExclusive();
            claimedBuffersLock.ExitExclusive();
        }
        
        /#1#// <summary> Brute force thread lock. When this method completes, the lock is acquired. Be careful manually calling this! </summary>
        public void Lock()
        {
            while (Interlocked.CompareExchange(ref globalLock, 1, 0) != 0) { }
        }#1#

        /// <summary> Instantly forces an unlock, no matter what. Be careful manually calling this! </summary>
        //public void Unlock() => Interlocked.Exchange(ref globalLock, 0);

        #region Methods

        /// <summary> Auto read-locks </summary>
        public UnsafeList<byte>* GetBuffer(int allocIndex)
        {
            if (allocIndex < 0)
            {
                Debug.LogError($"Index {allocIndex} invalid. List range: {RootBuffer->Length}");
                return null;
            }
            
            using var readLock = rootBufferLock.ScopedReadLock(1f);

            return GetBufferInternalNoLock(allocIndex);
        }
        
        /// <summary> Requires external locking to work safely! </summary>
        public UnsafeList<byte>* GetBufferNoLockUnsafe(int allocIndex)
        {
            if (allocIndex < 0)
            {
                Debug.LogError($"Index {allocIndex} invalid. List range: {RootBuffer->Length}");
                return null;
            }

            return GetBufferInternalNoLock(allocIndex);
        }
        
        UnsafeList<byte>* GetBufferInternalNoLock(int allocIndex)
        {
            if (allocIndex >= RootBuffer->Length)
            {
                Debug.LogError($"Index {allocIndex} invalid. List range: {RootBuffer->Length}");
                return null;
            }

            // Verbose on purpose
            // Resizing of the root buffer is protected by the buffer holding only list ptrs, not the actual list buffers themselves.
            // Get list ptr element (not a ptr to it!)
            IntPtr listPtr = (*RootBuffer)[allocIndex];
            // Cast to UnsafeList<byte> ptr
            return (UnsafeList<byte>*) listPtr;
            //return (UnsafeList<byte>*) ((*RootBuffer)[allocIndex]);
        }
        
        /// <summary> Thread-safe! </summary> 
        public UnsafeBufferClaim<T> ClaimBuffer<T>(UnsafeBufferPoolData* dataPtr)
            where T : unmanaged
        {
            int claimIndex = 0;
            UnsafeList<byte>* bufferPtr = (UnsafeList<byte>*) IntPtr.Zero;
            bool error = true;

            //Lock(); // No more global locks
            try
            {
                bool freeIndicesHasSomething = false;

                using (var freeIndicesReadLock = freeIndicesLock.ScopedReadLock(1f))
                {
                    /*if (!freeIndicesReadLock)
                        Debug.LogError("Failed to acquire free indices read lock!");#1#
                    
                    freeIndicesHasSomething = !FreeIndices->IsEmpty;
                }

                // Try get free index
                if (freeIndicesHasSomething)
                {
                    using var freeIndicesWriteLock = freeIndicesLock.ScopedExclusiveLock(1f);
                    
                    // TODO: This could be tighter..
                    
                    int indexOfIndex = FreeIndices->Length - 1;
                    claimIndex = (*FreeIndices)[indexOfIndex];
                    FreeIndices->RemoveAt(indexOfIndex);
                }
                
                // Else claim the next pool alloc index in a thread safe way
                if (!freeIndicesHasSomething)
                    claimIndex = Interlocked.Increment(ref poolAllocIndex) - 1; // Subtract from thread-safe increment so we know we actually got the right value

                using (claimedBuffersLock.ScopedExclusiveLock(1f))
                {
                    if (!claimedBuffers.Add(claimIndex))
                        Debug.LogError($"Buffer {claimIndex} already claimed!");
                }

                EnsureCapacity(claimIndex + 1);
                
                // Retrieve actual buffer (this should be inside lock to stop it from reading when rootbuffer capacity is being increased!)
                bufferPtr = GetBuffer(claimIndex);

                error = false;
            }
            finally
            {
            }
            //Unlock();

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

            // Return in wrapper
            return new UnsafeBufferClaim<T>(bufferPtr, dataPtr, claimIndex);
        }

        /// <summary> Auto locks </summary>
        public static void ReleaseBuffer<T>(UnsafeBufferPoolData* bufferPoolData, UnsafeBufferClaim<T> bufferClaim, bool logIfBufferAlreadyDisposed = true)
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
            //bufferPoolData->Lock();
            
            bool removedFromClaimed = false;
            using (var claimedBuffersWriteLock = bufferPoolData->claimedBuffersLock.ScopedExclusiveLock(1f))
            {
                removedFromClaimed = bufferPoolData->claimedBuffers.Remove(bufferClaim.poolAddress);
            }

            if (removedFromClaimed)
            {
                using var freeIndicesWriteLock = bufferPoolData->freeIndicesLock.ScopedExclusiveLock(1f);
                bufferPoolData->FreeIndices->Add(bufferClaim.poolAddress);
            }

            /*else
            {
                Debug.LogError($"Buffer {bufferClaim.poolAddress} not claimed!");
            }#1#

            //bufferPoolData->Unlock();
        }

        /#1#// <summary> Very dangerous method, bypasses internal thread lock! Can be used for fast batch operations with data->Lock and data->Unlock. </summary>
        public void ReleaseBufferNoLock<T>(UnsafeBufferClaim<T> bufferClaim)
            where T : unmanaged
        {
            // Clear data fast
            if (bufferClaim.guardedBuffer.TryGetTPtr(out var bufferPtr))
                bufferPtr->Clear();
            else
                Debug.LogError("Buffer pointer is null, zero or corrupt!");
            // Release claim
            claimedBuffers.Remove(bufferClaim.poolAddress);
            // Return index
            FreeIndices->Add(bufferClaim.poolAddress);
        }#1#

        /// <summary> Auto locks </summary>
        public void EnsureCapacity(int capacity)
        {
            using (rootBufferLock.ScopedReadLock(1f))
            {
                if (capacity <= RootBuffer->Length)
                    return;
            }

            using (rootBufferLock.ScopedExclusiveLock(1f))
            {
                if (RootBuffer->Capacity < capacity)
                    RootBuffer->Capacity = capacity;
            
                while (RootBuffer->Length < capacity)
                {
                    RootBuffer->AddNoResize((IntPtr)UnsafeList<byte>.Create(unsafeBufferInitialSize, Allocator.Persistent));
                }
            }
        }

        #endregion
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

        public bool IsCreated => guardedBuffer.IsValid && guardedBuffer.TPtr->IsCreated && guardedBuffer.TPtr->Ptr is not null;
        
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
                if (!IsCreated)
                    throw new InvalidOperationException("Buffer is not created or is otherwise invalid!");
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
                if (index < 0 || index >= Length)
                {
                    Debug.LogError($"Invalid index '{index}' outside valid range [0-{Length - 1}");
                    return default;
                }
                
                return ReadUnsafe(index); //*(TPtr + index);
                //return *(T*) ((IntPtr) buffer->Ptr + index * sizeofT);
            }
            //(*buffer).ReinterpretLoadUnsafe<byte, T>(index * sizeofT);
            set
            {
                if (index < 0 || index >= Length)
                {
                    Debug.LogError($"Invalid index '{index}' outside valid range [0-{Length - 1}");
                    return;
                }
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

        ///<summary> Read. </summary>
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
            TPtr[writeTypeIndex] = value;
            
            //(*buffer).ReinterpretStore(writeByteIndex, value);
        }

        /// <summary> Not concurrent! </summary> 
        public void Insert(int index, T value)
        {
            if (index < 0 || index > Length)
            {
                Debug.LogError($"Invalid index '{index}' outside valid range [0-{Length}");
                return;
            }
            
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
            UnsafeBufferPoolData.ReleaseBuffer(bufferPoolData, this, logIfBufferAlreadyDisposed);
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

            data->LockAll();

            bool error = true;
            try
            {
                while (processed < UnsafeBufferPool.BufferCheckCount &&
                       shrunk < UnsafeBufferPool.BufferShrinkCount &&
                       data->freeIndexProcessMarker < data->FreeIndices->Length)
                {
                    processed++;

                    var freeIndex = (*data->FreeIndices)[data->freeIndexProcessMarker];
                    var buffer = data->GetBufferNoLockUnsafe(freeIndex);
                    
                    // Ensure Exists
                    if (!buffer->IsCreated)
                    {
                        Debug.LogError("Error while managing UnsafeBufferPool memory: Free buffer is NOT CREATED!!!");
                        continue;
                    }
                    
                    // Shrink
                    if (buffer->Capacity > data->unsafeBufferInitialSize)
                    {
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

            data->UnlockAll();
            
#if LOG_BUFFER_SHRINKS
            if (shrunk > 0)
                Debug.Log($"Buffers Shrunk: {shrunk}");
#endif
        }
    }
}*/