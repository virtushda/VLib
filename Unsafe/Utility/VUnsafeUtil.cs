using Unity.Collections.LowLevel.Unsafe;

namespace VLib.Unsafe.Utility
{
    public static unsafe class VUnsafeUtil
    {
        /// <summary> Allows you to return a 'ref' to null, such that TryGetRef patterns can cleanly return null refs when false. </summary>
        public static ref T NullRef<T>() where T : struct => ref UnsafeUtility.AsRef<T>(null);

        /// <summary> <inheritdoc cref="NullRef{T}"/> </summary>
        public static bool IsNullRef<T>(this ref T reference) where T : struct => UnsafeUtility.AddressOf(ref reference) == null;
        
        /// <summary> Copies the contents of struct A into struct B. <br/>
        /// The size of A must be less than or equal to the size of B. </summary>
        public static void StructCopyAIntoB<TA, TB> (in TA a, ref TB b)
            where TA : struct 
            where TB : struct
        {
            // Ensure that the size of TA is less than or equal to the size of TB, so we can safely copy TA into TB
            BurstAssert.ValueGreaterOrEqualTo(UnsafeUtility.SizeOf<TB>(), UnsafeUtility.SizeOf<TA>());
            ref var bAsA = ref UnsafeUtility.As<TB, TA>(ref b);
            bAsA = a;
        }
    }
}