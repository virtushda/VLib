using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using VLib.Libraries.VLib.Unsafe.Utility;

namespace VLib
{
    /// <summary> Designed to serve as a native buffer where indices are recycled instead of removed. This allows for safely persisting references to individual elements. </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] {typeof(int)})]
    public struct VUnsafeBufferList<T> : IAllocating, INativeList<T>, IEnumerable<T> // Used by collection initializers.
        where T : unmanaged
    {
        VUnsafeRef<Data> data;

        ref Data DataRef => ref data.ValueRef;
        readonly ref readonly Data DataReadRef => ref data.ValueRef;

        struct Data : IAllocating
        {
            internal UnsafeList<T> listData;
            internal VPackedIndexProvider packedIndices;
            UnsafeList<bool> indicesActive;
            UnsafeHashMap<int, SafePtr<T>> bufferRenters;

            public readonly bool IsCreated => listData.IsCreated;

            public readonly bool SupportsRenting
            {
                get
                {
                    this.ConditionalCheckIsCreated();
                    return bufferRenters.IsCreated;
                }
            }
            
            public readonly int Length
            {
                get
                {
                    this.ConditionalCheckIsCreated(); 
                    return listData.Length;
                }
            }

            public readonly int Capacity
            {
                get 
                { 
                    this.ConditionalCheckIsCreated();
                    return listData.Capacity; 
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
            public void ConditionalCheckSupportsRenting()
            {
                if (!SupportsRenting)
                    throw new InvalidOperationException("This buffer does not support renting SafePtrs.");
            }
            
            public Data(int initialCapacity, Allocator allocator, bool supportsRenting, bool logStillActiveOnDispose)
            {
                listData = new UnsafeList<T>(initialCapacity, allocator, NativeArrayOptions.ClearMemory);
                packedIndices = VPackedIndexProvider.Create(reportAllNotDisposed: logStillActiveOnDispose);
                indicesActive = new (initialCapacity, allocator);
                indicesActive.Length = initialCapacity;
                bufferRenters = supportsRenting ? new (initialCapacity, allocator) : default;
            }
            
            public void Dispose()
            {
                if (!IsCreated)
                    return;
                
                FlushRenters();
                bufferRenters.Dispose();
                
                listData.Dispose();
                indicesActive.Dispose();
                packedIndices.Dispose();
            }

            /// <summary> Tells you whether the index is inside the buffer range or not, without care to whether the index is considered 'active'. </summary>
            public readonly bool IndexInBufferRange(int index)
            {
                this.ConditionalCheckIsCreated();
                return index < Length && index >= 0;
            }

            /// <summary> Tells you whether the index is inside the buffer range and is considered 'active'. </summary>
            public bool IndexActive(int index)
            {
                // Rely on conditional check inside this call
                if (!IndexInBufferRange(index))
                    return false;
                return indicesActive[index];
            }

            public unsafe SafePtr<T> RentIndexPointer(int index)
            {
                ConditionalCheckSupportsRenting();
                if (!listData.IsIndexValid(index))
                    throw new IndexOutOfRangeException($"Index {index} is out of range in VUnsafeBufferList.");
                if (!indicesActive[index])
                    throw new InvalidOperationException($"Index {index} is not active in VUnsafeBufferList.");
                if (bufferRenters.TryGetValue(index, out var renter))
                {
                    if (renter.IsCreated)
                        return renter;
                    // If renter pointer is disposed, remove it and continue
                    bufferRenters.Remove(index);
                }

                // Generate new renter
                var safePtr = new SafePtr<T>(listData.GetListElementPtr(index));
                bufferRenters.Add(index, safePtr);
                return safePtr;
            }

            public void FlushRenters()
            {
                CheckNoRenters();
                DisposeAllRenters();
            }
            
            public void CheckNoRenters()
            {
                if (!SupportsRenting)
                    return;
                CleanRenterList();
                if (!bufferRenters.IsEmpty)
                    UnityEngine.Debug.LogError($"VUnsafeBufferList: {bufferRenters.Count} when checking for no renters!");
            }

            void CleanRenterList()
            {
                if (!SupportsRenting)
                    return;
                
                // Collect disposed renters
                using var disposedPointers = new UnsafeList<int>(bufferRenters.Count, Allocator.Temp);
                foreach (var renter in bufferRenters)
                    if (!renter.Value.IsCreated)
                        disposedPointers.Add(renter.Key);
                
                // Clean up
                foreach (var disposedIndex in disposedPointers)
                    bufferRenters.Remove(disposedIndex);
            }

            public void DisposeAllRenters()
            {
                if (!SupportsRenting)
                    return;
                foreach (var renter in bufferRenters)
                    renter.Value.DisposeRefToDefault();
                bufferRenters.Clear();
            }

            public void DisposeRentersOf(int index)
            {
                if (!SupportsRenting)
                    return;
                if (!bufferRenters.TryGetValue(index, out var renter))
                    return;
                renter.DisposeRefToDefault();
                bufferRenters.Remove(index);
            }

            public int ClaimNextIndex()
            {
                this.ConditionalCheckIsCreated();
                var index = packedIndices.FetchIndex();
                EnsureMinLength(index + 1);
                SetActive(index, true);
                return index;
            }

            /// <returns>True if index claimed by this method. False if already was claimed.</returns>
            public bool EnsureClaimedAndActive(int index)
            {
                this.ConditionalCheckIsCreated();
                // Ensure list capacity and length
                if (Length <= index)
                    Resize(index + 1);
                bool indexTaken = packedIndices.TryClaimIndex(index);
                SetActive(index, true);
                return indexTaken;
            }

            /// <summary> Disposes renters, disables the index, writes default to it and returns it to the pool. </summary>
            public void ReturnIndex(int index)
            {
                this.ConditionalCheckIsCreated();
                DisposeRentersOf(index);
                SetActive(index, false);
                listData[index] = default;
                packedIndices.ReturnIndex(index);
            }
            
            /// <returns>True if the index active state was changed, false if it was already set to the desired state.</returns>
            bool SetActive(int index, bool active)
            {
                this.ConditionalCheckIsCreated();
                if (indicesActive[index] == active)
                    return false;
                indicesActive[index] = active;
                return true;
            }

            public void WriteToIndex(int index, T value)
            {
                ConditionalCheckIndexActive(index);
                // Write value
                listData[index] = value;
            }

            public void EnsureMinLength(int newLength)
            {
                if (Length < newLength)
                    Resize(newLength);
            }
            
            public void Resize(int newLength)
            {
                EnsureMinCapacity(newLength);
                listData.Resize(newLength, NativeArrayOptions.ClearMemory);
                indicesActive.Resize(newLength, NativeArrayOptions.ClearMemory);
            }
            
            public void EnsureMinCapacity(int minCapacity)
            {
                this.ConditionalCheckIsCreated();
                if (listData.Capacity < minCapacity)
                    SetCapacity(minCapacity);
            }

            public void SetCapacity(int newCapacity)
            {
                this.ConditionalCheckIsCreated();
                // Enforce power of two capacity
                newCapacity = math.ceilpow2(newCapacity);
                
                if (newCapacity == listData.Capacity)
                    return;
                
                // Cannot maintain safeptrs to memory that is being moved.
                CheckNoRenters();
                DisposeAllRenters();
                
                listData.Capacity = newCapacity;
                indicesActive.Capacity = newCapacity;
                if (packedIndices.MaxIndex < newCapacity - 1)
                    packedIndices.MaxIndex = newCapacity - 1;
            }

            public void Clear()
            {
                this.ConditionalCheckIsCreated();
                DisposeAllRenters();
                // Return all indices
                for (int i = 0; i < listData.Length; i++)
                {
                    if (SetActive(i, false))
                        packedIndices.ReturnIndex(i);
                }
                // Clear data
                listData.Clear();
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
            public readonly void ConditionalCheckIndexValid(int index)
            {
                if (!IndexInBufferRange(index))
                    throw new IndexOutOfRangeException($"Index {index} is out of range in VUnsafeList of '{Length}' Length.");
            }
        
            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
            public readonly void ConditionalCheckIndexActive(int index)
            {
                if (!IndexActive(index))
                    throw new IndexOutOfRangeException($"Index {index} is not active in VUnsafeBufferList.");
            }
        }
        
        /// <summary> Initializes and returns a VUnsafeList with a capacity of one. </summary>
        /// <param name="allocator">The allocator to use.</param>
        public VUnsafeBufferList(Allocator allocator) : this(1, false, allocator)
        {
        }

        /// <summary> Initializes and returns a VUnsafeList. </summary>
        /// <param name="initialCapacity">The initial capacity of the list.</param>
        /// <param name="supportsRenting">Whether to support the renting of SafePtrs from the buffer, allowing for extended safety and lifecycle for buffer elements.</param>
        /// <param name="allocator">The allocator to use.</param>
        /// <param name="options">Options for the backing collection</param>
        public VUnsafeBufferList(int initialCapacity, bool supportsRenting, Allocator allocator, bool logStillActiveOnDispose = false)
        {
            this = default;
            Initialize(initialCapacity, allocator, supportsRenting, logStillActiveOnDispose);
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] {typeof(AllocatorManager.AllocatorHandle)})]
        void Initialize(int initialCapacity, Allocator allocator, bool supportsRenting, bool logStillActiveOnDispose)
        {
            data = new VUnsafeRef<Data>(new Data(initialCapacity, allocator, supportsRenting, logStillActiveOnDispose), allocator);
        }

        /// <summary> Releases all resources. </summary>
        public void Dispose()
        {
            if (!IsCreated)
                return;
            DataRef.Dispose();
        }

        /// <summary>
        /// Whether this list has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this list has been allocated (and not yet deallocated).</value>
        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => data.IsCreated && DataReadRef.IsCreated;
        }

        /// <summary>
        /// Whether this list is empty.
        /// </summary>
        /// <value>True if the list is empty or if the list has not been constructed.</value>
        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                this.ConditionalCheckIsCreated();
                return DataRef.listData.Length == 0;
            }
        }

        public int ActiveCount
        {
            get
            {
                this.ConditionalCheckIsCreated();
                return DataRef.packedIndices.Count();
            }
        }

        /// <summary> The range within capacity being used. There may be holes with inactive indices. </summary>
        /// <param name="value>">The new length. If the new length is greater than the current capacity, the capacity is increased.
        /// Newly allocated memory is cleared.</param>
        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                this.ConditionalCheckIsCreated();
                return DataRef.listData.Length;
            }

            set
            {
                this.ConditionalCheckIsCreated();
                DataRef.Resize(value);
            }
        }

        /// <summary>
        /// The number of elements that fit in the current allocation.
        /// </summary>
        /// <value>The number of elements that fit in the current allocation.</value>
        /// <param name="value">The new capacity. Must be greater or equal to the length.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the new capacity is smaller than the length.</exception>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                this.ConditionalCheckIsCreated();
                return DataRef.listData.Capacity;
            }
            set
            {
                this.ConditionalCheckIsCreated();
                DataRef.SetCapacity(value);
            }
        }

        /// <summary> Tells you whether the index is inside the buffer range or not, without care to whether the index is considered 'active'. </summary>
        public readonly bool IndexInBufferRange(int index)
        {
            this.ConditionalCheckIsCreated();
            return DataReadRef.IndexInBufferRange(index);
        }

        /// <summary> Tells you whether the index is inside the buffer range and is considered 'active'. </summary>
        public bool IndexActive(int index)
        {
            this.ConditionalCheckIsCreated();
            return DataRef.IndexActive(index);
        }

        /// <summary> The element at a given index. </summary>
        /// <param name="index">An index into this list.</param>
        /// <value>The value to store at the `index`.</value>
        /// <exception cref="IndexOutOfRangeException">Thrown if `index` is out of bounds.</exception>
        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                this.ConditionalCheckIsCreated();
                DataReadRef.ConditionalCheckIndexActive(index);
                return DataRef.listData[index];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                this.ConditionalCheckIsCreated();
                DataRef.ConditionalCheckIndexActive(index);
                DataRef.listData[index] = value;
            }
        }

        /// <summary> Take a safe pointer to a given index. <br/>
        /// This point will be invalidated if this index is removed or invalidated by resizing or clearing. <br/>
        /// You do not need to dispose the safe pointer yourself, if you want to throw out your reference, you can simply assign 'default'. <br/>
        /// Disposing the pointer directly will invalidate the reference at its source and invalidate the reference everywhere. </summary>
        public SafePtr<T> RentIndexPointer(int index)
        {
            this.ConditionalCheckIsCreated();
            return DataRef.RentIndexPointer(index);
        }

        /// <summary> Returns a reference to the element at an index. </summary>
        /// <param name="index">An index.</param>
        /// <returns>A reference to the element at the index.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if index is out of bounds.</exception>
        public readonly ref T ElementAt(int index)
        {
            this.ConditionalCheckIsCreated();
            DataReadRef.ConditionalCheckIndexActive(index);
            return ref DataRef.listData.ElementAt(index);
        }

        /// <summary> Accesses an index whether it's active or not. Use carefully. </summary>
        public readonly ref T ElementAtUnsafe(int index)
        {
            this.ConditionalCheckIsCreated();
            DataReadRef.ConditionalCheckIndexValid(index);
            return ref DataRef.listData.ElementAt(index);
        }
        
        public readonly bool TryGetValue(int index, out T value)
        {
            if (IndexActive(index))
            {
                value = this[index];
                return true;
            }
            value = default;
            return false;
        }
        
        /// <summary> Can access inactive indices. </summary>
        public readonly bool TryGetValueUnsafe(int index, out T value)
        {
            if (IndexInBufferRange(index))
            {
                value = DataRef.listData[index];
                return true;
            }
            value = default;
            return false;
        }
        
        public readonly ref T TryGetRef(int index, out bool hasRef)
        {
            if (IndexActive(index))
            {
                hasRef = true;
                return ref DataRef.listData.ElementAt(index);
            }
            hasRef = false;
            return ref VUnsafeUtil.NullRef<T>();
        }

        /// <summary> Can access inactive indices. </summary>
        public readonly ref T TryGetRefUnsafe(int index, out bool hasRef)
        {
            if (IndexInBufferRange(index))
            {
                hasRef = true;
                return ref DataRef.listData.ElementAt(index);
            }
            hasRef = false;
            return ref VUnsafeUtil.NullRef<T>();
        }

        public readonly ref readonly T TryGetRefReadonly(int index, out bool hasRef) => ref TryGetRef(index, out hasRef);
        
        /// <summary> Can access inactive indices. </summary>
        public readonly ref readonly T TryGetRefReadonlyUnsafe(int index, out bool hasRef) => ref TryGetRefUnsafe(index, out hasRef);

        public readonly int PeekUnusedIndex()
        {
            this.ConditionalCheckIsCreated();
            return DataRef.packedIndices.PeekNext();
            //return unusedIndices.IsEmpty ? Length : unusedIndices.ListData.Peek();
        }

        /// <summary> Add a value, using a free index inside the list's range, if possible, otherwise expanding the list </summary>
        public int AddCompact(in T value)
        {
            this.ConditionalCheckIsCreated();
            ref var dataRef = ref DataRef;

            int index = dataRef.ClaimNextIndex();
            dataRef.WriteToIndex(index, value);
            return index;
        }

        /// <summary> Will expand list to fit incoming index. </summary>
        public bool TryAddAtIndex(int index, T value, bool allowWriteInsideCurrentListRange = true)
        {
            this.ConditionalCheckIsCreated();
            if (!allowWriteInsideCurrentListRange && IndexInBufferRange(index))
                return false;
            
            ref var dataRef = ref DataRef;
            
            dataRef.EnsureClaimedAndActive(index);
            
            // Write value
            dataRef.listData[index] = value;
            return true;
        }

        // USE REMOVEATCLEAR INSTEAD
        /*/// <summary>Explicitly command the list to recycle this index for future additions</summary>
        public void MarkIndexUnused(int index)
        {
            this.ConditionalCheckIsCreated();
            if (IndexInBufferRange(index))
                unusedIndices.ListData.AddSortedExclusive(index, out _);
        }*/

        /*/// <summary> Appends an element to the end of this list. </summary>
        /// <param name="value">The value to add to the end of this list.</param>
        /// <remarks> Length is incremented by 1. Will not increase the capacity. </remarks>
        /// <exception cref="InvalidOperationException">Thrown if incrementing the length would exceed the capacity.</exception>
        void AddNoResize(T value)
        {
            ConditionalCheckIsCreated();
            listData.AddNoResize(value);
        }*/

        /*/// <summary> Appends elements from a buffer to the end of this list. </summary>
        /// <param name="ptr">The buffer to copy from.</param>
        /// <param name="count">The number of elements to copy from the buffer.</param>
        /// <remarks> Length is increased by the count. Will not increase the capacity. </remarks>
        /// <exception cref="InvalidOperationException">Thrown if the increased length would exceed the capacity.</exception>
        unsafe void AddRangeNoResize(void* ptr, int count)
        {
            ConditionalCheckIsCreated();
            CheckArgPositive(count);
            listData.AddRangeNoResize(ptr, count);
        }*/

        /*/// <summary>
        /// Appends the elements of another list to the end of this list.
        /// </summary>
        /// <param name="list">The other list to copy from.</param>
        /// <remarks>
        /// Length is increased by the length of the other list. Will not increase the capacity.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown if the increased length would exceed the capacity.</exception>
        void AddRangeNoResize(NativeList<T> list)
        {
            ConditionalCheckIsCreated();
            listData.AddRangeNoResize(list);
        }*/

        /*/// <summary>
        /// Appends an element to the end of this list.
        /// </summary>
        /// <param name="value">The value to add to the end of this list.</param>
        /// <remarks>
        /// Length is incremented by 1. If necessary, the capacity is increased.
        /// </remarks>
        void Add(in T value)
        {
            ConditionalCheckIsCreated();
            listData.Add(in value);
        }*/

        /*/// <summary>
        /// Appends the elements of an array to the end of this list.
        /// </summary>
        /// <param name="array">The array to copy from.</param>
        /// <remarks>
        /// Length is increased by the number of new elements. Does not increase the capacity.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the increased length would exceed the capacity.</exception>
        unsafe void AddRange(NativeArray<T> array)
        {
            ConditionalCheckIsCreated();
            AddRange(array.GetUnsafeReadOnlyPtr(), array.Length);
        }*/

        /*/// <summary>
        /// Appends the elements of a buffer to the end of this list.
        /// </summary>
        /// <param name="ptr">The buffer to copy from.</param>
        /// <param name="count">The number of elements to copy from the buffer.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if count is negative.</exception>
        unsafe void AddRange(void* ptr, int count)
        {
            ConditionalCheckIsCreated();
            CheckArgPositive(count);
            listData.AddRange(ptr, count);
        }*/

        /*/// <summary>
        /// Appends value count times to the end of this list.
        /// </summary>
        /// <param name="value">The value to add to the end of this list.</param>
        /// <param name="count">The number of times to replicate the value.</param>
        /// <remarks>
        /// Length is incremented by count. If necessary, the capacity is increased.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if count is negative.</exception>
        void AddReplicate(in T value, int count)
        {
            ConditionalCheckIsCreated();
            CheckArgPositive(count);
            listData.AddReplicate(in value, count);
        }*/

        /*/// <summary>
        /// Shifts elements toward the end of this list, increasing its length.
        /// </summary>
        /// <remarks>
        /// Right-shifts elements in the list so as to create 'free' slots at the beginning or in the middle.
        ///
        /// The length is increased by `end - begin`. If necessary, the capacity will be increased accordingly.
        ///
        /// If `end` equals `begin`, the method does nothing.
        ///
        /// The element at index `begin` will be copied to index `end`, the element at index `begin + 1` will be copied to `end + 1`, and so forth.
        ///
        /// The indexes `begin` up to `end` are not cleared: they will contain whatever values they held prior.
        /// </remarks>
        /// <param name="begin">The index of the first element that will be shifted up.</param>
        /// <param name="end">The index where the first shifted element will end up.</param>
        /// <exception cref="ArgumentException">Thrown if `end &lt; begin`.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if `begin` or `end` are out of bounds.</exception>
        void InsertRangeWithBeginEnd(int begin, int end)
        {
            ConditionalCheckIsCreated();
            listData.InsertRangeWithBeginEnd(begin, end);
        }*/

        /*/// <summary>
        /// Shifts elements toward the end of this list, increasing its length.
        /// </summary>
        /// <remarks>
        /// Right-shifts elements in the list so as to create 'free' slots at the beginning or in the middle.
        ///
        /// The length is increased by `count`. If necessary, the capacity will be increased accordingly.
        ///
        /// If `count` equals `0`, the method does nothing.
        ///
        /// The element at index `index` will be copied to index `index + count`, the element at index `index + 1` will be copied to `index + count + 1`, and so forth.
        ///
        /// The indexes `index` up to `index + count` are not cleared: they will contain whatever values they held prior.
        /// </remarks>
        /// <param name="index">The index of the first element that will be shifted up.</param>
        /// <param name="count">The number of elements to insert.</param>
        /// <exception cref="ArgumentException">Thrown if `count` is negative.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if `index` is out of bounds.</exception>
        void InsertRange(int index, int count) => InsertRangeWithBeginEnd(index, index + count);*/

        /*/// <summary>
        /// Creates and schedules a job that releases all resources (memory and safety handles) of this list.
        /// </summary>
        /// <param name="inputDeps">The dependency for the new job.</param>
        /// <returns>The handle of the new job. The job depends upon `inputDeps` and releases all resources (memory and safety handles) of this list.</returns>
        public JobHandle Dispose(JobHandle inputDeps)
        {
            if (!IsCreated)
                return inputDeps;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
           var jobHandle = new VUnsafeListDisposeJob {Data = new VUnsafeListDispose {m_ListData = (UntypedUnsafeList*) listData, m_Safety = m_Safety}}.Schedule(inputDeps);
            AtomicSafetyHandle.Release(m_Safety);
#else
            var jobHandle = new VUnsafeListDisposeJob { Data = new VUnsafeListDispose { m_ListData = (UntypedUnsafeList*)m_ListData } }.Schedule(inputDeps);
#endif
            listData = null;

            return jobHandle;
        }*/

        /// <summary> Sets the length to 0. </summary>
        /// <remarks> Does not change the capacity. </remarks>
        public void Clear()
        {
            this.ConditionalCheckIsCreated();
            DataRef.Clear();
        }

        /*public void ClearListOnly()
        {
            ConditionalCheckIsCreated();
            listData.Clear();
        }

        public void ClearUnusedIndicesOnly()
        {
            ConditionalCheckIsCreated();
            unusedIndices.Clear();
        }*/
        
        /// <summary> Sets the value to default at index and adds the index to the recycle indices list.
        /// This is the only real way to "remove" from this type of list.</summary>
        public void RemoveAtClear(int index)
        {
            if (!IndexInBufferRange(index))
                return;
            DataRef.ReturnIndex(index);
        }
        
        //public readonly NativeArray<T> AsArray() => DataRef.listData.AsArray();

        /*/// <summary>
        /// Returns an array that aliases this list. The length of the array is updated when the length of
        /// this array is updated in a prior job.
        /// </summary>
        /// <remarks>
        /// Useful when a job populates a list that is then used by another job.
        ///
        /// If you pass both jobs the same list, you have to complete the first job before you schedule the second:
        /// otherwise, the second job doesn't see the first job's changes to the list's length.
        ///
        /// If instead you pass the second job a deferred array that aliases the list, the array's length is kept in sync with
        /// the first job's changes to the list's length. Consequently, the first job doesn't have to
        /// be completed before you can schedule the second: the second job simply has to depend upon the first.
        /// </remarks>
        /// <returns>An array that aliases this list and whose length can be specially modified across jobs.</returns>
        /// <example>
        /// The following example populates a list with integers in one job and passes that data to a second job as
        /// a deferred array. If we tried to pass the list directly to the second job, that job would not see any
        /// modifications made to the list by the first job. To avoid this, we instead pass the second job a deferred array that aliases the list.
        /// <code>
        /// using UnityEngine;
        /// using Unity.Jobs;
        /// using Unity.Collections;
        ///
        /// public class DeferredArraySum : MonoBehaviour
        ///{
        ///    public struct Populate : IJob
        ///    {
        ///        public VUnsafeList&lt;int&gt; list;
        ///
        ///        public void Execute()
        ///        {
        ///            for (int i = list.Length; i &lt; list.Capacity; i++)
        ///            {
        ///                list.Add(i);
        ///            }
        ///        }
        ///    }
        ///
        ///    // Sums all numbers from deferred.
        ///    public struct Sum : IJob
        ///    {
        ///        [ReadOnly] public NativeArray&lt;int&gt; deferred;
        ///        public NativeArray&lt;int&gt; sum;
        ///
        ///        public void Execute()
        ///        {
        ///            sum[0] = 0;
        ///            for (int i = 0; i &lt; deferred.Length; i++)
        ///            {
        ///                sum[0] += deferred[i];
        ///            }
        ///        }
        ///    }
        ///
        ///    void Start()
        ///    {
        ///        var list = new VUnsafeList&lt;int&gt;(100, Allocator.TempJob);
        ///        var deferred = list.AsDeferredJobArray(),
        ///        var output = new NativeArray&lt;int&gt;(1, Allocator.TempJob);
        ///
        ///        // The Populate job increases the list's length from 0 to 100.
        ///        var populate = new Populate { list = list }.Schedule();
        ///
        ///        // At time of scheduling, the length of the deferred array given to Sum is 0.
        ///        // When Populate increases the list's length, the deferred array's length field in the
        ///        // Sum job is also modified, even though it has already been scheduled.
        ///        var sum = new Sum { deferred = deferred, sum = output }.Schedule(populate);
        ///
        ///        sum.Complete();
        ///
        ///        Debug.Log("Result: " + output[0]);
        ///
        ///        list.Dispose();
        ///        output.Dispose();
        ///    }
        /// }
        /// </code>
        /// </example>
        public NativeArray<T> AsDeferredJobArray()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
           AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
#endif
            byte* buffer = (byte*) listData;
            // We use the first bit of the pointer to infer that the array is in list mode
            // Thus the job scheduling code will need to patch it.
            buffer += 1;
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(buffer, 0, Allocator.Invalid);

#if ENABLE_UNITY_COLLECTIONS_CHECKS 
           NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, m_Safety);
#endif

            return array;
        }*/

        /// <summary>
        /// Returns an array containing a copy of this list's content.
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array containing a copy of this list's content.</returns>
        public readonly unsafe NativeArray<T> ToArray(AllocatorManager.AllocatorHandle allocator)
        {
            this.ConditionalCheckIsCreated();
            NativeArray<T> result = CollectionHelper.CreateNativeArray<T>(Length, allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeUtility.MemCpy((byte*) result.ForceGetUnsafePtrNOSAFETY(), (byte*) DataRef.listData.Ptr, Length * UnsafeUtility.SizeOf<T>());
            return result;
        }

        /// <summary>
        /// Copies all elements of specified container to this container.
        /// </summary>
        /// <param name="other">An container to copy into this container.</param>
        public void CopyFrom(in NativeArray<T> other)
        {
            this.ConditionalCheckIsCreated();
            DataRef.listData.CopyFrom(other);
        }

        /// <summary>
        /// Copies all elements of specified container to this container.
        /// </summary>
        /// <param name="other">An container to copy into this container.</param>
        public void CopyFrom(in UnsafeList<T> other)
        {
            this.ConditionalCheckIsCreated();
            DataRef.listData.CopyFrom(other);
        }

        /// <summary>
        /// Copies all elements of specified container to this container.
        /// </summary>
        /// <param name="other">An container to copy into this container.</param>
        public void CopyFrom(in VUnsafeList<T> other) => CopyFrom(other.ListData);

        /// <summary>
        /// Copies all elements of specified container to this container.
        /// </summary>
        /// <param name="other">An container to copy into this container.</param>
        public unsafe void CopyFrom(ref NativeList<T> other) => CopyFrom(*other.GetUnsafeList());

        /// <summary>
        /// Sets the length of this list, increasing the capacity if necessary.
        /// </summary>
        /// <param name="length">The new length of this list.</param>
        /// <param name="options">Whether to clear any newly allocated bytes to all zeroes.</param>
        public void Resize(int length)
        {
            this.ConditionalCheckIsCreated();
            DataRef.Resize(length);
        }

        /*/// <summary>
        /// Sets the length of this list, increasing the capacity if necessary.
        /// </summary>
        /// <remarks>Does not clear newly allocated bytes.</remarks>
        /// <param name="length">The new length of this list.</param>
        public void ResizeUninitialized(int length)
        {
            this.ConditionalCheckIsCreated();
            Resize(length, NativeArrayOptions.UninitializedMemory);
        }*/

        /// <summary>
        /// Sets the capacity.
        /// </summary>
        /// <param name="capacity">The new capacity.</param>
        public void SetCapacity(int capacity)
        {
            this.ConditionalCheckIsCreated();
            DataRef.SetCapacity(capacity);
        }

        /*/// <summary>
        /// Sets the capacity to match the length.
        /// </summary>
        public void TrimExcess()
        {
            ConditionalCheckIsCreated();
            listData.TrimExcess();
        }*/
        
        #region Enumeration
        
        /// <summary>
        /// Returns an enumerator over the elements of this list.
        /// </summary>
        /// <returns>An enumerator over the elements of this list.</returns>
        public VUnsafeBufferList<T>.Enumerator GetEnumerator() => new(this);

        /// <summary>
        /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
        /// </summary>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
        /// </summary>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        
        public struct Enumerator : IEnumerator<T>
        {
            VUnsafeBufferList<T> list;
            int index;

            public Enumerator(VUnsafeBufferList<T> list)
            {
                BurstAssert.TrueCheap(list.IsCreated);
                this.list = list;
                index = -1;
            }

            public T Current => list[index];

            object IEnumerator.Current => Current;

            public void Dispose() { }

            public bool MoveNext()
            {
                // Find the next active index
                for (int i = index + 1; i < list.Length; ++i)
                {
                    if (list.IndexActive(i))
                    {
                        index = i;
                        return true;
                    }
                }
                return false;
            }

            public void Reset() => index = -1;
        }
        
        #endregion

        #region ParallelWriter

        /*/// <summary>
        /// Returns a parallel writer of this list.
        /// </summary>
        /// <returns>A parallel writer of this list.</returns>
        public ParallelWriter AsParallelWriter()
        {
            return new ParallelWriter(listData);
        }

        /// <summary>
        /// A parallel writer for a VUnsafeList.
        /// </summary>
        /// <remarks>
        /// Use <see cref="AsParallelWriter"/> to create a parallel writer for a list.
        /// </remarks>
        [NativeContainer]
        [NativeContainerIsAtomicWriteOnly]
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] {typeof(int)})]
        public struct ParallelWriter
        {
            /#1#// <summary>
            /// The data of the list.
            /// </summary>
            public readonly void* Ptr
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ListData.Ptr;
            }#1#

            /// <summary>
            /// The internal unsafe list.
            /// </summary>
            /// <value>The internal unsafe list.</value>
            [NativeDisableUnsafePtrRestriction] public VUnsafeList<T> ListData;

            public readonly bool IsCreated => ListData.IsCreated && ListData.IsCreated;
            
            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
            public readonly void ConditionalCheckIsCreated()
            {
                if (!IsCreated)
                    throw new InvalidOperationException("ParallelWriter can not be written to because it was not initialized");
            }
            
            internal ParallelWriter(VUnsafeList<T> listData) => ListData = listData;

            /// <summary>
            /// Appends an element to the end of this list.
            /// </summary>
            /// <param name="value">The value to add to the end of this list.</param>
            /// <remarks>
            /// Increments the length by 1 unless doing so would exceed the current capacity.
            /// </remarks>
            /// <exception cref="InvalidOperationException">Thrown if adding an element would exceed the capacity.</exception>
            public void AddNoResize(T value)
            {
                ConditionalCheckIsCreated();
                var idx = Interlocked.Increment(ref ListData.Length) - 1;
                CheckSufficientCapacity(ListData.Capacity, idx + 1);

                UnsafeUtility.WriteArrayElement(ListData.Ptr, idx, value);
            }

            /// <summary>
            /// Appends elements from a buffer to the end of this list.
            /// </summary>
            /// <param name="ptr">The buffer to copy from.</param>
            /// <param name="count">The number of elements to copy from the buffer.</param>
            /// <remarks>
            /// Increments the length by `count` unless doing so would exceed the current capacity.
            /// </remarks>
            /// <exception cref="InvalidOperationException">Thrown if adding the elements would exceed the capacity.</exception>
            public void AddRangeNoResize(void* ptr, int count)
            {
                CheckArgPositive(count);
                ConditionalCheckIsCreated();
                
                var idx = Interlocked.Add(ref ListData.m_length, count) - count;
                CheckSufficientCapacity(ListData.Capacity, idx + count);

                var sizeOf = sizeof(T);
                void* dst = (byte*) ListData.Ptr + idx * sizeOf;
                UnsafeUtility.MemCpy(dst, ptr, count * sizeOf);
            }

            /// <summary>
            /// Appends the elements of another list to the end of this list.
            /// </summary>
            /// <param name="list">The other list to copy from.</param>
            /// <remarks>
            /// Increments the length of this list by the length of the other list unless doing so would exceed the current capacity.
            /// </remarks>
            /// <exception cref="InvalidOperationException">Thrown if adding the elements would exceed the capacity.</exception>
            public void AddRangeNoResize(UnsafeList<T> list)
            {
                AddRangeNoResize(list.Ptr, list.Length);
            }

            /#1#// <summary>
            /// Appends the elements of another list to the end of this list.
            /// </summary>
            /// <param name="list">The other list to copy from.</param>
            /// <remarks>
            /// Increments the length of this list by the length of the other list unless doing so would exceed the current capacity.
            /// </remarks>
            /// <exception cref="InvalidOperationException">Thrown if adding the elements would exceed the capacity.</exception>
            public void AddRangeNoResize(NativeList<T> list)
            {
                AddRangeNoResize(*list.);
            }#1#
        }*/

        #endregion

        #region Checks

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public readonly void ConditionalCheckIndexValid(int index) => DataReadRef.ConditionalCheckIndexValid(index);

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public readonly void ConditionalCheckIndexActive(int index) => DataReadRef.ConditionalCheckIndexActive(index);

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        static void CheckInitialCapacity(int initialCapacity)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Capacity must be >= 0");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        static void CheckTotalSize(int initialCapacity, long totalSize)
        {
            // Make sure we cannot allocate more than int.MaxValue (2,147,483,647 bytes)
            // because the underlying UnsafeUtility.Malloc is expecting a int.
            // TODO: change UnsafeUtility.Malloc to accept a UIntPtr length instead to match C++ API
            if (totalSize > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), $"Capacity * sizeof(T) cannot exceed {int.MaxValue} bytes");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        static void CheckSufficientCapacity(int capacity, int length)
        {
            if (capacity < length)
                throw new InvalidOperationException($"Length {length} exceeds Capacity {capacity}");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        static void CheckIndexInRange(int value, int length)
        {
            if (value < 0)
                throw new IndexOutOfRangeException($"Value {value} must be positive.");

            if ((uint) value >= (uint) length)
                throw new IndexOutOfRangeException(
                    $"Value {value} is out of range in VUnsafeList of '{length}' Length.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        static void CheckArgPositive(int value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException($"Value {value} must be positive.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        void CheckHandleMatches(AllocatorManager.AllocatorHandle handle)
        {
            if (!DataRef.listData.IsCreated)
                throw new ArgumentOutOfRangeException($"Allocator handle {handle} can't match because container is not initialized.");
            if (DataRef.listData.Allocator.Index != handle.Index)
                throw new ArgumentOutOfRangeException($"Allocator handle {handle} can't match because container handle index doesn't match.");
            if (DataRef.listData.Allocator.Version != handle.Version)
                throw new ArgumentOutOfRangeException($"Allocator handle {handle} matches container handle index, but has different version.");
        }

        #endregion

        #region Readonly
        
        public static implicit operator ReadOnly(VUnsafeBufferList<T> list) => list.AsReadOnly();

        /// <summary> Returns a read only of this list. </summary>
        /// <returns>A read only of this list.</returns>
        public readonly ReadOnly AsReadOnly()
        {
            this.ConditionalCheckIsCreated();
            // Lol, just wrap the list, idk why they would input the ptr and length manually, that stuff could be modified elsewhere leading to a crash, easily.
            return new ReadOnly(this);
        }

        /// <summary> A readonly version of VUnsafeList, use AsReadOnly() to get one. </summary>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        public readonly struct ReadOnly
            /*: IEnumerable<T>*/
        {
            public readonly VUnsafeBufferList<T> list;
            
            /*/// <summary> The internal buffer of the list. </summary>
            public readonly T* Ptr => list.listData.Ptr;*/

            /// <summary> The number of elements. </summary>
            public readonly int Length => list.Length;
            
            public readonly int Capacity => list.Capacity;
            
            public T this[int index]
            {
                get
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG                   
                    return ReadSafe(index);
#else
                    return list[index];
#endif
                }
            }

            public ReadOnly(VUnsafeBufferList<T> list) => this.list = list;

            /// <summary> Performs a guarded read and logs and error if something was wrong. </summary>
            public readonly T ReadSafe(int index)
            {
                
                if (list.TryGetValue(index, out var value))
                    return value;
                UnityEngine.Debug.LogError($"Index {index} is out of range in VUnsafeList.ReadOnly of '{Length}' Length.");
                return default;
            }
            
            public readonly bool TryGetValue(int index, out T value) => list.TryGetValue(index, out value);

            /*/// <summary>
            /// Returns an enumerator over the elements of the list.
            /// </summary>
            /// <returns>An enumerator over the elements of the list.</returns>
            public Enumerator GetEnumerator() => new() { m_Ptr = Ptr, m_Length = Length, m_Index = -1 };

            /// <summary>
            /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
            /// </summary>
            /// <returns>Throws NotImplementedException.</returns>
            /// <exception cref="NotImplementedException">Method is not implemented.</exception>
            IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();

            /// <summary>
            /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
            /// </summary>
            /// <returns>Throws NotImplementedException.</returns>
            /// <exception cref="NotImplementedException">Method is not implemented.</exception>
            IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw new NotImplementedException();*/
        }

        #endregion
    }

    /*[NativeContainer]
    [GenerateTestsForBurstCompatibility]
    internal unsafe struct VUnsafeListDispose
    {
        [NativeDisableUnsafePtrRestriction] public UntypedUnsafeList* m_ListData;

        public void Dispose()
        {
            var listData = (UnsafeList<int>*) m_ListData;
            UnsafeList<int>.Destroy(listData);
        }
    }

    [BurstCompile]
    [GenerateTestsForBurstCompatibility]
    internal unsafe struct VUnsafeListDisposeJob : IJob
    {
        internal VUnsafeListDispose Data;

        public void Execute()
        {
            Data.Dispose();
        }
    }*/

    /*/// <summary>
    /// Provides extension methods for UnsafeList.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public static unsafe class VUnsafeBufferListExtensions
    {
        /// <summary>
        /// Returns true if a particular value is present in this list.
        /// </summary>
        /// <typeparam name="T">The type of elements in this list.</typeparam>
        /// <typeparam name="U">The value type.</typeparam>
        /// <param name="list">The list to search.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>True if the value is present in this list.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] {typeof(int), typeof(int)})]
        public static bool Contains<T, U>(this VUnsafeBufferList<T> list, U value)
            where T : unmanaged, IEquatable<U>
        {
            list.ConditionalCheckIsCreated();
            return NativeArrayExtensions.IndexOf<T, U>(list.listData.GetUnsafePtr(), list.Length, value) != -1;
        }

        /// <summary>
        /// Finds the index of the first occurrence of a particular value in this list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <typeparam name="U">The value type.</typeparam>
        /// <param name="list">The list to search.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>The index of the first occurrence of the value in this list. Returns -1 if no occurrence is found.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] {typeof(int), typeof(int)})]
        public static int IndexOf<T, U>(this VUnsafeBufferList<T> list, U value)
            where T : unmanaged, IEquatable<U>
        {
            list.ConditionalCheckIsCreated();
            return NativeArrayExtensions.IndexOf<T, U>(list.listData.GetUnsafePtr(), list.Length, value);
        }
    }*/
}