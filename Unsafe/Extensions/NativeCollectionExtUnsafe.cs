#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define SAFETY
//#define CORRUPTION_CHECKS
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using VLib.Unsafe.Utility;

namespace VLib
{
    public static class UnsafeListReadonlyExtensions
    {
        public static bool IndexValid<T>(this UnsafeList<T>.ReadOnly list, int index)
            where T : unmanaged
        {
            return index >= 0 && index < list.Length;
        }

        /// <summary> "Safe", will throw an exception if list is not created or index out of range. </summary>
        public static unsafe T ReadAtIndex<T>(this UnsafeList<T>.ReadOnly list, int index)
            where T : unmanaged
        {
            if (list.Ptr == null)
                throw new NullReferenceException("UnsafeList is not created!");
            if (!list.IndexValid(index))
                throw new IndexOutOfRangeException($"Index '{index}' is invalid on UnsafeList with length {list.Length}");
            return list.Ptr[index];
        }
    }
    
    public static unsafe class NativeCollectionExtUnsafe
    {
        public static Span<T> LengthAsSpan<T>(this UnsafeList<T> list)
            where T : unmanaged
        {
            list.ConditionalCheckIsCreated();
            return new Span<T>(list.Ptr, list.Length);
        }
        
        public static long MemoryFootprintBytes<T>(this NativeArray<T> array) 
            where T : struct =>
            array.IsCreated ? UnsafeUtility.SizeOf<T>() * array.Length : 0;

        /*public static void CopyToUnsafe<T>(this NativeArray<T> src, int srcIndex, T[] dst, int dstIndex, int length)
            where T : unmanaged
        {
            if (dst == null)
                throw new ArgumentNullException(nameof(dst));
            GCHandle gcHandle = GCHandle.Alloc(dst, GCHandleType.Pinned);
            void* srcPtr = src.GetUnsafeReadOnlyPtr();
            UnsafeUtility.MemCpy((void*)((IntPtr)(void*)gcHandle.AddrOfPinnedObject() + dstIndex * UnsafeUtility.SizeOf<T>()),
                                 (void*)((IntPtr)srcPtr + srcIndex * UnsafeUtility.SizeOf<T>()),
                                 length * UnsafeUtility.SizeOf<T>());
            gcHandle.Free();
        }*/

        public static void CopyTo<T>(in this UnsafeList<T> src, int srcIndex, UnsafeList<T> dst, int dstIndex, int length)
            where T : unmanaged
        {
            src.ConditionalCheckIsCreated();
            dst.ConditionalCheckIsCreated();
            VCollectionUtils.ConditionalCheckRangeValid(srcIndex, length, src.Length);
            VCollectionUtils.ConditionalCheckRangeValid(dstIndex, length, dst.Length);
            
            VCollectionUtils.MemcpyTyped(src.Ptr, srcIndex, dst.Ptr, dstIndex, length);
        }

        public static void CopyToUnsafe<T>(in this UnsafeList<T> src, int srcIndex, GCHandle dstHandlePinnedType, int dstIndex, int length)
            where T : unmanaged
        {
            if (!dstHandlePinnedType.IsAllocated)
            {
                UnityEngine.Debug.LogError("Safety Exception: GCHandle is not allocated!");
                return;
            }

            BurstAssert.True(length > 0);
            BurstAssert.True(src.IsCreated);
            BurstAssert.True(srcIndex >= 0 && srcIndex < src.Length);
            BurstAssert.True(dstIndex >= 0);
            BurstAssert.True(src.Length >= srcIndex + length);
            
            VCollectionUtils.MemcpyTyped(src.Ptr, srcIndex, (T*) dstHandlePinnedType.AddrOfPinnedObject(), dstIndex, length);
        }

        public static void CopyToUnsafe<T>(in this UnsafeList<T> src, int srcIndex, T[] dstArray, int dstIndex, int length)
            where T : unmanaged
        {
            if (dstArray == null)
            {
                UnityEngine.Debug.LogError("Safety Exception: Array is null!");
                return;
            }

            BurstAssert.True(length > 0);
            BurstAssert.True(src.IsCreated);
            VCollectionUtils.ConditionalCheckRangeValid(srcIndex, length, src.Length);
            VCollectionUtils.ConditionalCheckRangeValid(dstIndex, length, dstArray.Length);
            
            fixed (T* targetArrayPtr = dstArray)
            {
                VCollectionUtils.MemcpyTyped(src.Ptr, srcIndex, targetArrayPtr, dstIndex, length);
            }
        }

        public static void CopyToUnsafe<T>(in this NativeArray<T> src, int srcIndex, ref UnsafeList<T> dst, int dstIndex, int length)
            where T : unmanaged
        {
            src.ConditionalCheckIsCreated();
            dst.ConditionalCheckIsCreated();

            //Ensure Cap
            int previousListLength = dst.Length;
            int requiredCap = dstIndex + length;
            if (dst.Capacity < requiredCap)
                dst.Resize(requiredCap, NativeArrayOptions.ClearMemory);
            
            VCollectionUtils.ConditionalCheckRangeValid(srcIndex, length, src.Length);
            VCollectionUtils.ConditionalCheckRangeValid(dstIndex, length, dst.Length);

            VCollectionUtils.MemcpyTyped((T*) src.GetUnsafeReadOnlyPtr(), srcIndex, dst.Ptr, dstIndex, length);
            /*void* srcPtr = ;
            UnsafeUtility.MemCpy((void*)((IntPtr)dst.Ptr + dstIndex * UnsafeUtility.SizeOf<T>()),
                                 (void*)((IntPtr)srcPtr + srcIndex * UnsafeUtility.SizeOf<T>()),
                                 length * UnsafeUtility.SizeOf<T>());*/

            dst.Length = math.max(previousListLength, requiredCap);
        }
        
        /*/// <summary>
        /// Clears this list and then copies all the elements of an array to this list.
        /// </summary>
        /// <typeparam name="T">The type of elements.</typeparam>
        /// <param name="list">This list.</param>
        /// <param name="array">The managed array to copy from.</param>
        [ExcludeFromBurstCompatTesting("Takes managed array")]
        public static void CopyFromNBC<T>(this VUnsafeList<T> list, T[] array)
            where T : unmanaged
        {
            list.ConditionalCheckIsCreated();
            list.Clear();
            list.Resize(array.Length, NativeArrayOptions.UninitializedMemory);
            array.PinArrayCopyOut(0, list.listData->Ptr, 0, array.Length);
        }*/

        /// <summary> Stops a managed array of blittable types from moving in memory, then memcpys the desired buffer range directly. </summary>
        [ExcludeFromBurstCompatTesting("Takes managed array")]
        public static void PinArrayCopyOut<T>(this T[] source,
            int sourceIndex,
            T* dest,
            int destIndex,
            int length)
            where T : unmanaged
        {
            if (source.Length < sourceIndex + length)
                throw new ArgumentException("Source array is smaller than the length of the copy!");
            if (dest == null)
                throw new ArgumentNullException(nameof(dest));
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (sourceIndex < 0)
                throw new ArgumentOutOfRangeException($"Source index '{sourceIndex}' is less than 0!");
            if (destIndex < 0)
                throw new ArgumentOutOfRangeException($"Destination index '{destIndex}' is less than 0!");
            
            GCHandle gcHandle = GCHandle.Alloc((object) source, GCHandleType.Pinned);
            IntPtr num = gcHandle.AddrOfPinnedObject();
            UnsafeUtility.MemCpy(
                (void*) ((IntPtr) dest + destIndex * UnsafeUtility.SizeOf<T>()), 
                (void*) ((IntPtr) (void*) num + sourceIndex * UnsafeUtility.SizeOf<T>()), 
                (long) (length * UnsafeUtility.SizeOf<T>()));
            gcHandle.Free();
        }

        /// <summary> Stops a managed array of blittable types from moving in memory, then memcpys the desired buffer range directly. </summary>
        [ExcludeFromBurstCompatTesting("Takes managed array")]
        public static void PinArrayCopyIn<T>(this T[] dest,
            int sourceIndex,
            T* source,
            int destIndex,
            int length)
            where T : unmanaged
        {
            if (dest == null)
                throw new ArgumentNullException(nameof(dest));
            VCollectionUtils.ConditionalCheckRangeValid(destIndex, length, dest.Length);
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (sourceIndex < 0)
                throw new ArgumentOutOfRangeException($"Source index '{sourceIndex}' is less than 0!");
            
            fixed (T* targetPtr = dest)
            {
                VCollectionUtils.MemcpyTyped(source, sourceIndex, targetPtr, destIndex, length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* ForceGetUnsafePtrNOSAFETY<T>(this NativeArray<T> array) 
            where T : struct =>
            array.IsCreated ? NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(array) : throw new NullReferenceException("Array is not created!");

        public static ref T ElementAt<T>(this NativeArray<T> array, int index)
            where T : struct
        {
            array.ConditionalCheckIsCreated();
            VCollectionUtils.ConditionalCheckIndexValid(index, array.Length);
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafePtr(), index);
        }

        /// <summary> <see cref="ElementAt{T}"/>, but the ref is readonly. Will error if you don't have 'read' access according to Unity's safety system. </summary>
        public static ref readonly T ElementAtReadOnly<T>(this NativeArray<T> array, int index)
            where T : struct
        {
            return ref ElementAtReadOnlyUnsafe(array, index);
        }

        /// <summary> <see cref="ElementAtReadOnly{T}"/>, but returns a writable ref.
        /// This is a bit of a codesmell, but that's more Unity's fault for making their native safety managed.  </summary>
        public static ref T ElementAtReadOnlyUnsafe<T>(this NativeArray<T> array, int index)
            where T : struct
        {
            array.ConditionalCheckIsCreated();
            VCollectionUtils.ConditionalCheckIndexValid(index, array.Length);
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafeReadOnlyPtr(), index);
        }

        /// <summary> <inheritdoc cref="ElementAtReadOnlyUnsafe{T}(Unity.Collections.NativeArray{T},int)"/> </summary>
        public static ref T ElementAtReadOnlyUnsafe<T>(this NativeArray<T>.ReadOnly array, int index)
            where T : struct
        {
            array.ConditionalCheckIsCreated();
            VCollectionUtils.ConditionalCheckIndexValid(index, array.Length);
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafeReadOnlyPtr(), index);
        }

        public static ref T ElementAtNoChecksUNSAFE<T>(this NativeArray<T> array, int index)
            where T : struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG           
            array.ConditionalCheckIsCreated();
            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException("Safety Exception: " + nameof(index));
#endif
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.ForceGetUnsafePtrNOSAFETY(), index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ReadUnsafe<T>(in this NativeArray<T> array, int index)
            where T : unmanaged
        {
#if SAFETY
            var arrayIsCreated = array.IsCreated;
            if (Hint.Unlikely(!arrayIsCreated)) //!arrayIsCreated)
                UnityEngine.Debug.LogError("Safety Exception: Array is not created!");
            
            var indexIsValid = (uint)index < array.Length;
            if (Hint.Unlikely(!indexIsValid))
                UnityEngine.Debug.LogError($"Safety Exception: Index '{index}' is not valid within array of length '{array.Length}'");
#endif
            return UnsafeUtility.ReadArrayElement<T>(array.ForceGetUnsafePtrNOSAFETY(), index);
        }
        //((T*)array.ForceGetUnsafePtrNOSAFETY())[index];
        
        /// <summary> Read from a nativearray bypassing the safety system that's active in the editor only (by default) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ReadBypassSafety<T>(in this NativeArray<T> array, int index)
            where T : unmanaged
        {
#if SAFETY
            array.ConditionalCheckIsCreated();
            if (index >= array.Length)
                throw new IndexOutOfRangeException($"Safety Exception: Index '{index}' is >= Length '{array.Length}'");
            return array.ReadUnsafe(index);
#else
            return array[index]; //Safety only exists in editor... great feature... but so annoying sometimes!
#endif
        }

        /// <summary> Write to a nativearray bypassing the safety system that's active in the editor only (by default) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBypassSafety<T>(ref this NativeArray<T> array, int index, T value)
            where T : unmanaged
        {
#if SAFETY
            array.ConditionalCheckIsCreated();
            if (index < 0)
                throw new IndexOutOfRangeException($"Safety Exception: Index '{index}' is < 0");
            if (index >= array.Length)
                throw new IndexOutOfRangeException($"Safety Exception: Index '{index}' is >= Length '{array.Length}'");
            ((T*)array.ForceGetUnsafePtrNOSAFETY())[index] = value;
#else
            array[index] = value; //Safety only exists in editor... great feature... but so annoying sometimes!
#endif
        }

        public static ref T TryGetRef<T>(this NativeArray<T> nativeArray, int index, out bool success)
            where T : unmanaged
        {
            nativeArray.ConditionalCheckIsCreated();
            var isValidIndex = nativeArray.IsValidIndex(index);
            success = isValidIndex;
            return ref isValidIndex ? ref nativeArray.ElementAt(index) : ref VUnsafeUtil.NullRef<T>();
        }
        
        public static ref T TryGetRefReadOnly<T>(this NativeArray<T> nativeArray, int index, out bool success)
            where T : unmanaged
        {
            nativeArray.ConditionalCheckIsCreated();
            var isValidIndex = nativeArray.IsValidIndex(index);
            success = isValidIndex;
            return ref isValidIndex ? ref nativeArray.ElementAtReadOnlyUnsafe(index) : ref UnsafeUtility.AsRef<T>(null);
        }
        
        public static ref T TryGetRefNoSafety<T>(this NativeArray<T> nativeArray, int index, out bool success)
            where T : unmanaged
        {
            nativeArray.ConditionalCheckIsCreated();
            var isValidIndex = nativeArray.IsValidIndex(index);
            success = isValidIndex;
            return ref isValidIndex ? ref nativeArray.ElementAtNoChecksUNSAFE(index) : ref UnsafeUtility.AsRef<T>(null);
        }
        
        public static bool TryGetElementPtr<T>(this NativeArray<T> nativeArray, int index, out T* valuePtr)
            where T : unmanaged
        {
            nativeArray.ConditionalCheckIsCreated();
            var isValidIndex = nativeArray.IsValidIndex(index);
            valuePtr = isValidIndex ? nativeArray.GetArrayRefElementPtr(index) : (T*) IntPtr.Zero;
            return isValidIndex;
        }
        
        public static bool TryGetElementPtrReadOnly<T>(this NativeArray<T> nativeArray, int index, out T* valuePtr)
            where T : unmanaged
        {
            nativeArray.ConditionalCheckIsCreated();
            var isValidIndex = nativeArray.IsValidIndex(index);
            valuePtr = isValidIndex ? nativeArray.GetArrayElementPtrReadOnly(index) : default;
            return isValidIndex;
        }
        
        public static bool TryGetElementPtrNoSafety<T>(this NativeArray<T> nativeArray, int index, out T* valuePtr)
            where T : unmanaged
        {
            nativeArray.ConditionalCheckIsCreated();
            var isValidIndex = nativeArray.IsValidIndex(index);
            valuePtr = isValidIndex ? nativeArray.GetArrayElementPtrNoSafety(index) : default;
            return isValidIndex;
        }

        public static T* GetArrayElementPtr<T>(in this NativeArray<T> array, int index)
            where T : unmanaged
        {
            // Detect this stuff in editor mode
#if SAFETY
            if (!array.IsCreated)
                UnityEngine.Debug.LogError("Safety Exception: Array is not created!");
            if (!array.IsValidIndex(index))
                UnityEngine.Debug.LogError($"Safety Exception: Index '{index}' is >= Length '{array.Length}'");
#endif
            return ((T*) array.GetUnsafePtr()) + index;
        }

        public static T* GetArrayRefElementPtr<T>(ref this NativeArray<T> array, int index)
            where T : unmanaged
        {
            // Detect this stuff in editor mode
#if SAFETY
            if (!array.IsCreated)
                UnityEngine.Debug.LogError("Safety Exception: Array is not created!");
            if (!array.IsValidIndex(index))
                UnityEngine.Debug.LogError($"Safety Exception: Index '{index}' is >= Length '{array.Length}'");
#endif
            return ((T*) array.GetUnsafePtr()) + index;
        }

        public static T* GetArrayElementPtrReadOnly<T>(ref this NativeArray<T> array, int index)
            where T : unmanaged
        {
            // Detect this stuff in editor mode
#if SAFETY
            if (!array.IsCreated)
                UnityEngine.Debug.LogError("Safety Exception: Array is not created!");
            if (!array.IsValidIndex(index))
                UnityEngine.Debug.LogError($"Safety Exception: Index '{index}' is >= Length '{array.Length}'");
#endif
            return ((T*) array.GetUnsafeReadOnlyPtr()) + index;
        }
        //(T*) ((IntPtr) array.GetUnsafeReadOnlyPtr() + index * sizeof(T));
        
        public static T* GetArrayElementPtrNoSafety<T>(ref this NativeArray<T> array, int index)
            where T : unmanaged
        {
            // Detect this stuff in editor mode
#if SAFETY
            if (!array.IsCreated)
                UnityEngine.Debug.LogError("Safety Exception: Array is not created!");
            if (!array.IsValidIndex(index))
                UnityEngine.Debug.LogError($"Safety Exception: Index '{index}' is >= Length '{array.Length}'");
#endif
            return ((T*) array.ForceGetUnsafePtrNOSAFETY()) + index;
        }
        //(T*) ((IntPtr) array.ForceGetUnsafePtrNOSAFETY() + index * sizeof(T));

        /// <summary>WARNING: Very unsafe! Do not dispose returned list or hold onto it outside very tight scopes! This is a view into a nativearray buffer.</summary>
        public static UnsafeList<T> AsUnsafeList_UNSAFE<T>(in this NativeArray<T> array, NativeSafety safety = NativeSafety.ReadWrite) 
            where T : unmanaged
        {
            void* bufferVoidPtr = null;
            if (!array.IsCreated)
                throw new ArgumentException("Safety Exception: NativeArray is not created!");
            if (safety == NativeSafety.ReadWrite)
                bufferVoidPtr = array.GetUnsafePtr();
            else if (safety == NativeSafety.ReadOnly)
                bufferVoidPtr = array.GetUnsafeReadOnlyPtr();
            else
                bufferVoidPtr = array.ForceGetUnsafePtrNOSAFETY();
            
            T* bufferPtr = (T*) bufferVoidPtr;
            return new UnsafeList<T>(bufferPtr, array.Length);
        }
        
        public static NativeArray<T> AsNativeArray<T>(in this UnsafeList<T> list) 
            where T : unmanaged
        {
            if (!list.IsCreated)
                throw new ArgumentException("Safety Exception: UnsafeList is not created!");
            return NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(list.Ptr, list.Length, Allocator.TempJob);
        }

        /*/// <summary>Does not dispose the passed nativearray!</summary>
        public static void DisposeElements<T>(this NativeArray<T> array, int start = 0, int count = -1)
            where T : struct, IDisposable
        {
            if (!array.IsCreated)
                return;
            if (count < 0)
                count = array.Length;
            
            for (int i = start; i < count; i++) 
                array.ElementAt(i).Dispose();
        }*/

        public static void DisposeElements<T>(ref this NativeArray<T> array, bool disposeSelf, int start = 0, int count = -1)
            where T : struct, IDisposable
        {
            if (!array.IsCreated)
                return;
            if (count < 0)
                count = array.Length;

            for (int i = start; i < count; i++)
            {
                array[i].Dispose();
            }

            if (disposeSelf)
                array.Dispose();
        }

        public static JobHandle DisposeElementsOnComplete<T>(ref this NativeArray<T> array, JobHandle inputDeps, bool disposeSelf, int start = 0, int count = -1)
            where T : struct, INativeDisposable
        {
            if (count < 0)
                count = array.Length;

            JobHandle disposeHandle = inputDeps;
            for (int i = start; i < count; i++)
                JobHandle.CombineDependencies(disposeHandle, array[i].Dispose(inputDeps));

            if (disposeSelf)
                return array.Dispose(disposeHandle);
            return disposeHandle;
        }

        /// <summary> Dispose a disposable at index in a native array and clear the memory behind it. </summary>
        /// <returns> False if invalid index </returns>
        public static bool DisposeToDefault<T>(this NativeArray<T> array, int index)
            where T : struct, IDisposable
        {
            if (!array.IsValidIndex(index))
                return false;
            array[index].Dispose();
            array[index] = default;
            return true;
        }

        /// <summary> Good for when you want to clear the memory behind disposing a struct. BE CAREFUL that you actually have the right ref and you're not disposing a local copy! </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DisposeRefToDefault<T>(ref this T value)
            where T : struct, IDisposable
        {
            value.Dispose();
            value = default;
        }
        
        public static ref UnsafeList<T> GetUnsafeListRef<T>(this NativeList<T> list)
            where T : unmanaged
        {
            return ref UnsafeUtility.AsRef<UnsafeList<T>>(list.GetUnsafeList());
        }

        public static void IndexValidOrThrow<T>(this NativeList<T> list, int index) 
            where T : unmanaged
        {
            if (list.IsIndexValid(index))
                return;
            string created = (list.IsCreated ? "Created" : "Uncreated");
            string exceptionMsg = $"Index '{index}' invalid on {created} NativeList with length {list.Length}";
            throw new IndexOutOfRangeException(exceptionMsg);
        }

        public static bool IsIndexValid<T>(this NativeList<T> list, int index, bool logError = true) 
            where T : unmanaged
        {
#if CORRUPTION_CHECKS
            if (logError && !math.ispow2(list.Capacity))
                UnityEngine.Debug.LogError($"Capacity {list.Capacity} is not a power of 2!");
#endif
            return !(!list.IsCreated || index < 0 || index >= list.Length);
        }

        public static bool IsIndexValid<T>(in this UnsafeList<T> list, int index, bool logError = true) 
            where T : unmanaged
        {
#if CORRUPTION_CHECKS
            if (logError && !math.ispow2(list.Capacity))
                UnityEngine.Debug.LogError($"Capacity {list.Capacity} is not a power of 2!");
#endif
            return !(!list.IsCreated || (uint)index >= list.Length);
        }

        public static bool IsIndexValid<T>(this VUnsafeList<T> list, int index, bool logError = true) 
            where T : unmanaged
        {
#if CORRUPTION_CHECKS
            if (logError && !math.ispow2(list.Capacity))
                UnityEngine.Debug.LogError($"Capacity {list.Capacity} is not a power of 2!");
#endif
            return !(!list.IsCreated || index < 0 || index >= list.Length);
        }

        public static ref T GetRef<T>(ref this NativeList<T> list, int index, bool logError = true)
            where T : unmanaged
        {
#if CORRUPTION_CHECKS
            if (logError && !math.ispow2(list.Capacity))
                UnityEngine.Debug.LogError($"Capacity {list.Capacity} is not a power of 2!");
#endif
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            if (!list.IsCreated)
                UnityEngine.Debug.LogError("Safety Exception: List is not created!");
            // You might want to validate the index first, as the unsafe method won't do that.
            if (index < 0 || index >= list.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
#endif
            return ref UnsafeUtility.ArrayElementAsRef<T>(list.GetUnsafePtr(), index);
        }

        public static ref T ElementAtReadOnlyUnsafe<T>(ref this NativeList<T> list, int index, bool logError = true)
            where T : unmanaged
        {
#if CORRUPTION_CHECKS
            if (logError && !math.ispow2(list.Capacity))
                UnityEngine.Debug.LogError($"Capacity {list.Capacity} is not a power of 2!");
#endif
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG      
            if (!list.IsCreated)
                UnityEngine.Debug.LogError("Safety Exception: List is not created!");     
            // You might want to validate the index first, as the unsafe method won't do that.
            if (index < 0 || index >= list.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
#endif
            return ref UnsafeUtility.ArrayElementAsRef<T>(list.GetUnsafeReadOnlyPtr(), index);
        }
        
        public static ref T TryGetRef<T>(this NativeList<T> list, int index, out bool success)
            where T : unmanaged
        {
            return ref list.AsUnsafeList().TryGetRef(index, out success);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeList<T>* AsUnsafeListPtr<T>(this ref NativeList<T> list, bool logError = true)
            where T : unmanaged
        {
            // Detect this stuff in editor mode
#if CORRUPTION_CHECKS
            if (logError && !math.ispow2(list.Capacity))
                UnityEngine.Debug.LogError($"Capacity {list.Capacity} is not a power of 2!");
#endif
#if SAFETY
            if (!list.IsCreated)
                UnityEngine.Debug.LogError("Safety Exception: Array is not created!");
#endif
            return (UnsafeList<T>*) NativeListUnsafeUtility.GetInternalListDataPtrUnchecked(ref list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* ForceGetUnsafePtrNOSAFETY<T>(this ref NativeList<T> list, bool logError = true)
            where T : unmanaged
        {
            // Detect this stuff in editor mode#if CORRUPTION_CHECKS
#if CORRUPTION_CHECKS
            if (logError && !math.ispow2(list.Capacity))
                UnityEngine.Debug.LogError($"Capacity {list.Capacity} is not a power of 2!");
#endif
#if SAFETY
            if (!list.IsCreated)
                UnityEngine.Debug.LogError("Safety Exception: Array is not created!");
#endif
            return ((UnsafeList<T>*) NativeListUnsafeUtility.GetInternalListDataPtrUnchecked(ref list))->Ptr;
        }

        public static T* GetListElementPtr<T>(ref this NativeList<T> list, int index, bool logError = true)
            where T : unmanaged
        {
            // Detect this stuff in editor mode
#if CORRUPTION_CHECKS
            if (logError && !math.ispow2(list.Capacity))
                UnityEngine.Debug.LogError($"Capacity {list.Capacity} is not a power of 2!");
#endif
#if SAFETY
            if (!list.IsCreated)
                UnityEngine.Debug.LogError("Safety Exception: List is not created!");
            if (!list.IsIndexValid(index))
                UnityEngine.Debug.LogError($"Safety Exception: Index '{index}' invalid on NativeList with length {list.Length}");
#endif
            return list.GetUnsafePtr() + index;
        }

        /// <summary> Safety in editor only </summary>
        public static T* GetListElementPtr<T>(in this UnsafeList<T> list, int index, bool logError = true)
            where T : unmanaged
        {
            // Detect this stuff in editor mode
#if CORRUPTION_CHECKS
            if (logError && !math.ispow2(list.Capacity))
                UnityEngine.Debug.LogError($"Capacity {list.Capacity} is not a power of 2!");
#endif
#if SAFETY
            if (!list.IsCreated)
                UnityEngine.Debug.LogError("Safety Exception: List is not created!");
            if (!list.IsIndexValid(index, logError))
                UnityEngine.Debug.LogError($"Safety Exception: Array is not created or index {index} is invalid in UnsafeList with length {list.Length}");
#endif
            return list.Ptr + index;
        }

        public static T* GetListElementPtrReadOnly<T>(ref this NativeList<T> list, int index, bool logError = true)
            where T : unmanaged
        {
            // Detect this stuff in editor mode
#if CORRUPTION_CHECKS
            if (logError && !math.ispow2(list.Capacity))
                UnityEngine.Debug.LogError($"Capacity {list.Capacity} is not a power of 2!");
#endif
#if SAFETY
            if (!list.IsCreated)
                UnityEngine.Debug.LogError("Safety Exception: List is not created!");
            if (!list.IsIndexValid(index))
                UnityEngine.Debug.LogError($"Safety Exception: Index '{index}' invalid on NativeList with length {list.Length}");
#endif
            return list.GetUnsafeReadOnlyPtr() + index;
        }

        public static T* GetListElementPtrNoSafety<T>(ref this NativeList<T> list, int index, bool logError = true)
            where T : unmanaged
        {
            // Detect this stuff in editor mode
#if CORRUPTION_CHECKS
            if (logError && !math.ispow2(list.Capacity))
                UnityEngine.Debug.LogError($"Capacity {list.Capacity} is not a power of 2!");
#endif
#if SAFETY
            if (!list.IsCreated)
                UnityEngine.Debug.LogError("Safety Exception: List is not created!");
            if (!list.IsIndexValid(index))
                UnityEngine.Debug.LogError($"Safety Exception: Index '{index}' invalid on NativeList with length {list.Length}");
#endif
            return list.ForceGetUnsafePtrNOSAFETY() + index;
        }

        public static long MemoryFootprintBytes<T>(this NativeList<T> list, bool logError = true) 
            where T : unmanaged
        {
#if CORRUPTION_CHECKS
            if (logError && !math.ispow2(list.Capacity))
                UnityEngine.Debug.LogError($"Capacity {list.Capacity} is not a power of 2!");
#endif
            return list.IsCreated ? UnsafeUtility.SizeOf<T>() * list.Capacity : 0;
        }

        /// <summary> Untested </summary>
        [Obsolete("May work, but is untested!")]
        public static void Insert<T>(this ref UnsafeList<T> sortedList, int startIndex, NativeArray<T> valuesToAdd, bool logError = true)
            where T : unmanaged
        {
#if CORRUPTION_CHECKS
            if (logError && !math.ispow2(sortedList.Capacity))
                UnityEngine.Debug.LogError($"Capacity {sortedList.Capacity} is not a power of 2!");
#endif
            sortedList.ConditionalCheckIsCreated();
            //Make Rooom
            sortedList.InsertRangeWithBeginEnd(startIndex, startIndex + valuesToAdd.Length);
            valuesToAdd.ConditionalCheckIsCreated();
            //Copy Chunk
            valuesToAdd.CopyToUnsafe(0, ref sortedList, startIndex, valuesToAdd.Length);
            //Avoid UnsafeList -> NativeArray Mess
            //NativeArray<T>.Copy(valuesToAdd, 0, sortedList.AsArray().array, startIndex, valuesToAdd.Length);
        }

        /// <summary>
        /// Useful for a [hashmap -> index -> list of elements] pattern
        /// Is shortcut for hashmap.TryGet -> list[index]!
        /// </summary>
        [GenerateTestsForBurstCompatibility]
        public static bool TryGetRefInAligned<TKey, TVal>(this NativeParallelHashMap<TKey, int> hashMap, ref NativeList<TVal> list, TKey key, out TVal value, bool logError = true)
            where TKey : unmanaged, IEquatable<TKey>
            where TVal : unmanaged
        {
#if CORRUPTION_CHECKS
            if (logError && !math.ispow2(list.Capacity))
                UnityEngine.Debug.LogError($"Capacity {list.Capacity} is not a power of 2!");
#endif
            list.ConditionalCheckIsCreated();
            hashMap.ConditionalCheckIsCreated();
            
            if (hashMap.TryGetValue(key, out var index))
            {
                value = list.GetRef(index);
                return true;
            }

            value = default;
            return false;
        }

        /// <summary> Useful for a [hashmap -> index -> list of elements] pattern </summary>
        /// <param name="key">Key to Remove</param>
        /// <param name="lastKey">Key of the "last" value in the list. (Swapback)</param>
        [GenerateTestsForBurstCompatibility]
        public static void RemoveFromAligned<TKey, TVal>(ref this NativeParallelHashMap<TKey, int> hashMap, ref NativeList<TVal> list, TKey key, TKey lastKey, bool logError = true)
            where TKey : unmanaged, IEquatable<TKey>
            where TVal : unmanaged
        {
            var mapValue = hashMap[key];
            
#if CORRUPTION_CHECKS
            if (logError && !math.ispow2(list.Capacity))
                UnityEngine.Debug.LogError($"Capacity {list.Capacity} is not a power of 2!");
#endif
            
            hashMap.ConditionalCheckIsCreated();
            list.ConditionalCheckIsCreated();

            //Quick remove if last result
            if (mapValue == list.Length - 1)
            {
                hashMap.Remove(key);
                list.RemoveAt(mapValue);
            }
            else //Remove At, swap last value to removal index, remap da map
            {
                //Remove Current Key
                hashMap.Remove(key);
                list.RemoveAtSwapBack(mapValue);
                hashMap[lastKey] = mapValue;
            }
        }

        public static void Reverse<T>(ref this UnsafeList<T> list)
            where T : unmanaged
        {
            BurstAssert.True(list.IsCreated);
            int start = 0;
            int end = list.Length - 1;
            
            while (start < end)
            {
                (list[start], list[end]) = (list[end], list[start]);
                ++start;
                --end;
            }
        }
        
        public static long MemoryFootprintBytes<T>(this UnsafeList<T> list, bool logError = true) 
            where T : unmanaged
        {
#if CORRUPTION_CHECKS
            if (logError && list.Capacity != 0 && !math.ispow2(list.Capacity))
                UnityEngine.Debug.LogError($"Capacity {list.Capacity} is not a power of 2!");
#endif
            return UnsafeUtility.SizeOf<T>() * list.Capacity;
        }

        public static ref UnsafeList<T> AsUnsafeList<T>(this NativeList<T> list, bool logError = true)
            where T : unmanaged
        {
            if (!list.IsCreated)
                UnityEngine.Debug.LogError("Safety Exception: List is not created!");
#if CORRUPTION_CHECKS
            if (logError && !math.ispow2(list.Capacity))
                UnityEngine.Debug.LogError($"Capacity {list.Capacity} is not a power of 2!");
#endif
            return ref UnsafeUtility.AsRef<UnsafeList<T>>(list.GetUnsafeList());
        }
        
        /// <summary> Returns a reference to the element at a given index. </summary>
        /// <param name="index">The index to access. Must be in the range of [0..Length).</param>
        /// <returns>A reference to the element at the index.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T ElementAtReadOnly<T>(in this UnsafeList<T> list, int index)
            where T : unmanaged
        {
            VCollectionUtils.ConditionalCheckIndexValid(index, list.Length);
            return ref list.Ptr[index];
        }

        public static bool TryGet<T>(in this UnsafeList<T> list, int index, out T value, bool logError = true)
            where T : unmanaged
        {
#if CORRUPTION_CHECKS
            if (logError && !math.ispow2(list.Capacity))
                UnityEngine.Debug.LogError($"Capacity {list.Capacity} is not a power of 2!");
#endif
            if (list.IsIndexValid(index, logError))
            {
                value = list[index];
                return true;
            }

            value = default;
            return false;
        }
        
        public static ref T TryGetRef<T>(ref this UnsafeList<T> list, int index, out bool success)
            where T : unmanaged
        {
            success = list.IsIndexValid(index);
            return ref success ? ref list.ElementAt(index) : ref UnsafeUtility.AsRef<T>(null);
        }
        
        public static bool TryGetElementPtr<T>(in this UnsafeList<T> list, int index, out T* valuePtr)
            where T : unmanaged
        {
            var isValidIndex = list.IsIndexValid(index);
            valuePtr = isValidIndex ? list.GetListElementPtr(index) : (T*) IntPtr.Zero;
            return isValidIndex;
        }

        /// <summary> Will return 'default' on a 0-length list. </summary>
        public static T Pop<T>(ref this UnsafeList<T> list, bool logError = true)
            where T : unmanaged
        {
#if CORRUPTION_CHECKS
            if (logError && !math.ispow2(list.Capacity))
                UnityEngine.Debug.LogError($"Capacity {list.Capacity} is not a power of 2!");
#endif
            list.ConditionalCheckIsCreated();
            var length = list.Length;
            if (length > 0)
            {
                var lastIndex = length - 1;
                var element = list[lastIndex];
                list.RemoveAt(lastIndex);
                return element;
            }

            return default;
        }
        
        public static bool TryPop<T>(ref this UnsafeList<T> list , out T value, bool logError = true)
            where T : unmanaged
        {
            list.ConditionalCheckIsCreated();
            if (list.Length > 0)
            {
                value = list.Pop();
                return true;
            }
            value = default;
            return false;
        }
        
        /// <summary> Will return 'default' on a 0-length list. </summary>
        public static T Peek<T>(in this UnsafeList<T> list, bool logError = true)
            where T : unmanaged
        {
#if CORRUPTION_CHECKS
            if (logError && !math.ispow2(list.Capacity))
                UnityEngine.Debug.LogError($"Capacity {list.Capacity} is not a power of 2!");
#endif
            list.ConditionalCheckIsCreated();
            var length = list.Length;
            if (length > 0)
            {
                var lastIndex = length - 1;
                var element = list[lastIndex];
                return element;
            }

            return default;
        }

        public static bool PtrsValid<T>(ref this UnsafeList<T>.ParallelWriter parallelWriter)
            where T : unmanaged
        {
            if (!math.ispow2(parallelWriter.ListData->Capacity))
            {
                UnityEngine.Debug.LogError($"Capacity {parallelWriter.ListData->Capacity} is not a power of 2!");
                return false;
            }
            return parallelWriter.Ptr != null &&
                   parallelWriter.ListData != null &&
                   parallelWriter.ListData->IsCreated;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void CheckReinterpretLoadRange<T, U>(in this UnsafeList<T> list, int sourceIndex) 
            where T : unmanaged 
            where U : struct
        {
            list.ConditionalCheckIsCreated();
            long num1 = UnsafeUtility.SizeOf<T>();
            long num2 = UnsafeUtility.SizeOf<U>();
            long num3 = list.m_length * num1;
            long num4 = sourceIndex * num1;
            long num5 = num4 + num2;
            if (num4 < 0L || num5 > num3)
                throw new ArgumentOutOfRangeException(nameof (sourceIndex), "loaded byte range must fall inside container bounds");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void CheckReinterpretStoreRange<T, U>(in this UnsafeList<T> list, int destIndex)
            where T : unmanaged
            where U : struct
        {
            list.ConditionalCheckIsCreated();
            long num1 = UnsafeUtility.SizeOf<T>();
            long num2 = UnsafeUtility.SizeOf<U>();
            long num3 = list.m_length * num1;
            long num4 = destIndex * num1;
            long num5 = num4 + num2;
            if (num4 < 0L || num5 > num3)
                throw new ArgumentOutOfRangeException(nameof (destIndex), "stored byte range must fall inside container bounds");
        }

        public static U ReinterpretLoad<T, U>(in this UnsafeList<T> list, int sourceIndex) where U : struct where T : unmanaged
        {
            list.CheckReinterpretLoadRange<T, U>(sourceIndex);
            return ReinterpretLoadUnsafe<T, U>(list, sourceIndex);
        }

        static U ReinterpretLoadUnsafe<T, U>(in this UnsafeList<T> list, int sourceIndex, bool logError = true) 
            where U : struct where T : unmanaged
        {
#if CORRUPTION_CHECKS
            if (logError && !math.ispow2(list.Capacity))
                UnityEngine.Debug.LogError($"Capacity {list.Capacity} is not a power of 2!");
#endif
            return UnsafeUtility.ReadArrayElement<U>(list.Ptr + sourceIndex, 0);
        }

        /// <summary> Write with type interpretation. </summary>
        public static void ReinterpretStore<T, U>(in this UnsafeList<T> list, int destIndex, U data) 
            where U : struct where T : unmanaged
        {
            list.CheckReinterpretStoreRange<T, U>(destIndex);
            ReinterpretStoreUnsafe(list, destIndex, data);
        }

        static void ReinterpretStoreUnsafe<T, U>(in this UnsafeList<T> list, int destTIndex, U data, bool logError = true) 
            where U : struct where T : unmanaged
        {
#if CORRUPTION_CHECKS
            if (logError && !math.ispow2(list.Capacity))
                UnityEngine.Debug.LogError($"Capacity {list.Capacity} is not a power of 2!");
#endif
            UnsafeUtility.WriteArrayElement(list.Ptr + destTIndex, 0, data);
        }
        
        /// <summary> Clears memory (memclear, writes 0s) from length to capacity. </summary>
        public static void ClearUnusedMemory<T>(this NativeList<T> list)
            where T : unmanaged
        {
            list.ConditionalCheckIsCreated();
            // Trip safety checks
            byte* ptr = (byte*)list.GetUnsafePtr();
            // Then just call into unsafelist version for code reuse
            list.GetUnsafeList()->ClearUnusedMemory();
        }
        
        /// <summary> Clears memory (memclear, writes 0s) from length to capacity. </summary>
        public static void ClearUnusedMemory<T>(in this UnsafeList<T> list)
            where T : unmanaged
        {
            if (!list.IsCreated)
            {
                UnityEngine.Debug.LogError("UnsafeList is not created!");
                return;
            }
            
            byte* ptr = (byte*)list.Ptr;

            if (ptr == null)
            {
                UnityEngine.Debug.LogError("UnsafeList Ptr is null!");
                return;
            }
            
            var sizeOf = sizeof(T);
            var listLength = list.Length;
            var listCapacity = list.Capacity;
            
            if (listLength >= listCapacity)
                return;
            
            var elementsToClear = listCapacity - listLength;
            
            UnsafeUtility.MemClear(ptr + listLength * sizeOf, elementsToClear * sizeOf);
        }
    }
}