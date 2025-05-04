using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using VLib.Threading;

namespace VLib
{
    /// <summary> Designed to serve as an ECS-style native buffer where indices are recycled instead of removed. This allows for safely persisting references to individual elements. <br/>
    /// Wraps a <see cref="VUnsafeBufferList{T}"/> and provides a way to rent SafePtrs to elements. <br/>
    /// Not thread-safe unless otherwise specified. <br/>
    /// Copy-safe.</summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] {typeof(int)})]
    public struct VUnsafeBufferArray<T> : IAllocating, INativeList<T>, IEnumerable<T> // Used by collection initializers.
        where T : unmanaged
    {
        RefStruct<Data> data;

        /// <summary> Inherently checks IsCreated </summary>
        readonly ref Data DataRef
        {
            get
            {
                this.ConditionalCheckIsCreated();
                return ref data.ValueRef;
            }
        }
        readonly ref Data DataRefUnsafe => ref data.ValueRef;

        /// <summary> Inherently checks IsCreated </summary>
        readonly ref readonly Data DataReadRef => ref DataRef;
        readonly ref readonly Data DataReadRefUnsafe => ref data.ValueRef;

        public struct Data : IAllocating
        {
            internal VUnsafeBufferList<T> list;
            UnsafeHashMap<int, SafePtr<T>> bufferRenters;

            public readonly bool IsCreated => list.IsCreated;

            public readonly bool SupportsRenting
            {
                get
                {
                    this.ConditionalCheckIsCreated();
                    return bufferRenters.IsCreated;
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
            public void ConditionalCheckSupportsRenting()
            {
                if (!SupportsRenting)
                    throw new InvalidOperationException("This buffer does not support renting SafePtrs.");
            }

            public readonly int Length
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    this.ConditionalCheckIsCreated();
                    return list.Length;
                }
            }

            public readonly int Capacity
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    this.ConditionalCheckIsCreated();
                    return list.Capacity;
                }
            }

            public Data(int capacity, Allocator allocator, bool supportsRenting, bool logStillActiveOnDispose)
            {
                list = new VUnsafeBufferList<T>(capacity, allocator, logStillActiveOnDispose);
                bufferRenters = supportsRenting ? new(capacity, allocator) : default;
            }

            public void Dispose()
            {
                if (!IsCreated)
                    return;

                FlushRenters();
                bufferRenters.Dispose();
                list.Dispose();
            }

            /// <summary> Tells you whether the index is inside the buffer range or not, without care to whether the index is considered 'active'. </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool IndexInBufferRange(int index)
            {
                this.ConditionalCheckIsCreated();
                return list.IndexInBufferRange(index);
            }

            /// <summary> Tells you whether the index is inside the buffer range and is considered 'active'. </summary>
            public bool IndexActive(int index)
            {
                this.ConditionalCheckIsCreated();
                return list.IndexActive(index);
            }

            public SafePtr<T> RentIndexPointer(int index)
            {
                ConditionalCheckSupportsRenting();
                if (!list.IndexActive(index))
                    throw new InvalidOperationException($"Index {index} is not active in VUnsafeBufferArray.");
                if (bufferRenters.TryGetValue(index, out var renter))
                {
                    if (renter.IsCreated)
                        return renter;
                    // If renter pointer is disposed, remove it and continue
                    bufferRenters.Remove(index);
                }

                // Generate new renter
                SafePtr<T> safePtr = default;
                unsafe
                {
                    safePtr = new SafePtr<T>(list.ListDataUnsafe.GetListElementPtr(index));
                }
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
                    UnityEngine.Debug.LogError($"VUnsafeBufferArray: {bufferRenters.Count} when checking for no renters!");
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

            void DisposeRentersOf(int index)
            {
                if (!SupportsRenting)
                    return;
                if (!bufferRenters.TryGetValue(index, out var renter))
                    return;
                renter.DisposeRefToDefault();
                bufferRenters.Remove(index);
            }

            /*public int ClaimNextIndex()
            {
                this.ConditionalCheckIsCreated();
                var index = packedIndices.FetchIndex();
                EnsureMinLength(index + 1);
                SetActive(index, true);
                return index;
            }*/

            /*/// <returns>True if index claimed by this method. False if already was claimed.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool EnsureClaimedAndActive(int index)
            {
                this.ConditionalCheckIsCreated();
                return list.EnsureClaimedAndActive(index);
            }*/

            /// <summary> Disposes renters, disables the index, writes default to it and returns it to the pool. </summary>
            public void RemoveAtClear(int index, in T defaultValue = default)
            {
                this.ConditionalCheckIsCreated();
                DisposeRentersOf(index);
                list.RemoveAtClear(index, defaultValue);
            }

            /*/// <returns>True if the index active state was changed, false if it was already set to the desired state.</returns>
            bool SetActive(int index, bool active)
            {
                this.ConditionalCheckIsCreated();
                if (indicesActive[index] == active)
                    return false;
                indicesActive[index] = active;
                return true;
            }*/

            /*public void EnsureMinLength(int newLength)
            {
                if (Length < newLength)
                    Resize(newLength);
            }*/

            public void Resize(int newLength)
            {
                // NOTE: This system effectively acts like a list that doesn't change capacity.
                if (newLength > Capacity)
                    throw new ArgumentOutOfRangeException($"Cannot resize to {newLength} as it exceeds the capacity of {Capacity}.");
                // Check to ensure resizing up!
                if (newLength < Length)
                    throw new ArgumentOutOfRangeException($"Cannot resize to {newLength} as it is smaller than the current length of {Length}.");

                list.Resize(newLength);
            }

            public void Clear()
            {
                this.ConditionalCheckIsCreated();
                DisposeAllRenters();
                list.Clear();
            }

            /*public void CopyToAndDispose(ref Data otherRef)
            {
                BurstAssert.FalseCheap(SupportsRenting); // Ptr renting is not supported with copying

                listData.CopyTo(0, otherRef.listData, 0, math.min(listData.Length, otherRef.listData.Length));
                //
                if (otherRef.packedIndices.IsCreated)
                    otherRef.packedIndices.Dispose();
                otherRef.packedIndices = packedIndices;
                //
                indicesActive.CopyTo(0, otherRef.indicesActive, 0, math.min(indicesActive.Length, otherRef.indicesActive.Length));

                // Dispose own collections
                this.Dispose_ExceptingPackedIndices();
            }*/

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
            public readonly void ConditionalCheckIndexValid(int index) => list.ConditionalCheckIndexValid(index);

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
            public readonly void ConditionalCheckIndexActive(int index) => list.ConditionalCheckIndexActive(index);
        }

        /// <summary> Initializes and returns a VUnsafeList with a capacity of one. </summary>
        /// <param name="allocator">The allocator to use.</param>
        public VUnsafeBufferArray(Allocator allocator) : this(1, false, allocator)
        {
        }

        /// <summary> Initializes and returns a VUnsafeList. </summary>
        /// <param name="initialCapacity">The initial capacity of the list.</param>
        /// <param name="supportsRenting">Whether to support the renting of SafePtrs from the buffer, allowing for extended safety and lifecycle for buffer elements.</param>
        /// <param name="allocator">The allocator to use.</param>
        /// <param name="options">Options for the backing collection</param>
        public VUnsafeBufferArray(int initialCapacity, bool supportsRenting, Allocator allocator, bool logStillActiveOnDispose = false)
        {
            this = default;
            Initialize(initialCapacity, allocator, supportsRenting, logStillActiveOnDispose);
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] {typeof(AllocatorManager.AllocatorHandle)})]
        void Initialize(int initialCapacity, Allocator allocator, bool supportsRenting, bool logStillActiveOnDispose)
        {
            data = RefStruct<Data>.Create(new Data(initialCapacity, allocator, supportsRenting, logStillActiveOnDispose), allocator);
        }

        /// <summary> Releases all resources. </summary>
        public void Dispose()
        {
            if (!IsCreated)
                return;
            data.DisposeFullToDefault();
        }

        /// <summary> Whether this list has been allocated (and not yet deallocated). </summary>
        /// <value>True if this list has been allocated (and not yet deallocated).</value>
        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => data.IsCreated && DataReadRefUnsafe.IsCreated;
        }

        /// <summary> Whether this list is empty. </summary>
        /// <value>True if the list is empty or if the list has not been constructed.</value>
        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => DataRef.list.IsEmpty;
        }

        public int ActiveCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => DataRef.list.Count;
        }

        /// <summary> The range within capacity being used. There may be holes with inactive indices. </summary>
        /// <param name="value>">The new length. If the new length is greater than the current capacity, the capacity is increased.
        /// Newly allocated memory is cleared.</param>
        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => DataRef.list.Length;
            set => DataRef.Resize(value);
        }

        /// <summary> The number of elements that fit in the current allocation. </summary>
        /// <value>The number of elements that fit in the current allocation.</value>
        /// <param name="value">The new capacity. Must be greater or equal to the length.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the new capacity is smaller than the length.</exception>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => DataRef.list.Capacity;
            set => UnityEngine.Debug.LogError($"Capacity is not settable in VUnsafeBufferArray. Current capacity is {Capacity}, attempted to set to {value}.");
        }

        public bool IsFull => DataRef.list.IsFull;

        /// <summary> Tells you whether the index is inside the buffer range or not, without care to whether the index is considered 'active'. </summary>
        public readonly bool IndexInBufferRange(int index) => DataReadRef.list.IndexInBufferRange(index);

        /// <summary> Tells you whether the index is inside the buffer range and is considered 'active'. </summary>
        public readonly bool IndexActive(int index) => DataRef.list.IndexActive(index);

        /// <summary> The element at a given index. </summary>
        /// <param name="index">An index into this list.</param>
        /// <value>The value to store at the `index`.</value>
        /// <exception cref="IndexOutOfRangeException">Thrown if `index` is out of bounds.</exception>
        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => DataRef.list[index];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => DataRef.list[index] = value;
        }

        /// <summary> Take a safe pointer to a given index. <br/>
        /// This point will be invalidated if this index is removed or invalidated by resizing or clearing. <br/>
        /// You do not need to dispose the safe pointer yourself, if you want to throw out your reference, you can simply assign 'default'. <br/>
        /// Disposing the pointer directly will invalidate the reference at its source and invalidate the reference everywhere. <br/>
        /// This is NOT concurrent-safe. </summary>
        public SafePtr<T> RentIndexPointer(int index) => DataRef.RentIndexPointer(index);

        /// <summary> A variant of <see cref="RentIndexPointer"/> that can only be called on the main thread. This is one way to ensure thread-safety, where applicable. </summary>
        public SafePtr<T> RentIndexPointerMainThread(int index)
        {
            MainThread.AssertMainThreadConditional();
            return RentIndexPointer(index);
        }

        /// <summary> Returns a reference to the element at an index. </summary>
        /// <param name="index">An index.</param>
        /// <returns>A reference to the element at the index.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if index is out of bounds.</exception>
        public readonly ref T ElementAt(int index) => ref DataRef.list.ElementAt(index);

        /// <summary> Accesses an index whether it's active or not. Use carefully. </summary>
        public readonly ref T ElementAtUnsafe(int index) => ref DataRef.list.ElementAtUnsafe(index);

        public readonly bool TryGetValue(int index, out T value) => DataReadRef.list.TryGetValue(index, out value);

        /// <summary> Can access inactive indices. </summary>
        public readonly bool TryGetValueUnsafe(int index, out T value) => DataReadRef.list.TryGetValueUnsafe(index, out value);

        public readonly ref T TryGetRef(int index, out bool hasRef) => ref DataRef.list.TryGetRef(index, out hasRef);

        /// <summary> Can access inactive indices. </summary>
        public readonly ref T TryGetRefUnsafe(int index, out bool hasRef) => ref DataRef.list.TryGetRefUnsafe(index, out hasRef);

        public readonly ref readonly T TryGetRefReadonly(int index, out bool hasRef) => ref TryGetRef(index, out hasRef);

        /// <summary> Can access inactive indices. </summary>
        public readonly ref readonly T TryGetRefReadonlyUnsafe(int index, out bool hasRef) => ref TryGetRefUnsafe(index, out hasRef);

        public readonly int PeekUnusedIndex() => DataRef.list.PeekUnusedIndex();

        /// <summary> Add a value, using a free index inside the list's range, if possible. </summary>
        /// <returns>The index of the added value.</returns>
        public int AddCompact(in T value)
        {
            if (!TryAddCompact(value, out var index))
                throw new InvalidOperationException("Could not add value to VUnsafeBufferArray. Capacity is full.");
            return index;
        }

        /// <summary> Like <see cref="AddCompact"/>, but can safely return false if no room is available. </summary>
        public bool TryAddCompact(in T value, out int index) => DataRef.list.TryAddCompactNoResize(value, out index);

        /// <summary> Will expand list to fit incoming index. </summary>
        public bool TryAddAtIndex(int index, T value, bool allowWriteOverActive = true) => DataRef.list.TryAddAtIndex(index, value, allowWriteOverActive);

        /// <summary> Sets the length to 0. </summary>
        /// <remarks> Does not change the capacity. </remarks>
        public void Clear() => DataRef.Clear();

        /// <summary> Sets the value to default at index and adds the index to the recycle indices list.
        /// This is the only real way to "remove" from this type of list.</summary>
        public void RemoveAtClear(int index, in T defaultValue = default) => DataRef.RemoveAtClear(index, defaultValue);

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

        /*/// <summary>
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
        }*/

        /*/// <summary>
        /// Copies all elements of specified container to this container.
        /// </summary>
        /// <param name="other">An container to copy into this container.</param>
        public void CopyFrom(in NativeArray<T> other)
        {
            this.ConditionalCheckIsCreated();
            DataRef.listData.CopyFrom(other);
        }*/

        /*/// <summary>
        /// Copies all elements of specified container to this container.
        /// </summary>
        /// <param name="other">An container to copy into this container.</param>
        public void CopyFrom(in UnsafeList<T> other)
        {
            this.ConditionalCheckIsCreated();
            DataRef.listData.CopyFrom(other);
        }*/

        /*/// <summary>
        /// Copies all elements of specified container to this container.
        /// </summary>
        /// <param name="other">An container to copy into this container.</param>
        public void CopyFrom(in VUnsafeList<T> other) => CopyFrom(other.ListData);*/

        /*/// <summary>
        /// Copies all elements of specified container to this container.
        /// </summary>
        /// <param name="other">An container to copy into this container.</param>
        public unsafe void CopyFrom(ref NativeList<T> other) => CopyFrom(*other.GetUnsafeList());*/

        /// <summary> Sets the length of this list, cannot increase capacity! </summary>
        /// <param name="length">The new used length of the array.</param>
        public void Resize(int length)
        {
            this.ConditionalCheckIsCreated();
            DataRef.Resize(length);
        }

        /*/// <summary> Generate a new <see cref="VUnsafeBufferArray{T}"/> with a new capacity and transfer as much data from this as possible.
        /// If a smaller capacity is input, you will lose some data. <br/>
        /// This will dispose this collection and return an entirely new one. <br/>
        /// This is not compatible with <see cref="VUnsafeBufferArray{T}.Data.SupportsRenting"/>! </summary>
        public VUnsafeBufferArray<T> ReallocateCopyThenDisposeSelf(int newCapacity)
        {
            this.ConditionalCheckIsCreated();
            ref var dataRef = ref DataRef;

            // We cannot support renting, as these pointers would all be invalidated, and we cannot ensure safety while doing that.
            if (dataRef.SupportsRenting)
                throw new InvalidOperationException("Cannot reallocate a VUnsafeBufferArray that supports renting SafePtrs.");

            // New data
            var newData = new VUnsafeBufferArray<T>(newCapacity, false, Allocator.Persistent);
            ref var newDataRef = ref newData.DataRef;

            // Copy data, then dispose this data
            dataRef.CopyToAndDispose(ref newDataRef);
            // Dispose data container too
            data.DisposeRefToDefault();

            return newData;
        }*/

        #region Enumeration

        /// <summary> Returns an enumerator over the active elements of this list. Does NOT include inactive elements. </summary>
        /// <returns>An enumerator over the elements of this list.</returns>
        public Enumerator GetEnumerator() => new(data);

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
            RefStruct<Data> data;
            VUnsafeBufferList<T>.Enumerator listEnumerator;
            readonly bool created;
            
            public Enumerator(RefStruct<Data> data)
            {
                this.data = data;
                listEnumerator = new(data.ValueRef.list);
                created = data.IsCreated;
            }

            public void Dispose() => listEnumerator.Dispose();
            
            public bool MoveNext() => created && listEnumerator.MoveNext();
            public void Reset() => listEnumerator.Reset();

            public T Current => listEnumerator.Current;
            object IEnumerator.Current => Current;
            
            public int CurrentIndex => listEnumerator.CurrentIndex;
            public ref T CurrentRef => ref listEnumerator.CurrentRef;
        }

        #endregion

        #region Parallel

        /// <summary> Allocates <inheritdoc cref="Parallel"/> </summary>
        public Parallel AllocParallel() => new(this);

        /// <summary> A special container that makes a <see cref="VUnsafeBufferArray{T}"/> concurrent-safe! <br/>
        ///  </summary>
        public struct Parallel
        {
            VUnsafeBufferArray<T> array;
            BurstSpinLockReadWrite spinLock;

            public Parallel(VUnsafeBufferArray<T> array)
            {
                array.ConditionalCheckIsCreated();
                this.array = array;
                spinLock = new BurstSpinLockReadWrite(Allocator.Persistent);
            }

            public void Dispose(bool disposeList = true)
            {
                spinLock.DisposeRefToDefault();
                if (disposeList)
                    array.Dispose();
            }

            public Reader UseReader(float spinTimeout = 1f)
            {
                var lockHold = spinLock.ScopedReadLock(spinTimeout);
                if (!lockHold)
                    throw new TimeoutException($"Could not acquire read lock in {spinTimeout}.");
                return new Reader(array, lockHold);
            }

            public Writer UseWriter(float spinTimeout = 1f)
            {
                var lockHold = spinLock.ScopedExclusiveLock(spinTimeout);
                if (!lockHold)
                    throw new TimeoutException($"Could not acquire write lock in {spinTimeout}.");
                return new Writer(array, lockHold);
            }

            public struct Reader : IDisposable
            {
                VUnsafeBufferArray<T> array;
                BurstScopedReadLock lockHold;

                internal Reader(VUnsafeBufferArray<T> array, BurstScopedReadLock lockHold)
                {
                    this.array = array;
                    this.lockHold = lockHold;
                }

                public void Dispose() => lockHold.Dispose();
            }

            public struct Writer : IDisposable
            {
                VUnsafeBufferArray<T> array;
                BurstScopedExclusiveLock lockHold;

                internal Writer(VUnsafeBufferArray<T> array, BurstScopedExclusiveLock lockHold)
                {
                    this.array = array;
                    this.lockHold = lockHold;
                }

                public void Dispose() => lockHold.Dispose();
            }
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

        /*[Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        void CheckHandleMatches(AllocatorManager.AllocatorHandle handle)
        {
            if (!DataRef.list.IsCreated)
                throw new ArgumentOutOfRangeException($"Allocator handle {handle} can't match because container is not initialized.");
            if (DataRef.listData.Allocator.Index != handle.Index)
                throw new ArgumentOutOfRangeException($"Allocator handle {handle} can't match because container handle index doesn't match.");
            if (DataRef.listData.Allocator.Version != handle.Version)
                throw new ArgumentOutOfRangeException($"Allocator handle {handle} matches container handle index, but has different version.");
        }*/

        #endregion

        #region Readonly

        public static implicit operator ReadOnly(VUnsafeBufferArray<T> array) => array.AsReadOnly();

        /// <summary> Returns a read only of this list. </summary>
        /// <returns>A read only of this list.</returns>
        public readonly ReadOnly AsReadOnly()
        {
            this.ConditionalCheckIsCreated();
            // Lol, just wrap the list, idk why they would input the ptr and length manually, that stuff could be modified elsewhere leading to a crash, easily.
            return new ReadOnly(this);
        }

        /// <summary> A readonly version of VUnsafeList, use AsReadOnly() to get one. </summary>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] {typeof(int)})]
        public readonly struct ReadOnly : IEnumerable<T>
        {
            readonly VUnsafeBufferArray<T> array;

            /// <summary> The number of elements. </summary>
            public readonly int Length => array.Length;

            public readonly int Capacity => array.Capacity;
            public readonly int ActiveCount => array.ActiveCount;

            public T this[int index]
            {
                get
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                    return ReadSafe(index);
#else
                    return array[index];
#endif
                }
            }

            public ReadOnly(VUnsafeBufferArray<T> array) => this.array = array;

            /// <summary> Performs a guarded read and logs and error if something was wrong. </summary>
            public readonly T ReadSafe(int index)
            {
                if (array.TryGetValue(index, out var value))
                    return value;
                UnityEngine.Debug.LogError($"Index {index} is out of range in VUnsafeList.ReadOnly of '{Length}' Length.");
                return default;
            }

            public readonly bool TryGetValue(int index, out T value) => array.TryGetValue(index, out value);

            public readonly ref readonly T TryGetRef(int index, out bool hasRef) => ref array.TryGetRefReadonly(index, out hasRef);

            /// <summary> Returns an enumerator over the elements of the array. </summary>
            public VUnsafeBufferList<T>.Enumerator GetEnumerator()
            {
                array.ConditionalCheckIsCreated();
                return new(array.DataRef.list);
            }

            /// <summary> This method is not implemented. Use <see cref="GetEnumerator"/> instead. </summary>
            /// <exception cref="NotImplementedException">Method is not implemented.</exception>
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            /// <summary>This method is not implemented. Use <see cref="GetEnumerator"/> instead.</summary>
            IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
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
        public static bool Contains<T, U>(this VUnsafeBufferArray<T> list, U value)
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
        public static int IndexOf<T, U>(this VUnsafeBufferArray<T> list, U value)
            where T : unmanaged, IEquatable<U>
        {
            list.ConditionalCheckIsCreated();
            return NativeArrayExtensions.IndexOf<T, U>(list.listData.GetUnsafePtr(), list.Length, value);
        }
    }*/
}