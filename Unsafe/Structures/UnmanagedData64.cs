using Unity.Burst.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace VLib.Unsafe.Structures
{
    /// <summary> 64 bytes of unmanaged data, convert to another type! Basically a runtime generic/dynamic value at the cost of memory. </summary>
    public struct UnmanagedData64
    {
        float4x4 internalData;

        public T ConvertTo<T>()
            where T : struct
        {
            bool isOverSize = UnsafeUtility.SizeOf<T>() > 64;
            if (Hint.Unlikely(isOverSize))
                // Is an error instead of an exception because burst will crash if an exception is thrown, whereas the error will definitely make it into the log.
                Debug.LogError("Size of T must be 64 or fewer bytes!");
            return UnsafeUtility.As<UnmanagedData64, T>(ref this);
        }
        
        public static UnmanagedData64 ConvertFrom<T>(T data)
            where T : struct
        {
            if (UnsafeUtility.SizeOf<T>() > 64)
                throw new System.Exception("Size of T must be 64 or fewer bytes!");
            return UnsafeUtility.As<T, UnmanagedData64>(ref data);
        }
    }
}