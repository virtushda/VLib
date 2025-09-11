using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace VLib.Unsafe.Utility
{
    [GenerateTestsForBurstCompatibility]
    public static unsafe class VUnsafeUtil
    {
        /// <summary> Allows you to return a 'ref' to null, such that TryGetRef patterns can cleanly return null refs when false. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T NullRef<T>() where T : struct => ref UnsafeUtility.AsRef<T>(null);

        /// <summary> <inheritdoc cref="NullRef{T}"/> </summary>
        public static bool IsNullRef<T>(this ref T reference) where T : struct => UnsafeUtility.AddressOf(ref reference) == null;
        
        public static bool IsNullReadonlyRef<T>(in T reference) where T : struct => System.Runtime.CompilerServices.Unsafe.AsRef(reference).IsNullRef();

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), 
         Conditional("UNITY_DOTS_DEBUG"),
         Conditional("DEVELOPMENT_BUILD")]
        public static void CheckReadonlyRefNotNull<T>(in T reference)
            where T : struct
        {
            if (IsNullReadonlyRef(reference))
                throw new System.ArgumentNullException();
        }
        
        /// <summary> Copies the contents of struct A into struct B. <br/>
        /// The size of A must be less than or equal to the size of B. </summary>
        public static void StructCopyAIntoB<TA, TB> (in TA a, ref TB b)
            where TA : struct 
            where TB : struct
        {
            // Ensure that the size of TA is less than or equal to the size of TB, so we can safely copy TA into TB
            BurstAssert.ValueLessOrEqualTo(UnsafeUtility.SizeOf<TA>(), UnsafeUtility.SizeOf<TB>());
            // Write A value into B memory
            ref var bAsA = ref UnsafeUtility.As<TB, TA>(ref b);
            bAsA = a;
        }
        
        /// <summary> Allows you to refer to a struct as a different type, as long as it is smaller or equal in size. <br/>
        /// Compiles to <see cref="UnsafeUtility.As{TFrom,TTo}(ref TFrom)"/> in release. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref U AsChecked<T, U>(ref T t)
            where T : unmanaged
            where U : unmanaged
        {
            // Check we're not reading more memory, that is almost always dangerous
            BurstAssert.TrueThrowing(UnsafeUtility.SizeOf<U>() <= UnsafeUtility.SizeOf<T>());
            return ref UnsafeUtility.As<T, U>(ref t);
        }
    }
}