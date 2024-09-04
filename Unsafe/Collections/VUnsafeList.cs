using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using VLib.UnsafeListSlicing;

namespace VLib
{
    // Note from Seth: Didn't implement every last bell and whistle, but it's good enough for now.
    
    /// <summary> An unmanaged, resizable list. </summary>
    /// <remarks>The elements are stored contiguously in a buffer rather than as linked nodes.</remarks>
    /// <typeparam name="T">The type of the elements.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    //[NativeContainer]
    //[DebuggerDisplay("Length = {m_ListData == null ? default : m_ListData->Length}, Capacity = {m_ListData == null ? default : m_ListData->Capacity}")]
    //[DebuggerTypeProxy(typeof(NativeListDebugView<>))]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] {typeof(int)})]
    public unsafe struct VUnsafeList<T> : IDisposable /*INativeDisposable*/, INativeList<T>, IEnumerable<T>, IReadOnlyList<T> // Used by collection initializers.
        where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction] public UnsafeList<T>* listData;

        /// <summary> Initializes and returns a VUnsafeList with a capacity of one. </summary>
        /// <param name="allocator">The allocator to use.</param>
        public VUnsafeList(Allocator allocator) : this(1, allocator, NativeArrayOptions.UninitializedMemory)
        {
        }

        /// <summary> Initializes and returns a VUnsafeList. </summary>
        /// <param name="initialCapacity">The initial capacity of the list.</param>
        /// <param name="allocator">The allocator to use.</param>
        public VUnsafeList(int initialCapacity, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            this = default;
            Initialize(initialCapacity, allocator);
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] {typeof(AllocatorManager.AllocatorHandle)})]
        internal void Initialize(int initialCapacity, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            var totalSize = sizeof(T) * (long) initialCapacity;
            listData = UnsafeList<T>.Create(initialCapacity, allocator, NativeArrayOptions.UninitializedMemory);
        }

        public readonly void AssertIsCreated()
        {
            if (!IsCreated)
                throw new InvalidOperationException("VUnsafeList has not been allocated or has been deallocated.");
        }

        public readonly bool IndexValid(int index)
        {
            return index < Length && index >= 0;
        }

        public void AssertIndexValid(int index)
        {
            AssertIsCreated();
            if (!IndexValid(index))
                throw new IndexOutOfRangeException($"Index {index} is out of range in VUnsafeList of '{Length}' Length.");
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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AssertIndexValid(index);
#endif
                return (*listData)[index];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AssertIndexValid(index);
#endif
                (*listData)[index] = value;
            }
        }

        /// <summary>
        /// Returns a reference to the element at an index.
        /// </summary>
        /// <param name="index">An index.</param>
        /// <returns>A reference to the element at the index.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if index is out of bounds.</exception>
        public ref T ElementAt(int index)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AssertIndexValid(index);
#endif
            return ref listData->ElementAt(index);
        }

        /// <summary> The count of elements. </summary>
        /// <value>The current count of elements. Always less than or equal to the capacity.</value>
        /// <remarks>To decrease the memory used by a list, set <see cref="Capacity"/> after reducing the length of the list.</remarks>
        /// <param name="value>">The new length. If the new length is greater than the current capacity, the capacity is increased.
        /// Newly allocated memory is cleared.</param>
        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AssertIsCreated();
#endif
                return listData->Length;
            }

            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AssertIsCreated();
#endif
                listData->Resize(value, NativeArrayOptions.ClearMemory);
            }
        }

        public int Count => Length;

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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AssertIsCreated();
#endif
                return listData->Capacity;
            }

            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AssertIsCreated();
#endif
                listData->Capacity = value;
            }
        }
        
        public readonly bool TryGetValue(int index, out T value)
        {
            if (IndexValid(index))
            {
                value = this[index];
                return true;
            }

            value = default;
            return false;
        }

        public readonly bool TryGetElementPtr(int index, out T* valuePtr)
        {
            valuePtr = null;
            if (!IndexValid(index))
                return false;
            
            valuePtr = listData->Ptr + index;
            return true;
        }

        /// <summary> Appends an element to the end of this list. </summary>
        /// <param name="value">The value to add to the end of this list.</param>
        /// <remarks> Length is incremented by 1. Will not increase the capacity. </remarks>
        /// <exception cref="InvalidOperationException">Thrown if incrementing the length would exceed the capacity.</exception>
        public void AddNoResize(T value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AssertIsCreated();
#endif
            listData->AddNoResize(value);
        }

        /// <summary> Appends elements from a buffer to the end of this list. </summary>
        /// <param name="ptr">The buffer to copy from.</param>
        /// <param name="count">The number of elements to copy from the buffer.</param>
        /// <remarks> Length is increased by the count. Will not increase the capacity. </remarks>
        /// <exception cref="InvalidOperationException">Thrown if the increased length would exceed the capacity.</exception>
        public void AddRangeNoResize(void* ptr, int count)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AssertIsCreated();
#endif
            CheckArgPositive(count);
            listData->AddRangeNoResize(ptr, count);
        }

        /// <summary>
        /// Appends the elements of another list to the end of this list.
        /// </summary>
        /// <param name="list">The other list to copy from.</param>
        /// <remarks>
        /// Length is increased by the length of the other list. Will not increase the capacity.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown if the increased length would exceed the capacity.</exception>
        public void AddRangeNoResize(NativeList<T> list)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AssertIsCreated();
#endif
            listData->AddRangeNoResize(*listData);
        }

        /// <summary>
        /// Appends an element to the end of this list.
        /// </summary>
        /// <param name="value">The value to add to the end of this list.</param>
        /// <remarks>
        /// Length is incremented by 1. If necessary, the capacity is increased.
        /// </remarks>
        public void Add(in T value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AssertIsCreated();
#endif
            listData->Add(in value);
        }

        /// <summary>
        /// Appends the elements of an array to the end of this list.
        /// </summary>
        /// <param name="array">The array to copy from.</param>
        /// <remarks>
        /// Length is increased by the number of new elements. Does not increase the capacity.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the increased length would exceed the capacity.</exception>
        public void AddRange(NativeArray<T> array)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AssertIsCreated();
#endif
            AddRange(array.GetUnsafeReadOnlyPtr(), array.Length);
        }

        /// <summary>
        /// Appends the elements of a buffer to the end of this list.
        /// </summary>
        /// <param name="ptr">The buffer to copy from.</param>
        /// <param name="count">The number of elements to copy from the buffer.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if count is negative.</exception>
        public void AddRange(void* ptr, int count)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AssertIsCreated();
#endif
            CheckArgPositive(count);
            listData->AddRange(ptr, count);
        }

        /// <summary>
        /// Appends value count times to the end of this list.
        /// </summary>
        /// <param name="value">The value to add to the end of this list.</param>
        /// <param name="count">The number of times to replicate the value.</param>
        /// <remarks>
        /// Length is incremented by count. If necessary, the capacity is increased.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if count is negative.</exception>
        public void AddReplicate(in T value, int count)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AssertIsCreated();
#endif
            CheckArgPositive(count);
            listData->AddReplicate(in value, count);
        }

        /// <summary>
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
        public void InsertRangeWithBeginEnd(int begin, int end)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AssertIsCreated();
#endif
            listData->InsertRangeWithBeginEnd(begin, end);
        }

        /// <summary>
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
        public void InsertRange(int index, int count) => InsertRangeWithBeginEnd(index, index + count);

        public void Insert(int index, T value)
        {
            InsertRangeWithBeginEnd(index, index + 1);
            (*listData)[index] = value;
        }

        /// <summary>
        /// Copies the last element of this list to the specified index. Decrements the length by 1.
        /// </summary>
        /// <remarks>Useful as a cheap way to remove an element from this list when you don't care about preserving order.</remarks>
        /// <param name="index">The index to overwrite with the last element.</param>
        /// <exception cref="IndexOutOfRangeException">Thrown if `index` is out of bounds.</exception>
        public void RemoveAtSwapBack(int index)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AssertIsCreated();
#endif
            listData->RemoveAtSwapBack(index);
        }

        /// <summary> Copies the last *N* elements of this list to a range in this list. Decrements the length by *N*. </summary>
        /// <remarks>
        /// Copies the last `count` elements to the indexes `index` up to `index + count`.
        ///
        /// Useful as a cheap way to remove elements from a list when you don't care about preserving order.
        /// </remarks>
        /// <param name="index">The index of the first element to overwrite.</param>
        /// <param name="count">The number of elements to copy and remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if `index` is out of bounds, `count` is negative,
        /// or `index + count` exceeds the length.</exception>
        public void RemoveRangeSwapBack(int index, int count)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AssertIsCreated();
#endif
            listData->RemoveRangeSwapBack(index, count);
        }

        /// <summary>
        /// Removes the element at an index, shifting everything above it down by one. Decrements the length by 1.
        /// </summary>
        /// <param name="index">The index of the item to remove.</param>
        /// <remarks>
        /// If you don't care about preserving the order of the elements, <see cref="RemoveAtSwapBack(int)"/> is a more efficient way to remove elements.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if `index` is out of bounds.</exception>
        public void RemoveAt(int index)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AssertIndexValid(index);
#endif
            listData->RemoveAt(index);
        }

        /// <summary>
        /// Removes *N* elements in a range, shifting everything above the range down by *N*. Decrements the length by *N*.
        /// </summary>
        /// <param name="index">The index of the first element to remove.</param>
        /// <param name="count">The number of elements to remove.</param>
        /// <remarks>
        /// If you don't care about preserving the order of the elements, `RemoveRangeSwapBackWithBeginEnd`
        /// is a more efficient way to remove elements.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if `index` is out of bounds, `count` is negative,
        /// or `index + count` exceeds the length.</exception>
        public void RemoveRange(int index, int count)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AssertIsCreated();
#endif
            listData->RemoveRange(index, count);
        }
        
        public bool Remove<U>(U valueToRemove)
            where U : unmanaged, IEquatable<T>, IEquatable<U>
        {
            var list = *listData;
            for (var i = 0; i < list.Length; i++)
            {
                var value = list[i];
                if (value.Equals(valueToRemove))
                {
                    RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        /// <summary> Try to remove and return a value from the end of the list. False if list is empty. Calling on an uncreated list will throw. </summary>
        public bool TryPopLastElement(out T value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AssertIsCreated();
#endif
            var lastIndex = Length - 1;
            if (lastIndex < 0)
            {
                value = default;
                return false;
            }

            value = this[lastIndex];
            // Trim last element off implicitly
            Length--;
            return true;
        }

        /// <summary>
        /// Whether this list is empty.
        /// </summary>
        /// <value>True if the list is empty or if the list has not been constructed.</value>
        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => listData == null || listData->Length == 0;
        }

        /// <summary>
        /// Whether this list has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this list has been allocated (and not yet deallocated).</value>
        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => listData != null;
        }

        /// <summary> Releases all resources. </summary>
        public void Dispose()
        {
            if (!IsCreated)
                return;
            UnsafeList<T>.Destroy(listData);
            listData = null;
        }

        /// <summary> Makes it easier to switch from collection types that had this extension method even though it's not needed for this one. </summary>
        public void DisposeSafe() => Dispose();

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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AssertIsCreated();
#endif
            listData->Clear();
        }

        /// <summary>
        /// **Obsolete.** Use <see cref="AsArray"/> method to do explicit cast instead.
        /// </summary>
        /// <remarks>
        /// Returns a native array that aliases the content of a list.
        /// </remarks>
        /// <param name="nativeList">The list to alias.</param>
        /// <returns>A native array that aliases the content of the list.</returns>
        /*[Obsolete("Implicit cast from `NativeList<T>` to `NativeArray<T>` has been deprecated; Use '.AsArray()' method to do explicit cast instead.", false)]
        public static implicit operator NativeArray<T>(VUnsafeList<T> list) => list.AsArray();*/

        /// <summary> Returns a native array that aliases the content of this list. </summary>
        /// <returns> A native array that aliases the content of this list. </returns>
        public NativeArray<T> AsArray()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AssertIsCreated();
#endif
            return NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(listData->Ptr, listData->Length, Allocator.None);
        }
        
        public NativeArray<T> AsArrayCustomReadonly(int size)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (size < 0 || size > Capacity)
            {
                UnityEngine.Debug.LogError($"Size {size} is invalid for list with capacity {Capacity}");
                size = math.clamp(size, 0, Capacity);
            }
#endif
            return NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(listData->Ptr, size, Allocator.None);
        }

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
        public NativeArray<T> ToArray(AllocatorManager.AllocatorHandle allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AssertIsCreated();
#endif
            NativeArray<T> result = CollectionHelper.CreateNativeArray<T>(Length, allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeUtility.MemCpy((byte*) result.ForceGetUnsafePtrNOSAFETY(), (byte*) listData->Ptr, Length * UnsafeUtility.SizeOf<T>());
            return result;
        }

        /// <summary>
        /// Copies all elements of specified container to this container.
        /// </summary>
        /// <param name="other">An container to copy into this container.</param>
        public void CopyFrom(in NativeArray<T> other)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AssertIsCreated();
#endif
            listData->CopyFrom(other);
        }

        /// <summary>
        /// Copies all elements of specified container to this container.
        /// </summary>
        /// <param name="other">An container to copy into this container.</param>
        public void CopyFrom(in UnsafeList<T> other)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AssertIsCreated();
#endif
            listData->CopyFrom(other);
        }

        /// <summary>
        /// Copies all elements of specified container to this container.
        /// </summary>
        /// <param name="other">An container to copy into this container.</param>
        public void CopyFrom(in VUnsafeList<T> other) => CopyFrom(*other.listData);

        /// <summary>
        /// Copies all elements of specified container to this container.
        /// </summary>
        /// <param name="other">An container to copy into this container.</param>
        public void CopyFrom(ref NativeList<T> other) => CopyFrom(*other.GetUnsafeList());

        /// <summary>
        /// Sets the length of this list, increasing the capacity if necessary.
        /// </summary>
        /// <param name="length">The new length of this list.</param>
        /// <param name="options">Whether to clear any newly allocated bytes to all zeroes.</param>
        public void Resize(int length, NativeArrayOptions options)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AssertIsCreated();
#endif
            listData->Resize(length, options);
        }

        /// <summary>
        /// Sets the length of this list, increasing the capacity if necessary.
        /// </summary>
        /// <remarks>Does not clear newly allocated bytes.</remarks>
        /// <param name="length">The new length of this list.</param>
        public void ResizeUninitialized(int length)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AssertIsCreated();
#endif
            Resize(length, NativeArrayOptions.UninitializedMemory);
        }

        /// <summary>
        /// Sets the capacity.
        /// </summary>
        /// <param name="capacity">The new capacity.</param>
        public void SetCapacity(int capacity)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AssertIsCreated();
#endif
            listData->SetCapacity(capacity);
        }

        /// <summary>
        /// Sets the capacity to match the length.
        /// </summary>
        public void TrimExcess()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AssertIsCreated();
#endif
            listData->TrimExcess();
        }

        // wtf is a "parallel reader"?
        /*/// <summary>
        /// Returns a parallel reader of this list.
        /// </summary>
        /// <returns>A parallel reader of this list.</returns>
//        [Obsolete("'AsParallelReader' has been deprecated; use 'AsReadOnly' instead. (UnityUpgradable) -> AsReadOnly")]
        public NativeArray<T>.ReadOnly AsParallelReader()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new NativeArray<T>.ReadOnly(listData->Ptr, listData->Length, ref m_Safety);
#else
            return new NativeArray<T>.ReadOnly(m_ListData->Ptr, m_ListData->Length);
#endif
        }*/
        
        #region Enumeration
        
        /// <summary>
        /// Returns an enumerator over the elements of this list.
        /// </summary>
        /// <returns>An enumerator over the elements of this list.</returns>
        public Enumerator GetEnumerator() => new Enumerator { m_Ptr = listData->Ptr, m_Length = Length, m_Index = -1 };

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
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw new NotImplementedException();

        /// <summary>
        /// An enumerator over the elements of a list. Copied from UnsafeList.
        /// </summary>
        /// <remarks>
        /// In an enumerator's initial state, <see cref="Current"/> is invalid.
        /// The first <see cref="MoveNext"/> call advances the enumerator to the first element of the list.
        /// </remarks>
        public struct Enumerator : IEnumerator<T>
        {
            internal T* m_Ptr;
            internal int m_Length;
            internal int m_Index;

            /// <summary>
            /// Does nothing.
            /// </summary>
            public void Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next element of the list.
            /// </summary>
            /// <remarks>
            /// The first `MoveNext` call advances the enumerator to the first element of the list. Before this call, `Current` is not valid to read.
            /// </remarks>
            /// <returns>True if `Current` is valid to read after the call.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++m_Index < m_Length;

            /// <summary>
            /// Resets the enumerator to its initial state.
            /// </summary>
            public void Reset() => m_Index = -1;

            /// <summary>
            /// The current element.
            /// </summary>
            /// <value>The current element.</value>
            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_Ptr[m_Index];
            }

            object IEnumerator.Current => Current;
        }
        
        #endregion

        #region ParallelWriter

        /// <summary>
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
        //[NativeContainer]
        //[NativeContainerIsAtomicWriteOnly]
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] {typeof(int)})]
        public unsafe struct ParallelWriter
        {
            /// <summary>
            /// The data of the list.
            /// </summary>
            public readonly void* Ptr
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ListData->Ptr;
            }

            /// <summary>
            /// The internal unsafe list.
            /// </summary>
            /// <value>The internal unsafe list.</value>
            [NativeDisableUnsafePtrRestriction] public UnsafeList<T>* ListData;

            public bool IsCreated => ListData != null && ListData->IsCreated;

            internal unsafe ParallelWriter(UnsafeList<T>* listData) => ListData = listData;

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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (!IsCreated)
                    throw new InvalidOperationException("ParallelWriter can not be written to because it was not initialized");
#endif
                var idx = Interlocked.Increment(ref ListData->m_length) - 1;
                CheckSufficientCapacity(ListData->Capacity, idx + 1);

                UnsafeUtility.WriteArrayElement(ListData->Ptr, idx, value);
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

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (!IsCreated)
                    throw new InvalidOperationException("ParallelWriter can not be written to because it was not initialized");
#endif
                var idx = Interlocked.Add(ref ListData->m_length, count) - count;
                CheckSufficientCapacity(ListData->Capacity, idx + count);

                var sizeOf = sizeof(T);
                void* dst = ((byte*)ListData->Ptr) + idx * sizeOf;
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

            /*/// <summary>
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
            }*/
        }

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
            if (listData == null)
                throw new ArgumentOutOfRangeException($"Allocator handle {handle} can't match because container is not initialized.");
            if (listData->Allocator.Index != handle.Index)
                throw new ArgumentOutOfRangeException($"Allocator handle {handle} can't match because container handle index doesn't match.");
            if (listData->Allocator.Version != handle.Version)
                throw new ArgumentOutOfRangeException($"Allocator handle {handle} matches container handle index, but has different version.");
        }

        #endregion

        #region Readonly
        
        public static implicit operator ReadOnly(VUnsafeList<T> list) => list.AsReadOnly();

        /// <summary> Returns a read only of this list. </summary>
        /// <returns>A read only of this list.</returns>
        public ReadOnly AsReadOnly()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AssertIsCreated();
#endif
            // Lol, just wrap the list, idk why they would input the ptr and length manually, that stuff could be modified elsewhere leading to a crash, easily.
            return new ReadOnly(this);
        }

        /// <summary> A readonly version of VUnsafeList, use AsReadOnly() to get one. </summary>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        public readonly unsafe struct ReadOnly
            : IReadOnlyList<T>
        {
            readonly VUnsafeList<T> list;
            
            /// <summary> The internal buffer of the list. </summary>
            public readonly T* Ptr => list.listData->Ptr;

            /// <summary> The number of elements. </summary>
            public readonly int Length => list.Length;

            public readonly int Count => list.Length;
            
            public readonly int Capacity => list.Capacity;
            
            public T this[int index] => list[index];

            public ReadOnly(VUnsafeList<T> list) => this.list = list;

            /// <summary> Performs a guarded read and logs and error if something was wrong. </summary>
            public readonly T GetValueOrDefault(int index)
            {
                if (list.TryGetValue(index, out var value))
                    return value;
                UnityEngine.Debug.LogError($"Index {index} is out of range in VUnsafeList.ReadOnly of '{Length}' Length.");
                return default;
            }
            
            public readonly bool TryGetValue(int index, out T value) => list.TryGetValue(index, out value);

            /// <summary>
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
            IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw new NotImplementedException();
        }

        #endregion
        
        #region Slicing

        /// <summary> An unsafe window into a portion of the list. </summary>
        public VUnsafeListSlice<T> Slice(int start, int length)
        {
            CheckIndexInRange(start, Length);
            CheckIndexInRange(start + length - 1, Length);
            return new VUnsafeListSlice<T>(this, start, length);
        }
        
        #endregion

        public void ClearUnusedMemory() => listData->ClearUnusedMemory();
        
        public JobHandle DisposeAfter(JobHandle inDeps) => new DisposeJob<T> { list = this }.Schedule(inDeps);
    }

    internal struct DisposeJob<T> : IJob
        where T : unmanaged
    {
        internal VUnsafeList<T> list;
        
        public void Execute() => list.Dispose();
    }

    /// <summary>
    /// Provides extension methods for UnsafeList.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public unsafe static class VUnsafeListExtensions
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
        public static bool Contains<T, U>(this VUnsafeList<T> list, U value)
            where T : unmanaged, IEquatable<U>
        {
            return NativeArrayExtensions.IndexOf<T, U>(list.listData->Ptr, list.Length, value) != -1;
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
        public static int IndexOf<T, U>(this VUnsafeList<T> list, U value)
            where T : unmanaged, IEquatable<U>
        {
            return NativeArrayExtensions.IndexOf<T, U>(list.listData->Ptr, list.Length, value);
        }
    }
}