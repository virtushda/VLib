using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace VLib
{
    public static class HashSetExt
    {
        public static long MemoryFootprintBytes<T>(this HashSet<T> hashSet)
            where T : unmanaged
        {
            // Calc Capacity
            return Mathf.NextPowerOfTwo(hashSet.Count) * UnsafeUtility.SizeOf<T>();
        }
        
        public static long MemoryFootprintBytesManaged<T>(this HashSet<T> hashSet, bool countElements = false)
        {
            int capacity = Mathf.NextPowerOfTwo(hashSet.Count);
            long footprint = capacity * Marshal.SizeOf<IntPtr>();

            if (countElements)
            {
                var values = new T[hashSet.Count];
                hashSet.CopyTo(values);
                
                for (int i = 0; i < values.Length; i++)
                {
                    footprint += Marshal.SizeOf(values[i]);
                }
            }
            
            return footprint;
        }
    }
}