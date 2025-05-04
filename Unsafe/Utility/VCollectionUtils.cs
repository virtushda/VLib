using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Debug = UnityEngine.Debug;

namespace VLib
{
    public static class VCollectionUtils
    {
        #region Copy
        
        /// <summary> Memcpy wrapper that gives a bit more readable control, sparing you some pointer math. Zero safety. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void MemcpyTyped<T>(T* srcPtr, int srcIndex, T* dstPtr, int dstIndex, int length)
            where T : unmanaged
        {
            var source = srcPtr + srcIndex;
            var destination = dstPtr + dstIndex;
            UnsafeUtility.MemCpy(destination, source, length * UnsafeUtility.SizeOf<T>());
        }
        
        #endregion
        
        #region Queries

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IndexIsValid(int index, int length)
        {
            if ((uint)index < length)
                return true;
            Debug.LogError($"Invalid index {index}, collection length is {length}");
            return false;
        }

        #endregion

        #region Checks

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static unsafe void CheckPtrNonNull(void* ptr)
        {
            if (ptr == null)
                throw new NullReferenceException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static unsafe void CheckPtrNonNull(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                throw new NullReferenceException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void ConditionalCheckValueNonZero(int value)
        {
            if (value == 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Value must be greater than zero.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void ConditionalCheckValueGreaterThanZero(int value)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Value must be greater than zero.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void ConditionalCheckIndexValid(int index, int length)
        {
            if ((uint)index >= length)
                throw new IndexOutOfRangeException($"{index} out of range [0..{length}] (inclusive, exclusive)");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void ConditionalCheckLengthValid(int length, int capacity)
        {
            if ((uint)length > capacity)
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be >= 0 and <= capacity.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void ConditionalCheckRangeValid(int start, int count, int collectionLength)
        {
            if (start < 0)
                throw new ArgumentOutOfRangeException(nameof(start), "Start index must be >= 0");
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be >= 0");
            if (start + count > collectionLength)
                throw new ArgumentOutOfRangeException(nameof(count), "Start index + count must be <= length");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void ConditionalCheckIsCreated<T>(this NativeArray<T> array)
            where T : struct
        {
            if (!array.IsCreated)
                throw new InvalidOperationException("NativeArray is not created.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void ConditionalCheckIsCreated<T>(this NativeArray<T>.ReadOnly array)
            where T : struct
        {
            if (!array.IsCreated)
                throw new InvalidOperationException("NativeArray<T>.ReadOnly is not created.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void ConditionalCheckIsCreated<T>(this NativeList<T> list)
            where T : unmanaged
        {
            if (!list.IsCreated)
                throw new InvalidOperationException("NativeList is not created.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void ConditionalCheckIsCreated<T>(in this UnsafeList<T> list)
            where T : unmanaged
        {
            if (!list.IsCreated)
                throw new InvalidOperationException("UnsafeList is not created.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void ConditionalCheckIsCreated<T>(this NativeHashSet<T> set)
            where T : unmanaged, IEquatable<T>
        {
            if (!set.IsCreated)
                throw new InvalidOperationException("NativeHashSet is not created.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void ConditionalCheckIsCreated<T>(in this UnsafeHashSet<T> set)
            where T : unmanaged, IEquatable<T>
        {
            if (!set.IsCreated)
                throw new InvalidOperationException("UnsafeHashSet is not created.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void ConditionalCheckIsCreated<T, U>(this NativeHashMap<T, U> set)
            where T : unmanaged, IEquatable<T>
            where U : unmanaged
        {
            if (!set.IsCreated)
                throw new InvalidOperationException("NativeHashMap is not created.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void ConditionalCheckIsCreated<T, U>(in this UnsafeHashMap<T, U> set)
            where T : unmanaged, IEquatable<T>
            where U : unmanaged
        {
            if (!set.IsCreated)
                throw new InvalidOperationException("UnsafeHashMap is not created.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void ConditionalCheckIsCreated<T>(this NativeParallelHashSet<T> set)
            where T : unmanaged, IEquatable<T>
        {
            if (!set.IsCreated)
                throw new InvalidOperationException("NativeParallelHashSet is not created.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void ConditionalCheckIsCreated<T>(this UnsafeParallelHashSet<T> set)
            where T : unmanaged, IEquatable<T>
        {
            if (!set.IsCreated)
                throw new InvalidOperationException("UnsafeParallelHashSet is not created.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void ConditionalCheckIsCreated<T, U>(this NativeParallelHashMap<T, U> set)
            where T : unmanaged, IEquatable<T>
            where U : unmanaged
        {
            if (!set.IsCreated)
                throw new InvalidOperationException("NativeParallelHashMap is not created.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void ConditionalCheckIsCreated<T, U>(this UnsafeParallelHashMap<T, U> set)
            where T : unmanaged, IEquatable<T>
            where U : unmanaged
        {
            if (!set.IsCreated)
                throw new InvalidOperationException("UnsafeParallelHashMap is not created.");
        }

        #endregion
        
        #region Centralized implementations for IVLibUnsafeContainer interfaces
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureCapacity<TCollection>(ref this TCollection collection, int capacity)
            where TCollection : unmanaged, IVLibUnsafeContainer
        {
            if (collection.Capacity < capacity)
                collection.Capacity = capacity;
        }

        /// <summary> Take the collections pointer as type </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe T* GetUnsafeTypedPtr<TCollection, T>(this TCollection collection)
            where TCollection : IVLibUnsafeContainerReadOnly
            where T : unmanaged
        {
            return (T*) collection.GetUnsafePtr();
        }

        /// <summary> Creates an unsafe view of the collection that does not need to be disposed. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe UnsafeList<T> AsUnsafeList<TCollection, T>(this TCollection collection)
            where TCollection : IVLibUnsafeContainerReadOnly
            where T : unmanaged
        {
            return new UnsafeList<T>((T*) collection.GetUnsafePtr(), collection.Length);
        }
        
        public static unsafe int BinarySearch<TCollection, T>(this TCollection collection, T value)
            where TCollection : IVLibUnsafeContainerReadOnly
            where T : unmanaged, IComparable<T>
        {
            collection.ConditionalAssertIsCreated();
            return NativeSortExtension.BinarySearch(collection.GetUnsafeTypedPtr<TCollection, T>(), collection.Length, value);
        }
        
        #region CHECKS

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void ConditionalAssertIsCreated<TCollection>(this TCollection collection)
            where TCollection : IVLibUnsafeContainerReadOnly
        {
            if (!collection.IsCreated)
                throw new InvalidOperationException("The collection has not been created.");
        }

        #endregion
        
        #region COPYING

        /// <summary>
        /// Returns an array containing a copy of this list's content.
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array containing a copy of this list's content.</returns>
        internal static unsafe NativeArray<T> ToArray<TCollection, T>(TCollection collection, AllocatorManager.AllocatorHandle allocator)
            where TCollection : IVLibUnsafeContainerReadOnly
            where T : unmanaged
        {
            collection.ConditionalAssertIsCreated();
            var length = collection.Length;
            NativeArray<T> result = CollectionHelper.CreateNativeArray<T>(length, allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeUtility.MemCpy((byte*) result.ForceGetUnsafePtrNOSAFETY(), (byte*) collection.GetUnsafePtr(), length * UnsafeUtility.SizeOf<T>());
            return result;
        }

        /// <summary> Creates and fast-copies your native collection to a managed array </summary>
        internal static unsafe T[] ToManagedArray<TCollection, T>(TCollection collection)
            where TCollection : IVLibUnsafeContainerReadOnly
            where T : unmanaged
        {
            if (!collection.IsCreated || collection.Length == 0)
                return Array.Empty<T>();
            
            var length = collection.Length;
            var array = new T[length];
            array.PinArrayCopyIn(0, (T*)collection.GetUnsafePtr(), 0, length);
            return array;
        }

        /// <summary> Performs an additive copy. </summary>
        internal static void AddRange<TCollection, T>(TCollection source, TCollection dest, int sourceStart = 0, int count = 0)
            where TCollection : IVLibUnsafeContainer
            where T : unmanaged
        {
            source.ConditionalAssertIsCreated();
            dest.ConditionalAssertIsCreated();
            
            if (count < 1)
                count = source.Length - sourceStart;
            ConditionalCheckRangeValid(sourceStart, count, source.Length);
            
            var destStart = dest.Length;
            dest.Length += count;
            CopyToAsArray(source, dest.AsUnsafeList<TCollection, T>(), sourceStart, destStart, count);
        }

        /// <summary>
        /// Copies all elements of specified container to this container additively.
        /// </summary>
        /// <param name="source">An container to copy into this container.</param>
        internal static void AddRange<TCollection, T>(in UnsafeList<T> source, TCollection dest)
            where TCollection : IVLibUnsafeContainer
            where T : unmanaged
        {
            source.ConditionalCheckIsCreated();
            dest.ConditionalAssertIsCreated();
            
            var currentLength = dest.Length;
            dest.Length += source.Length;
            CopyFromAsArray(dest, source, 0, currentLength, source.Length);
        }

        internal static void CopyFromTo<TCollection, T>(in UnsafeList<T> source, TCollection dest, int sourceStart = 0, int count = 0)
            where TCollection : IVLibUnsafeContainer
            where T : unmanaged
        {
            source.ConditionalCheckIsCreated();
            dest.ConditionalAssertIsCreated();
            
            if (count < 1)
                count = source.Length - sourceStart;
            ConditionalCheckRangeValid(sourceStart, count, source.Length);
            
            dest.Length = count;
            CopyFromAsArray(dest, source, sourceStart, 0, count);
        }

        internal static unsafe void CopyFromTo<TCollection, T>(TCollection source, T[] dest, int sourceStart = 0, int destStart = 0, int count = 0)
            where TCollection : IVLibUnsafeContainerReadOnly
            where T : unmanaged
        {
            source.ConditionalAssertIsCreated();
            if (count < 1)
                count = source.Length - sourceStart;
            ConditionalCheckRangeValid(sourceStart, count, source.Length);

            var sourcePtr = source.GetUnsafeTypedPtr<TCollection, T>();
            
            // Rely on internal checks to guard the dest
            dest.PinArrayCopyIn(sourceStart, sourcePtr, destStart, count);
        }

        /// <summary> Treats the list like an array. Length MUST accommodate the copy range. </summary>
        public static unsafe void CopyToAsArray<TCollection, T>(TCollection source, in UnsafeList<T> dest, int sourceStart = 0, int destStart = 0, int count = 0)
            where TCollection : IVLibUnsafeContainerReadOnly
            where T : unmanaged
        {
            if (count <= 0)
                count = source.Length - sourceStart;
            
            // Source
            source.ConditionalAssertIsCreated();
            ConditionalCheckRangeValid(sourceStart, count, source.Length);
            // Dest
            dest.ConditionalCheckIsCreated();
            ConditionalCheckRangeValid(destStart, count, dest.Length);
            
            MemcpyTyped((T*)source.GetUnsafePtr(), sourceStart, dest.Ptr, destStart, count);
        }
        
        /// <summary> Treats the list like an array. Length MUST accommodate the copy range. </summary>
        public static unsafe void CopyFromAsArray<TCollection, T>(TCollection dest, in UnsafeList<T> source, int sourceStart = 0, int destStart = 0, int count = 0)
            where TCollection : IVLibUnsafeContainerReadOnly
            where T : unmanaged
        {
            if (count <= 0)
                count = source.Length - sourceStart;
            
            // Source
            source.ConditionalCheckIsCreated();
            ConditionalCheckRangeValid(sourceStart, count, source.Length);
            // Dest
            dest.ConditionalAssertIsCreated();
            ConditionalCheckRangeValid(destStart, count, dest.Length);
            
            MemcpyTyped(source.Ptr, sourceStart, (T*) dest.GetUnsafePtr(), destStart, count);
        }

        #endregion
        
        #endregion
    }
}