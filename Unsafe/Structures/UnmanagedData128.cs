using System;
using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using VLib.Unsafe.Utility;

namespace VLib.Unsafe.Structures
{
    /// <summary> 128 bytes of unmanaged data, convertible to any unmanaged type up to 128 bytes. </summary>
    [Serializable]
    public struct UnmanagedData128
    {
        // Use two float4x4 matrices (each 64 bytes) to total 128 bytes.
        [SerializeField] float4x4 internalDataA;
        [SerializeField] float4x4 internalDataB;

        public static void ConvertFrom<T>(in T data, out UnmanagedData128 unmanagedData)
            where T : struct
        {
            unmanagedData = default;
            VUnsafeUtil.StructCopyAIntoB(data, ref unmanagedData);
        }
    }
    
    public static class UnmanagedData128Ext
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T ConvertTo<T>(ref this UnmanagedData128 unmanagedData)
            where T : unmanaged
        {
            if (Hint.Unlikely(UnsafeUtility.SizeOf<T>() > 128))
                Debug.LogError("Size of T must be 128 or fewer bytes!");
            return ref UnsafeUtility.As<UnmanagedData128, T>(ref unmanagedData);
        }
    }

}