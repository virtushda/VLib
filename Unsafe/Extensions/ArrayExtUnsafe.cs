using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace VLib
{
    public static unsafe class ArrayExtUnsafe
    {
        public readonly struct NativeViewBaggage : IDisposable
        {
            public readonly bool allocated;
            public readonly ulong gcHandle;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            public readonly AtomicSafetyHandle safetyHandle;
#endif

            public NativeViewBaggage(ulong gcHandle
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                                     , AtomicSafetyHandle safetyHandle
#endif
            )
            {
                allocated = true;
                this.gcHandle = gcHandle;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                this.safetyHandle = safetyHandle;
#endif
            }

            public void Dispose()
            {
                if (!allocated)
                    return;
                
                if (gcHandle != 0)
                    UnsafeUtility.ReleaseGCObject(gcHandle);
                else
                    Debug.LogError("GC Handle is 0!");
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.Release(safetyHandle);
#endif
            }
        }

        /// <summary>
        /// Creates a native 'view' into managed memory without any copies. Be WARNED, this can be very dangerous.
        /// Baggage struct must be 'disposed'!
        /// </summary>
        /// <typeparam name="T">Must be native-compatible!</typeparam>
        public static NativeArray<T> GetNativeView<T>(this T[] array, out NativeViewBaggage baggage, int length = -1)
            where T : struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (array == null)
                throw new UnityException("Array is null!");
            if (array.Length < 1)
                throw new UnityException("Array is empty!");
            if (length > array.Length)
            {
                length = array.Length;
                Debug.LogError($"Length '{length}' is larger than array length '{array.Length}'! Auto-fix in editor only!");
            }
#endif
            
            //AtomicSafetyHandle.CheckGetSecondaryDataPointerAndThrow();
            
            if (length < 0)
                length = array.Length;
            //Get ptr to array
            var matrixArrayPtr = UnsafeUtility.PinGCArrayAndGetDataAddress(array, out var gcHandle);
            //Create View
            var arrayView = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(matrixArrayPtr, length, Allocator.Invalid);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safety = AtomicSafetyHandle.Create();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arrayView, safety);
            baggage = new NativeViewBaggage(gcHandle, safety);
#else
            baggage = new NativeViewBaggage(gcHandle
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                                            , default
#endif
                                            );
#endif
            return arrayView;
        }
        
        /// <summary>
        /// Creates an unsafe 'view' into managed memory without any copies. Be WARNED, this can be very dangerous.
        /// Baggage struct must be 'disposed'!
        /// </summary>
        /// <typeparam name="T">Must be native-compatible!</typeparam>
        public static UnsafeView<T> GetUnsafeView<T>(this T[] array, int length = -1)
            where T : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (array == null)
                throw new UnityException("Array is null!");
            if (array.Length < 1)
                throw new UnityException("Array is empty!");
            if (length > array.Length)
            {
                length = array.Length;
                Debug.LogError($"Length '{length}' is larger than array length '{array.Length}'! Auto-fix in editor only!");
            }
#endif
            if (length < 0)
                length = array.Length;
            
            return new UnsafeView<T>(array, length);
        }

        public readonly struct UnsafeView<T> : IDisposable where T : unmanaged
        {
            public readonly UnsafeList<T> list;
            private readonly ulong gcHandle;
            private readonly bool allocated;

            public UnsafeView(T[] array, int length)
            {
                var matrixArrayPtr = (T*)UnsafeUtility.PinGCArrayAndGetDataAddress(array, out gcHandle);
                list = new UnsafeList<T>(matrixArrayPtr, length);
                allocated = true;
            }

            public void Dispose()
            {
                if (!allocated) return;
                UnsafeUtility.ReleaseGCObject(gcHandle);
            }
        }
    }
}