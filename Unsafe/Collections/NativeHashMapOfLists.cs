using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace VLib
{
    /// <summary>
    /// An unordered, expandable associative array where the values are individual <see cref="UnsafeList{T}"/> elements.
    /// Behaves similarly to <see cref="NativeMultiHashMap{TKey,TValue}"/>, but trades some speed for vastly greater memory efficiency.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys.</typeparam>
    /// <typeparam name="TValue">The type of the values.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [DebuggerDisplay("Count = {m_HashMapData.Count()}, Capacity = {m_HashMapData.Capacity}, IsCreated = {m_HashMapData.IsCreated}, IsEmpty = {IsEmpty}")]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
    public unsafe struct NativeHashMapOfLists<TKey, TValue>
        : IDisposable
        , IEnumerable<KeyValue<TKey, TValue>> // Used by collection initializers.
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        /// <summary>Ptr -> UnsafeList[TValue]</summary>
        internal UnsafeParallelHashMap<TKey, IntPtr> m_HashMapData;
        internal UnsafePtrList<UnsafeList<TValue>> m_ListPool;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
        static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeParallelHashMap<TKey, TValue>>();

#if REMOVE_DISPOSE_SENTINEL
#else
        [NativeSetClassTypeToNullOnSchedule]
        DisposeSentinel m_DisposeSentinel;
#endif
#endif

        /// <summary>
        /// Initializes and returns an instance of NativeHashMap.
        /// </summary>
        /// <param name="capacity">The number of key-value pairs that should fit in the initial allocation.</param>
        /// <param name="allocator">The allocator to use.</param>
        public NativeHashMapOfLists(int capacity, AllocatorManager.AllocatorHandle allocator)
            : this(capacity, allocator, 2)
        {
        }

        NativeHashMapOfLists(int capacity, AllocatorManager.AllocatorHandle allocator, int disposeSentinelStackDepth)
        {
            m_HashMapData = new UnsafeParallelHashMap<TKey, IntPtr>(capacity, allocator);
            m_ListPool = new UnsafePtrList<UnsafeList<TValue>>(capacity, Allocator.Persistent);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
#if REMOVE_DISPOSE_SENTINEL
            m_Safety = CollectionHelper.CreateSafetyHandle(allocator);
#else
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, disposeSentinelStackDepth, allocator.ToAllocator);
#endif

            CollectionHelper.SetStaticSafetyId<NativeParallelHashMap<TKey, TValue>>(ref m_Safety, ref s_staticSafetyId.Data);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
#endif
            
            FitListPoolToCapacity();
        }

        /// <summary>
        /// Whether this hash map is empty.
        /// </summary>
        /// <value>True if this hash map is empty or if the map has not been constructed.</value>
        public bool IsEmpty
        {
            get
            {
                if (!IsCreated)
                {
                    return true;
                }

                CheckRead();
                return m_HashMapData.IsEmpty;
            }
        }

        /// <summary>
        /// The current number of key-value pairs in this hash map.
        /// </summary>
        /// <returns>The current number of key-value pairs in this hash map.</returns>
        public int Count()
        {
            CheckRead();
            return m_HashMapData.Count();
        }

        /// <summary>
        /// The number of key-value pairs that fit in the current allocation.
        /// </summary>
        /// <value>The number of key-value pairs that fit in the current allocation.</value>
        /// <param name="value">A new capacity. Must be larger than the current capacity.</param>
        /// <exception cref="Exception">Thrown if `value` is less than the current capacity.</exception>
        public int Capacity
        {
            get
            {
                CheckRead();
                return m_HashMapData.Capacity;
            }

            set
            {
                CheckWrite();
                
                //Special handling for shrinking, need to release internal list memory
                if (value < m_HashMapData.Capacity)
                    DetachUsedLists(false, value);

                m_HashMapData.Capacity = value;
                
                FitListPoolToCapacity();
            }
        }

        /// <summary>
        /// Removes all key-value pairs.
        /// </summary>
        /// <remarks>Does not change the capacity.</remarks>
        public void Clear()
        {
            CheckWrite();
            DetachUsedLists(true);
            FitListPoolToCapacity();
            m_HashMapData.Clear();
        }

        /// <summary>Generate lists or pooled lists up to capacity to facilitate job writing</summary>
        public void FitListPoolToCapacity()
        {
            int capacity = Capacity;
            int poolCount = m_ListPool.Length;
            int hashCount = m_HashMapData.Count();
            
            int poolTarget = capacity - hashCount;
            int poolDeltaToTarget = poolTarget - poolCount;

            //Grow
            if (poolDeltaToTarget > 0)
            {
                for (int i = 0; i < poolDeltaToTarget; i++)
                {
                    m_ListPool.Add((IntPtr) UnsafeList<TValue>.Create(8, Allocator.Persistent));
                }
            }
            //Shrink
            else if (poolDeltaToTarget < 0)
            {
                while (m_ListPool.Length > poolTarget)
                {
                    var lastIndex = m_ListPool.Length - 1;
                    var lastElement = m_ListPool[lastIndex];
                    UnsafeList<TValue>.Destroy(lastElement); // lastElement->Dispose();
                    m_ListPool.RemoveAt(lastIndex);
                }
            }
        }

        /// <summary>Disconnects lists from hash keys, and puts lists back into internal pool.
        /// WARNING: Does not remove hashkeys pointing at null memory! This is a utility function.</summary>
        /// <param name="repool">True: Repools lists, False: Disposes lists</param>
        void DetachUsedLists(bool repool, int start = 0, int stopBefore = int.MaxValue)
        {
            var keyArray = m_HashMapData.GetKeyArray(Allocator.TempJob);

            start = math.max(start, 0);
            stopBefore = math.min(stopBefore, keyArray.Length);
            
            for (int i = start; i < stopBefore; i++)
            {
                var key = keyArray[i];
                if (!TryGetValue(key, out var listPtr))
                    continue;
                
                if (listPtr != null)
                {
                    if (repool)
                    {
                        listPtr->Capacity = 8;
                        m_ListPool.Add(listPtr);
                    }
                    else
                        UnsafeList<TValue>.Destroy(listPtr); // listPtr->Dispose();
                }
            }

            keyArray.Dispose();
        }

        /// <summary>
        /// Adds a new key-value pair.
        /// </summary>
        /// <remarks>If the key is already present, this method throws without modifying the hash map.</remarks>
        /// <param name="key">The key to add.</param>
        /// <param name="item">The value to add.</param>
        /// <exception cref="ArgumentException">Thrown if the key was already present.</exception>
        public void Add(TKey key, TValue item)
        {
            if (!m_HashMapData.TryGetValue(key, out var listPtr))
            {
                if (m_ListPool.Length < 1)
                    throw new OutOfMemoryException("List pool is empty! There must be capacity for keys, set with Capacity property!");
                //Extract list ptr
                int listIndex = m_ListPool.Length - 1;
                listPtr = (IntPtr)m_ListPool[listIndex]; //1 from end
                m_ListPool.RemoveAt(listIndex);
                
                m_HashMapData.Add(key, listPtr);
            }
            ((UnsafeList<TValue>*)listPtr)->Add(item);
        }

        /// <summary>
        /// Removes a key-value pair.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns>True if a key-value pair was removed.</returns>
        public bool Remove(TKey key)
        {
            CheckWrite();
            
            //Repool list if small enough, otherwise dispose and backfill list pool
            if (m_HashMapData.TryGetValue(key, out var listPtr))
            {
                var listPtrTyped = (UnsafeList<TValue>*) listPtr;
                
                /*if (listPtrTyped->Length <= 32) //Repool
                {*/
                
                listPtrTyped->Clear();
                listPtrTyped->Capacity = 8;

                m_ListPool.Add(listPtr);
                return m_HashMapData.Remove(key);
                    
                //This code can't run during a job!
                /*else //Dispose and backfill pool
                {
                    if (listPtrTyped != null)
                        UnsafeList<TValue>.Destroy(listPtrTyped);
                    bool removed = m_HashMapData.Remove(key);
                    GenerateInternalLists();
                    return removed;
                }*/
            }
            return false;
        }

        /// <summary>
        /// Returns the value associated with a key.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="item">Outputs the value associated with the key. Outputs default if the key was not present.</param>
        /// <returns>True if the key was present.</returns>
        public bool TryGetValue(TKey key, out UnsafeList<TValue>* item)
        {
            CheckRead();
            if (m_HashMapData.TryGetValue(key, out var itemPtr))
            {
                item = (UnsafeList<TValue>*) itemPtr;
                return true;
            }
            item = default;
            return false;
        }

        /// <summary>
        /// Returns true if a given key is present in this hash map.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <returns>True if the key was present.</returns>
        public bool ContainsKey(TKey key)
        {
            CheckRead();
            return m_HashMapData.ContainsKey(key);
        }

        /// <summary>Gets value lists by key.</summary>
        /// <remarks>Getting a key that is not present will throw.</remarks>
        public UnsafeList<TValue>* this[TKey key]
        {
            get
            {
                CheckRead();
                if (m_HashMapData.TryGetValue(key, out var listPtr))
                    return (UnsafeList<TValue>*)listPtr;
                ThrowKeyNotPresent(key);
                return default;
            }
        }

        /// <summary>Whether this hash map has been allocated (and not yet deallocated).</summary>
        public bool IsCreated => m_HashMapData.IsCreated;

        /// <summary>Releases all resources (memory and safety handles).</summary>
        public void Dispose()
        {
            DisposeListPool();
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
#if REMOVE_DISPOSE_SENTINEL
            CollectionHelper.DisposeSafetyHandle(ref m_Safety);
#else
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif
#endif
            m_HashMapData.Dispose();
        }

        void DisposeListPool()
        {
            DetachUsedLists(false);
            while (m_ListPool.Length > 0)
            {
                int lastIndex = m_ListPool.Length - 1;
                var listPtr = m_ListPool[lastIndex];
                if (listPtr != null && listPtr->IsCreated && listPtr->Allocator != Allocator.Invalid)
                    UnsafeList<TValue>.Destroy(listPtr); //listPtr->Dispose();
                m_ListPool.RemoveAt(lastIndex);
            }
            m_ListPool.Dispose();
        }

        /// <summary>
        /// Creates and schedules a job that will dispose this hash map.
        /// </summary>
        /// <param name="inputDeps">A job handle. The newly scheduled job will depend upon this handle.</param>
        /// <returns>The handle of a new job that will dispose this hash map.</returns>
        /*[NotBurstCompatible /* This is not burst compatible because of IJob's use of a static IntPtr. Should switch to IJobBurstSchedulable in the future #1#]
        public JobHandle Dispose(JobHandle inputDeps)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
#if REMOVE_DISPOSE_SENTINEL
#else
            // [DeallocateOnJobCompletion] is not supported, but we want the deallocation
            // to happen in a thread. DisposeSentinel needs to be cleared on main thread.
            // AtomicSafetyHandle can be destroyed after the job was scheduled (Job scheduling
            // will check that no jobs are writing to the container).
            DisposeSentinel.Clear(ref m_DisposeSentinel);
#endif

            var jobHandle = new UnsafeHashMapDataDisposeJob { Data = new UnsafeHashMapDataDispose { m_Buffer = m_HashMapData.m_Buffer, m_AllocatorLabel = m_HashMapData.m_AllocatorLabel, m_Safety = m_Safety } }.Schedule(inputDeps);

            AtomicSafetyHandle.Release(m_Safety);
#else
            var jobHandle = new UnsafeHashMapDataDisposeJob { Data = new UnsafeHashMapDataDispose { m_Buffer = m_HashMapData.m_Buffer, m_AllocatorLabel = m_HashMapData.m_AllocatorLabel }  }.Schedule(inputDeps);
#endif
            m_HashMapData.m_Buffer = null;

            return jobHandle;
        }*/

        /// <summary>Returns an array with a copy of all this hash map's keys (in no particular order).</summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array with a copy of all this hash map's keys (in no particular order).</returns>
        public NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            CheckRead();
            return m_HashMapData.GetKeyArray(allocator);
        }

        /// <summary>Returns an array with a copy of all unsafe list ptrs. (in no particular order).
        /// To use, cast the IntPtrs to UnsafeList[TValue]*</summary>
        /// <param name="allocator">The allocator to use.</param>
        public NativeArray<IntPtr> GetValueArray(AllocatorManager.AllocatorHandle allocator)
        {
            CheckRead();
            return m_HashMapData.GetValueArray(allocator);
        }

        /// <summary>Returns a NativeKeyValueArrays with a copy of all this hash map's keys and values. To use, cast the IntPtrs to UnsafeList[TValue]*</summary>
        /// <remarks>The key-value pairs are copied in no particular order. For all `i`, `Values[i]` will be the value associated with `Keys[i]`.</remarks>
        /// <param name="allocator">The allocator to use.</param>
        public NativeKeyValueArrays<TKey, IntPtr> GetKeyValueArrays(AllocatorManager.AllocatorHandle allocator)
        {
            CheckRead();
            return m_HashMapData.GetKeyValueArrays(allocator);
        }

        public int GetMemoryFootprintBytes()
        {
            int hashsetCapacity = m_HashMapData.Capacity;
            var keyBytes = hashsetCapacity * UnsafeUtility.SizeOf<TKey>();
            var valueBytes = hashsetCapacity * UnsafeUtility.SizeOf<TValue>();
            int bytes = keyBytes + valueBytes;

            var keys = GetKeyArray(Allocator.TempJob);
            for (int i = 0; i < keys.Length; i++)
            {
                if (TryGetValue(keys[i], out var listPtr))
                    bytes += listPtr->m_capacity * UnsafeUtility.SizeOf<TValue>();
            }
            keys.Dispose();

            return bytes;
        }

        //Don't feel like trying to support parallel writing, probably not necessary
        /*/// <summary>
        /// Returns a parallel writer for this hash map.
        /// </summary>
        /// <returns>A parallel writer for this hash map.</returns>
        public ParallelWriter AsParallelWriter()
        {
            ParallelWriter writer;
            writer.m_Writer = m_HashMapData.AsParallelWriter();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            writer.m_Safety = m_Safety;
            CollectionHelper.SetStaticSafetyId<ParallelWriter>(ref writer.m_Safety, ref ParallelWriter.s_staticSafetyId.Data);
#endif
            return writer;
        }*/

        /*/// <summary>
        /// A parallel writer for a NativeHashMap.
        /// </summary>
        /// <remarks>
        /// Use <see cref="AsParallelWriter"/> to create a parallel writer for a NativeHashMap.
        /// </remarks>
        [NativeContainer]
        [NativeContainerIsAtomicWriteOnly]
        [DebuggerDisplay("Capacity = {m_Writer.Capacity}")]
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public unsafe struct ParallelWriter
        {
            internal UnsafeHashMap<TKey, TValue>.ParallelWriter m_Writer;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            internal AtomicSafetyHandle m_Safety;
            internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<ParallelWriter>();
#endif
            /// <summary>
            /// Returns the index of the current thread.
            /// </summary>
            /// <remarks>In a job, each thread gets its own copy of the ParallelWriter struct, and the job system assigns
            /// each copy the index of its thread.</remarks>
            /// <value>The index of the current thread.</value>
            public int m_ThreadIndex => m_Writer.m_ThreadIndex;

            /// <summary>
            /// The number of key-value pairs that fit in the current allocation.
            /// </summary>
            /// <value>The number of key-value pairs that fit in the current allocation.</value>
            public int Capacity
            {
                get
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                    return m_Writer.Capacity;
                }
            }

            /// <summary>
            /// Adds a new key-value pair.
            /// </summary>
            /// <remarks>If the key is already present, this method returns false without modifying this hash map.</remarks>
            /// <param name="key">The key to add.</param>
            /// <param name="item">The value to add.</param>
            /// <returns>True if the key-value pair was added.</returns>
            public bool TryAdd(TKey key, TValue item)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
                return m_Writer.TryAdd(key, item);
            }
        }*/

        /// <summary>
        /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
        /// </summary>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator<KeyValue<TKey, TValue>> IEnumerable<KeyValue<TKey, TValue>>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
        /// </summary>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /*/// <summary>
        /// An enumerator over the key-value pairs of a hash map.
        /// </summary>
        /// <remarks>
        /// In an enumerator's initial state, <see cref="Current"/> is not valid to read.
        /// From this state, the first <see cref="MoveNext"/> call advances the enumerator to the first key-value pair.
        /// </remarks>
        [NativeContainer]
        [NativeContainerIsReadOnly]
        public struct Enumerator : IEnumerator<KeyValue<TKey, TValue>>
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            internal AtomicSafetyHandle m_Safety;
#endif
            internal UnsafeHashMapDataEnumerator m_Enumerator;

            /// <summary>
            /// Does nothing.
            /// </summary>
            public void Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next key-value pair.
            /// </summary>
            /// <returns>True if <see cref="Current"/> is valid to read after the call.</returns>
            public bool MoveNext()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_Enumerator.MoveNext();
            }

            /// <summary>
            /// Resets the enumerator to its initial state.
            /// </summary>
            public void Reset()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                m_Enumerator.Reset();
            }

            /// <summary>
            /// The current key-value pair.
            /// </summary>
            /// <value>The current key-value pair.</value>
            public KeyValue<TKey, TValue> Current => m_Enumerator.GetCurrent<TKey, TValue>();

            object IEnumerator.Current => Current;
        }*/

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckRead()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckWrite()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void ThrowKeyNotPresent(TKey key)
        {
            throw new ArgumentException($"Key: {key} is not present in the NativeHashMap.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void ThrowKeyAlreadyAdded(TKey key)
        {
            throw new ArgumentException("An item with the same key has already been added", nameof(key));
        }
    }

    /*[BurstCompile]
    public static class NativeHashMapOfListsUtility
    {
        [BurstCompile]
        public static unsafe void FitListPoolToCapacityBurst<TKey, TValue>(ref NativeHashMapOfLists<TKey, TValue> hashMap)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            int capacity = hashMap.Capacity;
            int poolCount = hashMap.m_ListPool.Length;
            int hashCount = hashMap.m_HashMapData.Count();

            int poolTarget = capacity - hashCount;
            int poolDeltaToTarget = poolTarget - poolCount;

            //Grow
            if (poolDeltaToTarget > 0)
            {
                for (int i = 0; i < poolDeltaToTarget; i++)
                {
                    hashMap.m_ListPool.Add((IntPtr) UnsafeList<TValue>.Create(8, Allocator.Persistent));
                }
            }
            //Shrink
            else if (poolDeltaToTarget < 0)
            {
                while (hashMap.m_ListPool.Length > poolTarget)
                {
                    var lastIndex = hashMap.m_ListPool.Length - 1;
                    var lastElement = hashMap.m_ListPool[lastIndex];
                    UnsafeList<TValue>.Destroy(lastElement); // lastElement->Dispose();
                    hashMap.m_ListPool.RemoveAt(lastIndex);
                }
            }
        }
    }*/
}