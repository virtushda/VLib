using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace VLib
{
    /// <summary> 64 bytes of unmanaged data, convert to another type! </summary>
    public struct UnmanagedData64
    {
        float4x4 internalData;

        public T ConvertTo<T>()
            where T : struct
        {
            if (UnsafeUtility.SizeOf<T>() > 64)
                throw new System.Exception("Size of T must be 64 or fewer bytes!");
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