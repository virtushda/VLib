using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Libraries.VLib.Unsafe.Extensions.Collections
{
    public static unsafe class NativeCollectionsInternalExt
    {
        /// <summary> Returns a native array that aliases the content of this list, but uses the capacity instead of the length. </summary> 
        public static NativeArray<T> AsArrayCapacity<T>(this NativeList<T> list)
            where T : unmanaged
        {
            var listBufferPtr = list.GetUnsafePtr(); // This is not redundant, it performs safety checks in editor
            return AsArrayCapacityInternal(list, listBufferPtr, list.Capacity);
        }
        
        public static NativeArray<T> AsArrayCapacityReadonly<T>(this NativeList<T> list)
            where T : unmanaged
        {
            var listBufferPtr = list.GetUnsafeReadOnlyPtr(); // This is not redundant, it performs safety checks in editor
            return AsArrayCapacityInternal(list, listBufferPtr, list.Capacity);
        }
        
        public static NativeArray<T> AsArrayCustomReadonly<T>(this NativeList<T> list, int size)
            where T : unmanaged
        {
            var listBufferPtr = list.GetUnsafeReadOnlyPtr(); // This is not redundant, it performs safety checks in editor
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG           
            if (size < 0 || size > list.Capacity)
            {
                Debug.LogError($"Size {size} is invalid for list with capacity {list.Capacity}");
                size = math.clamp(size, 0, list.Capacity);
            }
#endif
            return AsArrayCapacityInternal(list, listBufferPtr, size);
        }

        // Copied from nativearray.cs
        static NativeArray<T> AsArrayCapacityInternal<T>(NativeList<T> list, T* listBufferPtr, int size)
            where T : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS         
            AtomicSafetyHandle.CheckGetSecondaryDataPointerAndThrow(list.m_Safety);
            var arraySafety = list.m_Safety;
            AtomicSafetyHandle.UseSecondaryVersion(ref arraySafety);
#endif
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(listBufferPtr, size, Allocator.None);

#if ENABLE_UNITY_COLLECTIONS_CHECKS          
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, arraySafety);
#endif
            return array;
        }

        ///<summary> You must call <see cref="ManualReleaseSafetyHandle{T}"/> on the returned native array, or you will anger the dimwitted Unity gods!!!!!! </summary>
        public static NativeArray<T> AsArrayCustomReadonly<T>(this UnsafeList<T> list, int size = -1)
            where T : unmanaged
        {
            if (size < 1)
                size = list.Length;
            if (size > list.Capacity)
            {
                Debug.LogError($"Size {size} is invalid for list with capacity {list.Capacity}");
                size = math.clamp(size, 0, list.Capacity);
            }
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(list.Ptr, size, Allocator.None);
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS          
            // Create a new safety handle for the array
            var safety = AtomicSafetyHandle.Create();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, safety);
#endif
            return array;
        }

        public static void ManualReleaseSafetyHandle<T>(this ref NativeArray<T> array)
            where T : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS           
            AtomicSafetyHandle.Release(array.m_Safety);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, default);
#endif
        }
    }
}