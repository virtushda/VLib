using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace VLib
{
    [StructLayout(LayoutKind.Sequential)]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
    public unsafe struct VUnsafeRefDisposable<T>
        : IDisposable
        , IEquatable<VUnsafeRefDisposable<T>>
        where T : unmanaged, IDisposable
    {
        [NativeDisableUnsafePtrRestriction]
        void* ptr;
        public void* Ptr => ptr;
        public T* TPtr => (T*)ptr;

        internal Allocator m_AllocatorLabel;

        /// <summary>
        /// Initializes and returns an instance of VUnsafeRef.
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <param name="options">Whether newly allocated bytes should be zeroed out.</param>
        public VUnsafeRefDisposable(Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            Allocate(allocator, out this);
            if (options == NativeArrayOptions.ClearMemory)
            {
                if (ptr == null)
                {
                    UnityEngine.Debug.LogError("Ptr NULL for VUnsafeBufferedRef!");
                    return;
                }
                UnsafeUtility.MemClear(ptr, UnsafeUtility.SizeOf<T>());
            }
        }

        /// <summary>
        /// Initializes and returns an instance of VUnsafeRef.
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <param name="value">The initial value.</param>
        public VUnsafeRefDisposable(T value, Allocator allocator)
        {
            Allocate(allocator, out this);
            *(T*)ptr = value;
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckAllocateArguments(int length, Allocator allocator, long totalSize)
        {
            if (allocator <= Allocator.None)
                throw new ArgumentException("Allocator must be Temp, TempJob or Persistent", nameof (allocator));
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof (length), "Length must be >= 0");
            IsUnmanagedAndThrow();
        }

        private static unsafe void Allocate(Allocator allocator, out VUnsafeRefDisposable<T> unsafeRef)
        {
            long num = UnsafeUtility.SizeOf<T>();
            CheckAllocateArguments(1, allocator, num);
            unsafeRef = new VUnsafeRefDisposable<T>();
            unsafeRef.ptr = UnsafeUtility.Malloc(num, UnsafeUtility.AlignOf<T>(), allocator);
            unsafeRef.m_AllocatorLabel = allocator;
        }

        /// <summary>
        /// The value stored in this reference.
        /// </summary>
        /// <param name="value">The new value to store in this reference.</param>
        /// <value>The value stored in this reference.</value>
        public T Value
        {
            get => *(T*)ptr;
            set => *(T*)ptr = value;
        }
        
        public ref T ValueRef => ref *(T*)ptr;

        /// <summary>
        /// Whether this reference has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this reference has been allocated (and not yet deallocated).</value>
        public bool IsCreated => ptr != null;

        /// <summary>
        /// Releases all resources (memory and safety handles).
        /// </summary>
        public void Dispose()
        {
            if (m_AllocatorLabel is Allocator.Invalid)
            {
                UnityEngine.Debug.LogError("Aborting dispose call, cannot dispose VUnsafeRefDisposable with 'Invalid' allocator. Was likely already disposed!");
                return;
            }
            
            if ((IntPtr)TPtr != IntPtr.Zero)
                TPtr->Dispose();
            
            if (m_AllocatorLabel > Allocator.None && (IntPtr)ptr != IntPtr.Zero)
            {
                UnsafeUtility.Free(ptr, m_AllocatorLabel);
                this.m_AllocatorLabel = Allocator.Invalid;
            }
            
            ptr = null;
        }
        
        [BurstDiscard]
        internal static void IsUnmanagedAndThrow()
        {
            if (!UnsafeUtility.IsValidNativeContainerElementType<T>())
                throw new InvalidOperationException(
                    $"{typeof(T)} used in VUnsafeRefDisposable<{typeof(T)}> must be unmanaged (contain no managed types) and cannot itself be a native container type.");
        }

        /// <summary>
        /// Copy the value of another reference to this reference.
        /// </summary>
        /// <param name="reference">The reference to copy from.</param>
        public void CopyFrom(VUnsafeRefDisposable<T> reference)
        {
            Copy(this, reference);
        }

        /// <summary>
        /// Copy the value of this reference to another reference.
        /// </summary>
        /// <param name="reference">The reference to copy to.</param>
        public void CopyTo(VUnsafeRefDisposable<T> reference)
        {
            Copy(reference, this);
        }

        /// <summary>
        /// Returns true if the value stored in this reference is equal to the value stored in another reference.
        /// </summary>
        /// <param name="other">A reference to compare with.</param>
        /// <returns>True if the value stored in this reference is equal to the value stored in another reference.</returns>
        public bool Equals(VUnsafeRefDisposable<T> other)
        {
            return ptr == other.ptr;
        }

        /// <summary>
        /// Returns true if the value stored in this reference is equal to an object.
        /// </summary>
        /// <remarks>Can only be equal if the object is itself a VUnsafeRef.</remarks>
        /// <param name="obj">An object to compare with.</param>
        /// <returns>True if the value stored in this reference is equal to the object.</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            return obj is VUnsafeRefDisposable<T> && Equals((VUnsafeRefDisposable<T>)obj);
        }

        /// <summary>
        /// Returns the hash code of this reference.
        /// </summary>
        /// <returns>The hash code of this reference.</returns>
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
        
        /// <summary>
        /// Returns true if the values stored in two references are equal.
        /// </summary>
        /// <param name="left">A reference.</param>
        /// <param name="right">Another reference.</param>
        /// <returns>True if the two values are equal.</returns>
        public static bool operator ==(VUnsafeRefDisposable<T> left, VUnsafeRefDisposable<T> right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Returns true if the values stored in two references are unequal.
        /// </summary>
        /// <param name="left">A reference.</param>
        /// <param name="right">Another reference.</param>
        /// <returns>True if the two values are unequal.</returns>
        public static bool operator !=(VUnsafeRefDisposable<T> left, VUnsafeRefDisposable<T> right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Copies the value of a reference to another reference.
        /// </summary>
        /// <param name="dst">The destination reference.</param>
        /// <param name="src">The source reference.</param>
        public static void Copy(VUnsafeRefDisposable<T> dst, VUnsafeRefDisposable<T> src)
        {
            if (!dst.IsCreated || !src.IsCreated)
                return;
            UnsafeUtility.MemCpy(dst.ptr, src.ptr, UnsafeUtility.SizeOf<T>());
        }
    }

    /*[NativeContainer]
    unsafe struct VUnsafeRefDispose
    {
        [NativeDisableUnsafePtrRestriction]
        internal void* m_Data;

        internal AllocatorManager.AllocatorHandle m_AllocatorLabel;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#endif

        public void Dispose()
        {
            Memory<>.Unmanaged.Free(m_Data, m_AllocatorLabel);
        }
    }*/

    /*[BurstCompile]
    struct VUnsafeRefDisposeJob : IJob
    {
        internal VUnsafeRefDispose Data;

        public void Execute()
        {
            Data.Dispose();
        }
    }*/
}