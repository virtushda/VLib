#if UNITY_EDITOR || DEVELOPMENT_BUILD || PKSAFE
#define SAFETY
//#define CORRUPTION_CHECKS
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Debug = System.Diagnostics.Debug;
using HashSetExtensions = Unity.Collections.LowLevel.Unsafe.HashSetExtensions;

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
            return ReadAtIndexUnsafe(list, index);
        }
        
        public static unsafe T ReadAtIndexUnsafe<T>(this UnsafeList<T>.ReadOnly list, int index)
            where T : unmanaged
        {
            return list.Ptr[index];
        }
    }
    
    public static unsafe class NativeCollectionExtUnsafe
    {
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

        public static void CopyToUnsafe<T>(this UnsafeList<T> src, int srcIndex, GCHandle dstHandlePinnedType, int dstIndex, int length)
            where T : unmanaged
        {
            //Could probably use more checks in here... Add if you encounter an exception PLAS
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!dstHandlePinnedType.IsAllocated)
            {
                UnityEngine.Debug.LogError("PKSAFE Exception: GCHandle is not allocated!");
                return;
            }
#endif

            UnsafeUtility.MemCpy((void*)((IntPtr)(void*)dstHandlePinnedType.AddrOfPinnedObject() + dstIndex * UnsafeUtility.SizeOf<T>()),
                                 (void*)((IntPtr)src.Ptr + srcIndex * UnsafeUtility.SizeOf<T>()),
                                 length * UnsafeUtility.SizeOf<T>());
        }

        public static void CopyToUnsafe<T>(this NativeArray<T> src, int srcIndex, ref UnsafeList<T> dst, int dstIndex, int length)
            where T : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!dst.IsCreated)
                throw new ArgumentNullException(nameof(dst));
#endif

            //Ensure Cap
            int previousListLength = dst.Length;
            int requiredCap = dstIndex + length;
            if (dst.Capacity < requiredCap)
                dst.Resize(requiredCap, NativeArrayOptions.ClearMemory);

            void* srcPtr = src.GetUnsafeReadOnlyPtr();
            UnsafeUtility.MemCpy((void*)((IntPtr)dst.Ptr + dstIndex * UnsafeUtility.SizeOf<T>()),
                                 (void*)((IntPtr)srcPtr + srcIndex * UnsafeUtility.SizeOf<T>()),
                                 length * UnsafeUtility.SizeOf<T>());

            dst.Length = math.max(previousListLength, requiredCap);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* ForceGetUnsafePtrNOSAFETY<T>(this NativeArray<T> array) 
            where T : struct =>
            array.IsCreated ? NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(array) : throw new NullReferenceException("Array is not created!");

        /// <summary>Dodgy with certain structs!</summary>
        public static ref T GetRef<T>(ref this NativeArray<T> array, int index)
            where T : struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // You might want to validate the index first, as the unsafe method won't do that.
            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
#endif
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafePtr(), index);
        }

        public static ref T GetRefReadOnly<T>(ref this NativeArray<T> array, int index)
            where T : struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // You might want to validate the index first, as the unsafe method won't do that.
            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
#endif
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafeReadOnlyPtr(), index);
        }

        public static ref T GetRefReadOnly<T>(ref this NativeArray<T>.ReadOnly array, int index)
            where T : struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // You might want to validate the index first, as the unsafe method won't do that.
            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
#endif
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafeReadOnlyPtr(), index);
        }

        public static ref T GetRefNoChecksUNSAFE<T>(ref this NativeArray<T> array, int index)
            where T : struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // You might want to validate the index first, as the unsafe method won't do that.
            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException("PKSAFE Exception: " + nameof(index));
#endif
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.ForceGetUnsafePtrNOSAFETY(), index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ReadUnsafe<T>(in this NativeArray<T> array, int index)
            where T : unmanaged
        {
#if PKSAFE
            if (!array.IsCreated)
                UnityEngine.Debug.LogError("PKSAFE Exception: Array is not created!");
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
            if (index >= array.Length)
                throw new IndexOutOfRangeException($"PKSAFE Exception: Index '{index}' is >= Length '{array.Length}'");
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
            if (index >= array.Length)
                throw new IndexOutOfRangeException($"PKSAFE Exception: Index '{index}' is >= Length '{array.Length}'");
            ((T*)array.ForceGetUnsafePtrNOSAFETY())[index] = value;
#else
            array[index] = value; //Safety only exists in editor... great feature... but so annoying sometimes!
#endif
        }
        
        public static bool TryGetElementPtr<T>(this NativeArray<T> nativeArray, int index, out T* valuePtr)
            where T : unmanaged
        {
            var isValidIndex = nativeArray.IsValidIndex(index);
            valuePtr = isValidIndex ? nativeArray.GetArrayRefElementPtr(index) : (T*) IntPtr.Zero;
            return isValidIndex;
        }
        
        public static bool TryGetElementPtrReadOnly<T>(this NativeArray<T> nativeArray, int index, out T* valuePtr)
            where T : unmanaged
        {
            var isValidIndex = nativeArray.IsValidIndex(index);
            valuePtr = isValidIndex ? nativeArray.GetArrayElementPtrReadOnly(index) : default;
            return isValidIndex;
        }
        
        public static bool TryGetElementPtrNoSafety<T>(this NativeArray<T> nativeArray, int index, out T* valuePtr)
            where T : unmanaged
        {
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
                UnityEngine.Debug.LogError("PKSAFE Exception: Array is not created!");
            if (!array.IsValidIndex(index))
                UnityEngine.Debug.LogError($"PKSAFE Exception: Index '{index}' is >= Length '{array.Length}'");
#endif
            return ((T*) array.GetUnsafePtr()) + index;
        }

        public static T* GetArrayRefElementPtr<T>(ref this NativeArray<T> array, int index)
            where T : unmanaged
        {
            // Detect this stuff in editor mode
#if SAFETY
            if (!array.IsCreated)
                UnityEngine.Debug.LogError("PKSAFE Exception: Array is not created!");
            if (!array.IsValidIndex(index))
                UnityEngine.Debug.LogError($"PKSAFE Exception: Index '{index}' is >= Length '{array.Length}'");
#endif
            return ((T*) array.GetUnsafePtr()) + index;
        }

        public static T* GetArrayElementPtrReadOnly<T>(ref this NativeArray<T> array, int index)
            where T : unmanaged
        {
            // Detect this stuff in editor mode
#if SAFETY
            if (!array.IsCreated)
                UnityEngine.Debug.LogError("PKSAFE Exception: Array is not created!");
            if (!array.IsValidIndex(index))
                UnityEngine.Debug.LogError($"PKSAFE Exception: Index '{index}' is >= Length '{array.Length}'");
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
                UnityEngine.Debug.LogError("PKSAFE Exception: Array is not created!");
            if (!array.IsValidIndex(index))
                UnityEngine.Debug.LogError($"PKSAFE Exception: Index '{index}' is >= Length '{array.Length}'");
#endif
            return ((T*) array.ForceGetUnsafePtrNOSAFETY()) + index;
        }
        //(T*) ((IntPtr) array.ForceGetUnsafePtrNOSAFETY() + index * sizeof(T));

        /// <summary>WARNING: Very unsafe! Do not dispose returned list! This is a view into a nativearray buffer.</summary>
        public static UnsafeList<T> AsUnsafeList<T>(in this NativeArray<T> array, NativeSafety safety = NativeSafety.ReadWrite) 
            where T : unmanaged
        {
            void* bufferVoidPtr = null;
            if (!array.IsCreated)
                throw new ArgumentException("PKSAFE Exception: NativeArray is not created!");
            if (safety == NativeSafety.ReadWrite)
                bufferVoidPtr = array.GetUnsafePtr();
            else if (safety == NativeSafety.ReadOnly)
                bufferVoidPtr = array.GetUnsafeReadOnlyPtr();
            else
                bufferVoidPtr = array.ForceGetUnsafePtrNOSAFETY();
            
            T* bufferPtr = (T*) bufferVoidPtr;
            return new UnsafeList<T>(bufferPtr, array.Length);
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
                array.GetRef(i).Dispose();
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
        public static void DisposeRefToDefault<T>(ref this T value)
            where T : struct, IDisposable
        {
            value.Dispose();
            value = default;
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

        public static bool IsIndexValid<T>(this UnsafeList<T> list, int index, bool logError = true) 
            where T : unmanaged
        {
#if CORRUPTION_CHECKS
            if (logError && !math.ispow2(list.Capacity))
                UnityEngine.Debug.LogError($"Capacity {list.Capacity} is not a power of 2!");
#endif
            return !(!list.IsCreated || index < 0 || index >= list.Length);
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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            
            // You might want to validate the index first, as the unsafe method won't do that.
            if (index < 0 || index >= list.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
#endif
            return ref UnsafeUtility.ArrayElementAsRef<T>(list.GetUnsafePtr(), index);
        }

        public static ref T GetRefReadOnly<T>(ref this NativeList<T> list, int index, bool logError = true)
            where T : unmanaged
        {
#if CORRUPTION_CHECKS
            if (logError && !math.ispow2(list.Capacity))
                UnityEngine.Debug.LogError($"Capacity {list.Capacity} is not a power of 2!");
#endif
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            
            // You might want to validate the index first, as the unsafe method won't do that.
            if (index < 0 || index >= list.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
#endif
            return ref UnsafeUtility.ArrayElementAsRef<T>(list.GetUnsafeReadOnlyPtr(), index);
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
                UnityEngine.Debug.LogError("PKSAFE Exception: Array is not created!");
#endif
            return (UnsafeList<T>*) NativeListUnsafeUtility.GetInternalListDataPtrUnchecked(ref list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* ForceGetUnsafePtrNOSAFETY<T>(this ref NativeList<T> list, bool logError = true)
            where T : unmanaged
        {
            // Detect this stuff in editor mode#if CORRUPTION_CHECKS
#if CORRUPTION_CHECKS
            if (logError && !math.ispow2(list.Capacity))
                UnityEngine.Debug.LogError($"Capacity {list.Capacity} is not a power of 2!");
#endif
#if SAFETY
            if (!list.IsCreated)
                UnityEngine.Debug.LogError("PKSAFE Exception: Array is not created!");
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
            if (!list.IsIndexValid(index))
                UnityEngine.Debug.LogError($"PKSAFE Exception: Index '{index}' invalid on NativeList with length {list.Length}");
#endif
            return list.GetUnsafePtr() + index;
            //return (T*) ((IntPtr) list.GetUnsafePtr() + index * sizeof(T));
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
            if (!list.IsIndexValid(index, logError))
                UnityEngine.Debug.LogError($"PKSAFE Exception: Array is not created or index {index} is invalid in UnsafeList with length {list.Length}");
#endif
            return list.Ptr + index;
        }

        /// <summary> Safety in editor only </summary>
        public static T* GetListElementPtr<T>(in this VUnsafeList<T> list, int index, bool logError = true)
            where T : unmanaged
        {
            // Detect this stuff in editor mode
#if CORRUPTION_CHECKS
            if (logError && !math.ispow2(list.Capacity))
                UnityEngine.Debug.LogError($"Capacity {list.Capacity} is not a power of 2!");
#endif
#if SAFETY
            if (!list.IsIndexValid(index, logError))
                UnityEngine.Debug.LogError($"PKSAFE Exception: Array is not created or index {index} is invalid in UnsafeList with length {list.Length}");
#endif
            return list.listData->Ptr + index;
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
            if (!list.IsIndexValid(index))
                UnityEngine.Debug.LogError($"PKSAFE Exception: Index '{index}' invalid on NativeList with length {list.Length}");
#endif
            return (T*) ((IntPtr) list.GetUnsafeReadOnlyPtr() + index * sizeof(T));
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
            if (!list.IsIndexValid(index))
                UnityEngine.Debug.LogError($"PKSAFE Exception: Index '{index}' invalid on NativeList with length {list.Length}");
#endif
            return ((T*) list.ForceGetUnsafePtrNOSAFETY()) + index;
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
            //Make Rooom
            sortedList.InsertRangeWithBeginEnd(startIndex, startIndex + valuesToAdd.Length);
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
        
        /*public static long MemoryFootprintBytes<TKey, TValue>(this NativeParallelHashMap<TKey, TValue> hashMap) 
            where TKey : unmanaged, IEquatable<TKey> 
            where TValue : unmanaged
        {
            long footprint = 0;
            int capacity = hashMap.Capacity;
            footprint += sizeof(TKey) * capacity;
            footprint += sizeof(TValue) * capacity;
            return footprint;
        }
        
        public static long MemoryFootprintBytes<T>(this NativeParallelHashSet<T> array) 
            where T : unmanaged, IEquatable<T> =>
            UnsafeUtility.SizeOf<T>() * array.Capacity;*/
        
        public static void DisposeSafe<T>(this ref UnsafeList<T> list, bool logError = true)
            where T : unmanaged
        {
#if CORRUPTION_CHECKS
            if (logError && list.Capacity != 0 && !math.ispow2(list.Capacity))
                UnityEngine.Debug.LogError($"Capacity {list.Capacity} is not a power of 2!");
#endif
            if (list.IsCreated)
            {
                list.Dispose();
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
#if CORRUPTION_CHECKS
            if (logError && !math.ispow2(list.Capacity))
                UnityEngine.Debug.LogError($"Capacity {list.Capacity} is not a power of 2!");
#endif
            return ref UnsafeUtility.AsRef<UnsafeList<T>>(list.GetUnsafeList());
        }

        public static bool TryGet<T>(this UnsafeList<T> list, int index, out T value, bool logError = true)
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
        
        public static bool TryGetElementPtr<T>(this UnsafeList<T> list, int index, out T* valuePtr)
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
        
        /// <summary> Will return 'default' on a 0-length list. </summary>
        public static T Peek<T>(ref this UnsafeList<T> list, bool logError = true)
            where T : unmanaged
        {
#if CORRUPTION_CHECKS
            if (logError && !math.ispow2(list.Capacity))
                UnityEngine.Debug.LogError($"Capacity {list.Capacity} is not a power of 2!");
#endif
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

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void CheckReinterpretLoadRange<T, U>(this UnsafeList<T> list, int sourceIndex) 
            where T : unmanaged 
            where U : struct
        {
            long num1 = UnsafeUtility.SizeOf<T>();
            long num2 = UnsafeUtility.SizeOf<U>();
            long num3 = list.m_length * num1;
            long num4 = sourceIndex * num1;
            long num5 = num4 + num2;
            if (num4 < 0L || num5 > num3)
                throw new ArgumentOutOfRangeException(nameof (sourceIndex), "loaded byte range must fall inside container bounds");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void CheckReinterpretStoreRange<T, U>(this UnsafeList<T> list, int destIndex)
            where T : unmanaged
            where U : struct
        {
            long num1 = UnsafeUtility.SizeOf<T>();
            long num2 = UnsafeUtility.SizeOf<U>();
            long num3 = list.m_length * num1;
            long num4 = destIndex * num1;
            long num5 = num4 + num2;
            if (num4 < 0L || num5 > num3)
                throw new ArgumentOutOfRangeException(nameof (destIndex), "stored byte range must fall inside container bounds");
        }

        public static U ReinterpretLoad<T, U>(this UnsafeList<T> list, int sourceIndex) where U : struct where T : unmanaged
        {
            list.CheckReinterpretLoadRange<T, U>(sourceIndex);
            return ReinterpretLoadUnsafe<T, U>(list, sourceIndex);
        }

        static U ReinterpretLoadUnsafe<T, U>(this UnsafeList<T> list, int sourceIndex, bool logError = true) 
            where U : struct where T : unmanaged
        {
#if CORRUPTION_CHECKS
            if (logError && !math.ispow2(list.Capacity))
                UnityEngine.Debug.LogError($"Capacity {list.Capacity} is not a power of 2!");
#endif
            return UnsafeUtility.ReadArrayElement<U>((void*) ((IntPtr) list.Ptr + (UnsafeUtility.SizeOf<T>() * sourceIndex)), 0);
        }

        /// <summary> Write with type interpretation. </summary>
        public static void ReinterpretStore<T, U>(this UnsafeList<T> list, int destIndex, U data) where U : struct where T : unmanaged
        {
            list.CheckReinterpretStoreRange<T, U>(destIndex);
            ReinterpretStoreUnsafe(list, destIndex, data);
        }

        static void ReinterpretStoreUnsafe<T, U>(this UnsafeList<T> list, int destTIndex, U data, bool logError = true) 
            where U : struct where T : unmanaged
        {
#if CORRUPTION_CHECKS
            if (logError && !math.ispow2(list.Capacity))
                UnityEngine.Debug.LogError($"Capacity {list.Capacity} is not a power of 2!");
#endif
            UnsafeUtility.WriteArrayElement((void*) ((IntPtr) list.Ptr + (UnsafeUtility.SizeOf<T>() * destTIndex)), 0, data);
        }

        // Faster, but going to stick with roundabout but known safe method above...
        /*public static void WriteToByteListAsUnsafe<T, U>(this UnsafeList<T> list, int indexAsU, U data)
            where T : unmanaged
            where U : unmanaged
        {
            *(U*)((IntPtr) list.Ptr + indexAsU * UnsafeUtility.SizeOf<U>()) = data;
        }*/
        
        /// <summary> Clears memory (memclear, writes 0s) from length to capacity. </summary>
        public static void ClearUnusedMemory<T>(this NativeList<T> list)
            where T : unmanaged
        {
            // Trip safety checks
            byte* ptr = (byte*)list.GetUnsafePtr();
            // Then just call into unsafelist version for code reuse
            list.GetUnsafeList()->ClearUnusedMemory();
        }
        
        /// <summary> Clears memory (memclear, writes 0s) from length to capacity. </summary>
        public static void ClearUnusedMemory<T>(this UnsafeList<T> list)
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