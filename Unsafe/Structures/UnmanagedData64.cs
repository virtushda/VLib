using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using VLib.Unsafe.Utility;

namespace VLib.Unsafe.Structures
{
    /// <summary> 64 bytes of unmanaged data, convert to another type! Basically a runtime generic/dynamic value at the cost of memory. </summary>
    public struct UnmanagedData64
    {
        float4x4 internalData;
        
        public static void ConvertFrom<T>(in T data, out UnmanagedData64 unmanagedData)
            where T : struct
        {
            unmanagedData = default;
            VUnsafeUtil.StructCopyAIntoB(data, ref unmanagedData);
        }
    }

    public static class UnmanagedData64Ext
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T ConvertTo<T>(ref this UnmanagedData64 unmanagedData)
            where T : unmanaged
        {
            if (Hint.Unlikely(UnsafeUtility.SizeOf<T>() > 64))
                // Is an error instead of an exception because burst will crash if an exception is thrown, whereas the error will definitely make it into the log.
                Debug.LogError("Size of T must be 64 or fewer bytes!");
            return ref UnsafeUtility.As<UnmanagedData64, T>(ref unmanagedData);
        }
    }
}