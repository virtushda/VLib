using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using VLib.Unsafe.Utility;
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
    public struct VUnsafeList<T> : IVLibUnsafeContainer, IDisposable /*INativeDisposable*/, INativeList<T>, IEnumerable<T>, IReadOnlyList<T> // Used by collection initializers.
        where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction] unsafe UnsafeList<T>* listData;
        public readonly unsafe ref UnsafeList<T> ListData
        {
            get
            {
                ConditionalCheckIsCreated();
                return ref *listData;
            }
        }

        public readonly unsafe ref UnsafeList<T> ListDataUnsafe => ref *listData;
        
        public readonly unsafe void* GetUnsafePtr() => ListDataUnsafe.Ptr;

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
        internal unsafe void Initialize(int initialCapacity, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            var totalSize = sizeof(T) * (long) initialCapacity;
            listData = UnsafeList<T>.Create(initialCapacity, allocator, NativeArrayOptions.UninitializedMemory);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public readonly void ConditionalCheckIsCreated()
        {
            if (!IsCreated)
                throw new InvalidOperationException("VUnsafeList has not been allocated or has been deallocated.");
        }
        public readonly bool IndexValid(int index)
        {
            // ConditionalCheckIsCreated(); // Length property checks this already
            return index < Length && index >= 0;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public readonly void ConditionalAssertIndexValid(int index)
        {
            // IndexValid checks IsCreated
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
            readonly get => ListData[index];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => ListData[index] = value;
        }

        /// <summary>
        /// Returns a reference to the element at an index.
        /// </summary>
        /// <param name="index">An index.</param>
        /// <returns>A reference to the element at the index.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if index is out of bounds.</exception>
        public readonly ref T ElementAt(int index) => ref ListData.ElementAt(index);

        public readonly ref readonly T ElementAtReadOnly(int index) => ref ListData.ElementAt(index);

        /// <summary> The count of elements. </summary>
        /// <value>The current count of elements. Always less than or equal to the capacity.</value>
        /// <remarks>To decrease the memory used by a list, set <see cref="Capacity"/> after reducing the length of the list.</remarks>
        /// <param name="value>">The new length. If the new length is greater than the current capacity, the capacity is increased.
        /// Newly allocated memory is cleared.</param>
        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => ListData.Length;
            set => ListData.Resize(value, NativeArrayOptions.ClearMemory);
        }

        ref int LengthRef => ref ListData.m_length;

        public readonly int Count => Length;

        /// <summary>
        /// The number of elements that fit in the current allocation.
        /// </summary>
        /// <value>The number of elements that fit in the current allocation.</value>
        /// <param name="value">The new capacity. Must be greater or equal to the length.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the new capacity is smaller than the length.</exception>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => ListData.Capacity;
            set => ListData.Capacity = value;
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

        public ref T TryGetRef(int index, out bool success)
        {
            success = IndexValid(index);
            if (success)
                return ref ElementAt(index);
            return ref VUnsafeUtil.NullRef<T>();
        }

        public readonly ref readonly T TryGetRefReadOnly(int index, out bool success)
        {
            success = IndexValid(index);
            if (success)
                return ref ElementAtReadOnly(index);
            return ref VUnsafeUtil.NullRef<T>();
        }

        /*public readonly bool TryGetElementPtr(int index, out T* valuePtr)
        {
            valuePtr = null;
            if (!IndexValid(index))
                return false;
            
            valuePtr = listData->Ptr + index;
            return true;
        }*/

        /// <summary> Appends an element to the end of this list. </summary>
        /// <param name="value">The value to add to the end of this list.</param>
        /// <remarks> Length is incremented by 1. Will not increase the capacity. </remarks>
        /// <exception cref="InvalidOperationException">Thrown if incrementing the length would exceed the capacity.</exception>
        public void AddNoResize(T value) => ListData.AddNoResize(value);

        /// <summary> Appends elements from a buffer to the end of this list. </summary>
        /// <param name="ptr">The buffer to copy from.</param>
        /// <param name="count">The number of elements to copy from the buffer.</param>
        /// <remarks> Length is increased by the count. Will not increase the capacity. </remarks>
        /// <exception cref="InvalidOperationException">Thrown if the increased length would exceed the capacity.</exception>
        public unsafe void AddRangeNoResize(void* ptr, int count)
        {
            ConditionalCheckArgPositive(count);
            ListData.AddRangeNoResize(ptr, count);
        }

        /// <summary>
        /// Appends the elements of another list to the end of this list.
        /// </summary>
        /// <param name="list">The other list to copy from.</param>
        /// <remarks>
        /// Length is increased by the length of the other list. Will not increase the capacity.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown if the increased length would exceed the capacity.</exception>
        public unsafe void AddRangeNoResize(NativeList<T> list) => ListData.AddRangeNoResize(*list.GetUnsafeList());

        /// <summary>
        /// Appends an element to the end of this list.
        /// </summary>
        /// <param name="value">The value to add to the end of this list.</param>
        /// <remarks>
        /// Length is incremented by 1. If necessary, the capacity is increased.
        /// </remarks>
        public void Add(in T value) => ListData.Add(in value);

        /// <summary>
        /// Appends the elements of an array to the end of this list.
        /// </summary>
        /// <param name="array">The array to copy from.</param>
        /// <remarks>
        /// Length is increased by the number of new elements. Does not increase the capacity.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the increased length would exceed the capacity.</exception>
        public unsafe void AddRange(NativeArray<T> array) => AddRange(array.GetUnsafeReadOnlyPtr(), array.Length); // Call checks is created

        public unsafe void AddRange(VUnsafeList<T> otherList, int startIndex = 0, int count = 0)
        {
            if (count <= 0)
                count = otherList.Length - startIndex; // This line checks other list is created implicitly
            
            otherList.ConditionalCheckRange(startIndex, count);
            
            ListData.AddRange(otherList.ListData.Ptr + startIndex, count); // This line check this list is created implicitly
        }

        /// <summary>
        /// Appends the elements of a buffer to the end of this list.
        /// </summary>
        /// <param name="ptr">The buffer to copy from.</param>
        /// <param name="count">The number of elements to copy from the buffer.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if count is negative.</exception>
        public unsafe void AddRange(void* ptr, int count)
        {
            ConditionalCheckArgPositive(count);
            ListData.AddRange(ptr, count); // This line checks this list is created implicitly
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
            ConditionalCheckArgPositive(count);
            ListData.AddReplicate(in value, count);
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
        public void InsertRangeWithBeginEnd(int begin, int end) => ListData.InsertRangeWithBeginEnd(begin, end); // Implicitly checks is created

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
            ListData[index] = value;
        }

        /// <summary>
        /// Copies the last element of this list to the specified index. Decrements the length by 1.
        /// </summary>
        /// <remarks>Useful as a cheap way to remove an element from this list when you don't care about preserving order.</remarks>
        /// <param name="index">The index to overwrite with the last element.</param>
        /// <exception cref="IndexOutOfRangeException">Thrown if `index` is out of bounds.</exception>
        public void RemoveAtSwapBack(int index) => ListData.RemoveAtSwapBack(index); // Implicitly checks is created

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
        public void RemoveRangeSwapBack(int index, int count) => ListData.RemoveRangeSwapBack(index, count); // Implicitly checks is created

        /// <summary>
        /// Removes the element at an index, shifting everything above it down by one. Decrements the length by 1.
        /// </summary>
        /// <param name="index">The index of the item to remove.</param>
        /// <remarks>
        /// If you don't care about preserving the order of the elements, <see cref="RemoveAtSwapBack(int)"/> is a more efficient way to remove elements.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if `index` is out of bounds.</exception>
        public void RemoveAt(int index) => ListData.RemoveAt(index); // Implicitly checks is created and unsafelist call checks index

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
        public void RemoveRange(int index, int count) => ListData.RemoveRange(index, count); // Implicitly checks is created and unsafelist call checks index

        /// <summary> Linear search removal. Not efficient with large collections. </summary>
        public bool Remove<U>(U valueToRemove)
            where U : unmanaged, IEquatable<T>, IEquatable<U>
        {
            ref var list = ref ListData;
            var length = list.Length;
            for (var i = 0; i < length; i++)
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
            var lastIndex = Length - 1; // Implicitly checks is created
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

        public unsafe void WriteValueToRange(T valueCopy, int startIndex, int count)
        {
            ConditionalCheckRange(startIndex, count);
            UnsafeUtility.MemCpyReplicate(ListData.Ptr + startIndex, &valueCopy, sizeof(T), count);
        }

        /// <summary>
        /// Whether this list is empty.
        /// </summary>
        /// <value>True if the list is empty or if the list has not been constructed.</value>
        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => !IsCreated || ListData.Length == 0;
        }

        /// <summary>
        /// Whether this list has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this list has been allocated (and not yet deallocated).</value>
        public readonly unsafe bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => listData != null;
        }

        /// <summary> Releases all resources. </summary>
        public unsafe void Dispose()
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
        public void Clear() => ListData.Clear();

        /// <summary> Returns a native array that aliases the content of this list. </summary>
        /// <returns> A native array that aliases the content of this list. <br/>
        /// The returned native array will not have a safety handle, if that is an issue use <see cref="AsArrayView"/> instead! </returns>
        public readonly unsafe NativeArray<T> AsArray()
        {
            return NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(ListData.Ptr, ListDataUnsafe.Length, Allocator.None); // Implicitly checks is created
        }

        /// <summary> Handles the atomic safety handle... </summary>
        public struct NativeArrayView : IDisposable
        {
            public static implicit operator NativeArray<T>(NativeArrayView view) => view.array;
            NativeArray<T> array;

            public NativeArray<T> Array => array;
            
            public NativeArrayView(VUnsafeList<T> list)
            {
                array = list.AsArray();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                // Setup safety
                var handle = AtomicSafetyHandle.Create();
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, handle);
#endif
            }
            
            public void Dispose()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                var handle = NativeArrayUnsafeUtility.GetAtomicSafetyHandle(array);
                AtomicSafetyHandle.CheckDeallocateAndThrow(handle);
                AtomicSafetyHandle.Release(handle);
#endif
            }
        }

        /// <summary> A version of <see cref="AsArray"/> that handles the atomic safety handle... Must be disposed to release the safety handle. </summary>
        public NativeArrayView AsArrayView() => new NativeArrayView(this);
        
        public unsafe NativeArray<T> AsArrayCustomReadonly(int size)
        {
            if (size < 0 || size > Capacity)
            {
                UnityEngine.Debug.LogError($"Size {size} is invalid for list with capacity {Capacity}");
                size = math.clamp(size, 0, Capacity);
            }
            return NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(ListData.Ptr, size, Allocator.None);
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
        public NativeArray<T> ToArray(AllocatorManager.AllocatorHandle allocator) => VCollectionUtils.ToArray<VUnsafeList<T>, T>(this, allocator);

        public T[] ToManagedArray() => VCollectionUtils.ToManagedArray<VUnsafeList<T>, T>(this);

        /// <summary>
        /// Copies all elements of specified container to this container.
        /// </summary>
        /// <param name="other">An container to copy into this container.</param>
        public void CopyFrom(in NativeArray<T> other)
        {
            other.ConditionalCheckIsCreated();
            VCollectionUtils.CopyFromTo(other.AsUnsafeList(NativeSafety.ReadOnly), this);
        }

        /// <summary>
        /// Copies all elements of specified container to this container.
        /// </summary>
        /// <param name="other">An container to copy into this container.</param>
        public void CopyFrom(in UnsafeList<T> other)
        {
            other.ConditionalCheckIsCreated();
            VCollectionUtils.CopyFromTo(other, this);
        }

        /// <summary>
        /// Copies all elements of specified container to this container.
        /// </summary>
        /// <param name="other">An container to copy into this container.</param>
        public void CopyFrom(in VUnsafeList<T> other) => CopyFrom(other.ListDataUnsafe);

        /// <summary>
        /// Copies all elements of specified container to this container.
        /// </summary>
        /// <param name="other">An container to copy into this container.</param>
        public unsafe void CopyFrom(ref NativeList<T> other)
        {
            var otherUnsafe = other.GetUnsafeList();
            VCollectionUtils.CheckPtrNonNull(otherUnsafe);
            CopyFrom(*otherUnsafe);
        }

        /// <summary> Treats the list like an array. Length MUST accommodate the copy range. </summary>
        public void CopyToNoAdd(VUnsafeList<T> dest, int sourceStart = 0, int destStart = 0, int count = 0)
        {
            CopyToNoAdd(dest.ListData, sourceStart, destStart, count); // Implicitly checks is created
        }

        /// <summary> Treats the list like an array. Length MUST accommodate the copy range. </summary>
        public void CopyToNoAdd(UnsafeList<T> dest, int sourceStart = 0, int destStart = 0, int count = 0)
        {
            VCollectionUtils.CopyToAsArray(this, dest, sourceStart, destStart, count);
        }
        
        /// <summary> Treats the list like an array. Length MUST accommodate the copy range. </summary>
        public void CopyFromAsArray(VUnsafeList<T> source, int sourceStart = 0, int destStart = 0, int count = 0)
        {
            CopyFromAsArray(source.ListDataUnsafe, sourceStart, destStart, count); // Call internally checks is created
        }

        /// <summary> Treats the list like an array. Length MUST accommodate the copy range. </summary>
        public void CopyFromAsArray(UnsafeList<T> source, int sourceStart = 0, int destStart = 0, int count = 0)
        {
            VCollectionUtils.CopyFromAsArray(this, source, sourceStart, destStart, count);
        }

        /// <summary>
        /// Sets the length of this list, increasing the capacity if necessary.
        /// </summary>
        /// <param name="length">The new length of this list.</param>
        /// <param name="options">Whether to clear any newly allocated bytes to all zeroes.</param>
        public void Resize(int length, NativeArrayOptions options) => ListData.Resize(length, options); // Implicitly checks is created

        /// <summary>
        /// Sets the length of this list, increasing the capacity if necessary.
        /// </summary>
        /// <remarks>Does not clear newly allocated bytes.</remarks>
        /// <param name="length">The new length of this list.</param>
        public void ResizeUninitialized(int length) => Resize(length, NativeArrayOptions.UninitializedMemory);

        /// <summary>
        /// Sets the capacity.
        /// </summary>
        /// <param name="capacity">The new capacity.</param>
        public void SetCapacity(int capacity) => ListData.SetCapacity(capacity); // Implicitly checks is created

        /// <summary>
        /// Sets the capacity to match the length.
        /// </summary>
        public void TrimExcess() => ListData.TrimExcess(); // Implicitly checks is created

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
        public unsafe Enumerator GetEnumerator() => new Enumerator { sourceList = this, m_Index = -1 };

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

        /// <summary>
        /// An enumerator over the elements of a list. Copied from UnsafeList. Any operation that moves the list's memory may invalidate the enumerator and lead to dangerous behaviour.
        /// </summary>
        /// <remarks>
        /// In an enumerator's initial state, <see cref="Current"/> is invalid.
        /// The first <see cref="MoveNext"/> call advances the enumerator to the first element of the list.
        /// </remarks>
        public struct Enumerator : IEnumerator<T>
        {
            internal VUnsafeList<T> sourceList;
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
            public bool MoveNext() => ++m_Index < sourceList.Length;

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
                get => sourceList[m_Index];
            }

            object IEnumerator.Current => Current;
        }
        
        #endregion

        #region ParallelWriter

        /// <summary>
        /// Returns a parallel writer of this list.
        /// </summary>
        /// <returns>A parallel writer of this list.</returns>
        public ParallelWriter AsParallelWriter() => new ParallelWriter(this);

        /// <summary>
        /// A parallel writer for a VUnsafeList.
        /// </summary>
        /// <remarks>
        /// Use <see cref="AsParallelWriter"/> to create a parallel writer for a list.
        /// </remarks>
        //[NativeContainer]
        //[NativeContainerIsAtomicWriteOnly]
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] {typeof(int)})]
        public struct ParallelWriter
        {
            /// <summary>
            /// The internal unsafe list.
            /// </summary>
            /// <value>The internal unsafe list.</value>
            VUnsafeList<T> list;

            public readonly bool IsCreated => list.IsCreated;

            internal ParallelWriter(VUnsafeList<T> listData) => list = listData;

            /// <summary>
            /// Appends an element to the end of this list.
            /// </summary>
            /// <param name="value">The value to add to the end of this list.</param>
            /// <remarks>
            /// Increments the length by 1 unless doing so would exceed the current capacity.
            /// </remarks>
            /// <exception cref="InvalidOperationException">Thrown if adding an element would exceed the capacity.</exception>
            public unsafe void AddNoResize(T value)
            {
                var idx = Interlocked.Increment(ref list.LengthRef) - 1; // Implicitly checks is created
                CheckSufficientCapacity(list.Capacity, idx + 1);

                UnsafeUtility.WriteArrayElement(list.ListDataUnsafe.Ptr, idx, value);
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
            public unsafe void AddRangeNoResize(void* ptr, int count)
            {
                ConditionalCheckArgPositive(count);
                
                var idx = Interlocked.Add(ref list.LengthRef, count) - count; // Implicitly checks is created
                CheckSufficientCapacity(list.Capacity, idx + count);

                var sizeOf = sizeof(T);
                void* dst = ((byte*)list.ListDataUnsafe.Ptr) + idx * sizeOf;
                UnsafeUtility.MemCpy(dst, ptr, count * sizeOf);
            }

            /// <summary>
            /// Appends the elements of another list to the end of this list.
            /// </summary>
            /// <param name="incomingList">The other list to copy from.</param>
            /// <remarks>
            /// Increments the length of this list by the length of the other list unless doing so would exceed the current capacity.
            /// </remarks>
            /// <exception cref="InvalidOperationException">Thrown if adding the elements would exceed the capacity.</exception>
            public unsafe void AddRangeNoResize(UnsafeList<T> incomingList) => AddRangeNoResize(incomingList.Ptr, incomingList.Length);

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

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
            void CheckIsCreated()
            {
                if (!IsCreated)
                    throw new InvalidOperationException("VUnsafeList ParallelWriter has not been initialized.");
            }
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
        static void ConditionalCheckArgPositive(int value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException($"Value {value} must be positive.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        unsafe void CheckHandleMatches(AllocatorManager.AllocatorHandle handle)
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
            ConditionalCheckIsCreated();
            // Lol, just wrap the list, idk why they would input the ptr and length manually, that stuff could be modified elsewhere leading to a crash, easily.
            return new ReadOnly(this);
        }

        /// <summary> A readonly version of VUnsafeList, use AsReadOnly() to get one. </summary>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        public readonly struct ReadOnly
            : IReadOnlyList<T>
        {
            readonly VUnsafeList<T> list;

            public bool IsCreated => list.IsCreated;
            
            /// <summary> The number of elements. </summary>
            public int Length => list.Length;

            public int Count => list.Length;
            
            public int Capacity => list.Capacity;

            public T this[int index] => list[index];

            public ReadOnly(VUnsafeList<T> list) => this.list = list;

            /// <summary> Performs a guarded read and logs and error if something was wrong. </summary>
            public T GetValueOrDefault(int index)
            {
                if (list.TryGetValue(index, out var value))
                    return value;
                UnityEngine.Debug.LogError($"Index {index} is out of range in VUnsafeList.ReadOnly of '{Length}' Length.");
                return default;
            }
            
            public ref readonly T ElementAtReadOnly(int index) => ref list.ElementAtReadOnly(index);
            
            public bool TryGetValue(int index, out T value) => list.TryGetValue(index, out value);
            
            public ref readonly T TryGetRef(int index, out bool hasRef) => ref list.TryGetRefReadOnly(index, out hasRef);

            /// <summary>
            /// Returns an enumerator over the elements of the list.
            /// </summary>
            /// <returns>An enumerator over the elements of the list.</returns>
            public Enumerator GetEnumerator() => new() { sourceList = list, m_Index = -1 };

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
        }

        #endregion
        
        #region Slicing

        /// <summary> An unsafe window into a portion of the list. <br/>
        /// Providing a length allocator allows the slice to act like its own list with constrained capacity, but at the cost of a small allocation. </summary>
        public VUnsafeListSlice<T> Slice(int start, int length, Allocator lengthAllocator)
        {
            CheckIndexInRange(start, Length); // Implicitly checks is created
            // Allow for empty slices, but put logic in conditional so it is stripped with the conditional
            CheckIndexInRange(length > 0 ? start + length - 1 : start, Length);
            return new VUnsafeListSlice<T>(this, start, length, lengthAllocator);
        }
        
        /*/// <summary> An unsafe window into a portion of the list. <br/>
        /// WARNING: This enables optimization, but if the list is used after the provided memory is moved or disposed, the slice will have undefined behaviour. </summary>
        public VUnsafeListSlice<T> SliceWithExternalAlloc(int start, int length, VUnsafeRef<int> externalLengthMemory)
        {
            CheckIndexInRange(start, Length);
            CheckIndexInRange(start + length - 1, Length);
            return new VUnsafeListSlice<T>(this, start, length, externalLengthMemory);
        }*/
        
        #endregion

        public void ClearUnusedMemory() => ListData.ClearUnusedMemory();
        
        public JobHandle DisposeAfter(JobHandle inDeps) => new DisposeJob<T> { list = this }.Schedule(inDeps);

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void ConditionalCheckRange(int start, int count)
        {
            if (start < 0)
                throw new ArgumentOutOfRangeException(nameof(start), $"Start is '{start}', must be >= 0.");
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), $"Count is '{count}', must be >= 0.");
            if (start + count > Length)
                throw new ArgumentOutOfRangeException(nameof(count), $"Start + Count is '{start + count}', must be <= Length '{Length}'.");
        }
        
        [BurstDiscard]
        public override string ToString() => $"Length:{Length} | Capacity:{Capacity} | Type:{typeof(T)}";
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
    public static class VUnsafeListExtensions
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
        public static unsafe bool Contains<T, U>(this VUnsafeList<T> list, U value)
            where T : unmanaged, IEquatable<U>
        {
            return NativeArrayExtensions.IndexOf<T, U>(list.ListData.Ptr, list.Length, value) != -1; // Implicitly checks is created
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
        public static unsafe int IndexOf<T, U>(this VUnsafeList<T> list, U value)
            where T : unmanaged, IEquatable<U>
        {
            return NativeArrayExtensions.IndexOf<T, U>(list.ListData.Ptr, list.Length, value); // Implicitly checks is created
        }
    }
}