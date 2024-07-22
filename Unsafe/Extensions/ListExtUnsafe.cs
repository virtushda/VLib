using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace VLib
{
    public static class ListExtUnsafe
    {
        /// <summary>
        /// Creates a native 'view' into managed memory without any copies. Be WARNED, this can be very dangerous.
        /// GCHandle must be freed with 'UnsafeUtility.ReleaseGCObject(gcHandle)'!
        /// </summary>
        /// <typeparam name="T">Must be native-compatible!</typeparam>
        public static NativeArray<T> GetNativeView<T>(this List<T> list, out ArrayExtUnsafe.NativeViewBaggage baggage, int count = -1)
            where T : struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (count > list.Count)
            {
                count = list.Count;
                Debug.LogError($"GetNativeView 'Count' '{count}' is larger than the list.count of '{list.Count}'.");
            }
#endif

            if (count < 0)
                count = list.Count;
            //Get array inside list (length will NOT match count)
            return list.GetInternalArray().GetNativeView(out baggage, count);
        }
        
        public static long MemoryFootprintBytes<T>(this List<T> list)
            where T : unmanaged
        {
            return list.Capacity * UnsafeUtility.SizeOf<T>();
        }
        
        public static long MemoryFootprintBytesManaged<T>(this List<T> list)
        {
            long footprint = 0;

            if (list == null)
                return footprint;

            // List footprint
            footprint += UnsafeUtility.SizeOf<IntPtr>() * list.Capacity;

            if (list.Count > 0 && list[0] is IMemoryReporter)
            {
                foreach (var item in list)
                    ((IMemoryReporter) item).ReportBytes();
            }
            else
            {
                for (int i = 0; i < list.Count; i++)
                {
                    footprint += Marshal.SizeOf(list[i]);
                }
            }

            return footprint;
        }
    }
}