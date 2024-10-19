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
    /*[DebuggerDisplay("Count = {m_HashMapData.Count()}, Capacity = {m_HashMapData.Capacity}, IsCreated = {m_HashMapData.IsCreated}, IsEmpty = {IsEmpty}")]*/
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
    public unsafe struct NativeHashMapOfLists<TKey, TValue>
        : IDisposable
        , IEnumerable<KeyValue<TKey, TValue>> // Used by collection initializers.
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        VUnsafeRef<UnsafeParallelHashMap<TKey, VUnsafeList<TValue>>> listMap;
        Allocator listAllocator;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
        static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeHashMapOfLists<TKey, TValue>>();

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
        public NativeHashMapOfLists(int capacity, AllocatorManager.AllocatorHandle allocator, Allocator listAllocator)
            : this(capacity, allocator, listAllocator, 2)
        {
        }

        NativeHashMapOfLists(int capacity, AllocatorManager.AllocatorHandle allocator, Allocator listAllocator, int disposeSentinelStackDepth)
        {
            this.listAllocator = listAllocator;
            var map = new UnsafeParallelHashMap<TKey, VUnsafeList<TValue>>(capacity, allocator);
            listMap = new VUnsafeRef<UnsafeParallelHashMap<TKey, VUnsafeList<TValue>>>(map, allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
#if REMOVE_DISPOSE_SENTINEL
            m_Safety = CollectionHelper.CreateSafetyHandle(allocator);
#else
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, disposeSentinelStackDepth, allocator.ToAllocator);
#endif

            CollectionHelper.SetStaticSafetyId<NativeParallelHashMap<TKey, TValue>>(ref m_Safety, ref s_staticSafetyId.Data);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
#endif
        }

        /// <summary>Releases all resources (memory and safety handles).</summary>
        public void Dispose()
        {
            Clear();
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
#if REMOVE_DISPOSE_SENTINEL
            CollectionHelper.DisposeSafetyHandle(ref m_Safety);
#else
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif
#endif
            listMap.ValueRef.Dispose();
            listMap.DisposeRefToDefault();
        }

        /// <summary>Whether this hash map has been allocated (and not yet deallocated).</summary>
        public bool IsCreated => listMap.ValueRef.IsCreated;

        /// <summary>
        /// Whether this hash map is empty.
        /// </summary>
        /// <value>True if this hash map is empty or if the map has not been constructed.</value>
        public bool IsEmpty
        {
            get
            {
                if (!IsCreated)
                    return true;
                CheckRead();
                return listMap.ValueRef.IsEmpty;
            }
        }

        /// <summary>
        /// The current number of key-value pairs in this hash map.
        /// </summary>
        /// <returns>The current number of key-value pairs in this hash map.</returns>
        public int Count()
        {
            CheckRead();
            return listMap.ValueRef.Count();
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
                return listMap.ValueRef.Capacity;
            }

            // Do we need this?
            /*set
            {
                CheckWrite();
                ref var mapRef = ref listMap.ValueRef;
                
                
                
                //Special handling for shrinking, need to release internal list memory
                if (value < mapRef.m_HashMapData.Capacity)
                    DetachUsedLists(false, value);

                mapRef.m_HashMapData.Capacity = value;
            }*/
        }

        /// <summary>
        /// Removes all key-value pairs. Disposes all lists.
        /// </summary>
        /// <remarks>Does not change the capacity.</remarks>
        public void Clear()
        {
            CheckWrite();
            ref var mapRef = ref listMap.ValueRef;
            foreach (var pair in mapRef)
            {
                if (pair.Value.IsCreated)
                    pair.Value.DisposeRefToDefault();
            }
            mapRef.Clear();
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
            ref var mapRef = ref listMap.ValueRef;
            if (!mapRef.TryGetValue(key, out var list))
            {
                list = new VUnsafeList<TValue>(listAllocator);
                mapRef.Add(key, list);
            }
            list.Add(item);
        }

        /// <summary>
        /// Removes a key-value pair.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns>True if a key-value pair was removed.</returns>
        public bool RemoveList(TKey key)
        {
            CheckWrite();
            
            ref var listMapRef = ref listMap.ValueRef;
            if (listMapRef.TryGetValue(key, out var list))
            {
                list.Dispose();
                listMapRef.Remove(key);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the value associated with a key.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="list">Outputs the value associated with the key. Outputs default if the key was not present.</param>
        /// <returns>True if the key was present.</returns>
        public bool TryGetList(TKey key, out VUnsafeList<TValue> list)
        {
            CheckRead();
            if (listMap.ValueRef.TryGetValue(key, out list))
                return true;
            
            list = default;
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
            return listMap.ValueRef.ContainsKey(key);
        }

        /// <summary>Gets value lists by key.</summary>
        /// <remarks>Getting a key that is not present will throw.</remarks>
        public VUnsafeList<TValue> this[TKey key]
        {
            get
            {
                CheckRead();
                if (listMap.ValueRef.TryGetValue(key, out var list))
                    return list;
                ThrowKeyNotPresent(key);
                return default;
            }
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
            return listMap.ValueRef.GetKeyArray(allocator);
        }

        /// <summary>Returns an array with a copy of all unsafe list ptrs. (in no particular order).
        /// To use, cast the IntPtrs to UnsafeList[TValue]*</summary>
        /// <param name="allocator">The allocator to use.</param>
        public NativeArray<VUnsafeList<TValue>> GetValueArray(AllocatorManager.AllocatorHandle allocator)
        {
            CheckRead();
            return listMap.ValueRef.GetValueArray(allocator);
        }

        /// <summary>Returns a NativeKeyValueArrays with a copy of all this hash map's keys and values. To use, cast the IntPtrs to UnsafeList[TValue]*</summary>
        /// <remarks>The key-value pairs are copied in no particular order. For all `i`, `Values[i]` will be the value associated with `Keys[i]`.</remarks>
        /// <param name="allocator">The allocator to use.</param>
        public NativeKeyValueArrays<TKey, VUnsafeList<TValue>> GetKeyValueArrays(AllocatorManager.AllocatorHandle allocator)
        {
            CheckRead();
            return listMap.ValueRef.GetKeyValueArrays(allocator);
        }

        public int GetMemoryFootprintBytes()
        {
            int hashsetCapacity = listMap.ValueRef.Capacity;
            var keyBytes = hashsetCapacity * UnsafeUtility.SizeOf<TKey>();
            var valueBytes = hashsetCapacity * UnsafeUtility.SizeOf<TValue>();
            int bytes = keyBytes + valueBytes;

            using var keys = GetKeyArray(Allocator.TempJob);
            for (int i = 0; i < keys.Length; i++)
            {
                if (TryGetList(keys[i], out var list))
                    bytes += list.Capacity * UnsafeUtility.SizeOf<TValue>();
            }

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