using Unity.Collections.LowLevel.Unsafe;

namespace VLib.Unsafe.Utility
{
    public static unsafe class VUnsafeUtil
    {
        /// <summary> Allows you to return a 'ref' to null, such that TryGetRef patterns can cleanly return null refs when false. </summary>
        public static ref T NullRef<T>() where T : struct => ref UnsafeUtility.AsRef<T>(null);
        
        public static bool IsNullRef<T>(this ref T reference) where T : struct => UnsafeUtility.AddressOf(ref reference) == null;
    }
}