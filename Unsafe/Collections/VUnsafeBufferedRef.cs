using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using VLib.Unsafe.Utility;
using Debug = UnityEngine.Debug;

namespace VLib
{
    /// <summary> Essentially an unsafe lower-level version of <see cref="NativeReference{T}"/> <br/>
    /// Double buffers its pointer to be able to detect memory corruption! <br/>
    /// (this check is stripped from most properties in builds, but is still available through 'TryGet' methods) </summary>
    /// <typeparam name="T"></typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
    public struct VUnsafeBufferedRef<T> : IEquatable<VUnsafeBufferedRef<T>>
        where T : unmanaged
    {
        VUnsafeBufferedPtr data;

        internal AllocatorManager.AllocatorHandle m_AllocatorLabel;

        /// <summary> Initializes and returns an instance of VUnsafeRef. </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <param name="options">Whether newly allocated bytes should be zeroed out.</param>
        public unsafe VUnsafeBufferedRef(AllocatorManager.AllocatorHandle allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            Allocate(allocator, out this);
            if (options == NativeArrayOptions.ClearMemory)
                UnsafeUtility.MemClear(data.Ptr, UnsafeUtility.SizeOf<T>());
        }

        /// <summary> Initializes and returns an instance of VUnsafeRef. </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <param name="value">The initial value.</param>
        public VUnsafeBufferedRef(T value, AllocatorManager.AllocatorHandle allocator)
        {
            Allocate(allocator, out this);
            Value = value;
        }

        /// <summary> This type does not ALWAYS allocate, it can be used as a pointer safety wrapper! </summary>
        static unsafe void Allocate(AllocatorManager.AllocatorHandle allocator, out VUnsafeBufferedRef<T> reference)
        {
            if (allocator.ToAllocator <= Allocator.None)
                throw new ArgumentException("Allocator must not be Allocator.None or Allocator.Invalid!");
            reference = default;
            reference.data.Ptr = (T*) UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), allocator.ToAllocator);
            reference.m_AllocatorLabel = allocator;
        }

        /// <summary> Does not check validity, just checks that internal pointer isn't null. Corruption can make this return true! </summary>
        public readonly bool IsCreated => data.IsCreated;

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public readonly void ConditionalCheckValid() => data.ConditionalCheckValid();

        /// <summary> NOTE: Extension <see cref="NativeCollectionExtUnsafe.DisposeRefToDefault{T}"/> is ideal to use.
        /// Releases all resources (memory and safety handles). Inherently safe, will not throw exception if already disposed.</summary>
        public unsafe void DisposeUnsafe()
        {
            if (TryGetTPtr(out var tPtr) && tPtr != null)
            {
                // Already disposed?
                if (m_AllocatorLabel.ToAllocator is Allocator.Invalid or Allocator.None)
                {
                    Debug.LogError("The NativeArray can not be Disposed because it was not allocated with a valid allocator. Are you disposing twice?");
                    return;
                }
                UnsafeUtility.Free(tPtr, m_AllocatorLabel.ToAllocator);
                m_AllocatorLabel = default;
                data = default;
            }
        }
        
        /// <summary> The value stored in this reference. Safety checks are conditionally compiled out in builds. Use TryGet if needed in builds. </summary>
        /// <param name="value">The new value to store in this reference.</param>
        /// <value>The value stored in this reference.</value>
        public unsafe T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => *TPtr;
            set => *TPtr = value;
        }

        public readonly bool TryGetValue(out T value)
        {
            value = TryGetRefReadonly(out var hasRef);
            return hasRef;
        }

        /// <summary> Safety is conditionally compiled out in builds. Use <see cref="TryGetTPtr"/> to go through safety. </summary>
        public readonly unsafe ref T ValueRef
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref *TPtr;
        }

        public readonly unsafe ref T TryGetRef(out bool hasRef)
        {
            hasRef = TryGetTPtr(out var ptr);
            return ref hasRef ? ref UnsafeUtility.AsRef<T>(ptr) : ref VUnsafeUtil.NullRef<T>();
        }
        
        public readonly ref T TryGetRefReadonly(out bool hasRef) => ref TryGetRef(out hasRef);
        
        /// <summary> Safety is conditionally compiled out in builds. Use <see cref="TryGetTPtr"/> to go through safety. </summary>
        public unsafe T* TPtr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => (T*) data.Ptr;
            private set => data.Ptr = value;
        }

        /// <summary> Try grab the ptr without triggering any log errors. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly unsafe bool TryGetTPtr(out T* tPtr)
        {
            var hasRef = data.TryGetPtr(out var ptr);
            tPtr = (T*) ptr;
            return hasRef;
        }

        /// <summary>
        /// Copy the value of another reference to this reference.
        /// </summary>
        /// <param name="reference">The reference to copy from.</param>
        public void CopyFrom(VUnsafeBufferedRef<T> reference) => Copy(this, reference);

        /// <summary>
        /// Copy the value of this reference to another reference.
        /// </summary>
        /// <param name="reference">The reference to copy to.</param>
        public readonly void CopyTo(VUnsafeBufferedRef<T> reference) => Copy(reference, this);

        /// <summary> Returns true if the value stored in this reference is equal to the value stored in another reference. </summary>
        public bool Equals(VUnsafeBufferedRef<T> other) => data.Equals(other.data);

        /// <summary>Returns true if the value stored in this reference is equal to an object.</summary>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is VUnsafeRef<T> && Equals((VUnsafeRef<T>)obj);
        }

        /// <summary>
        /// Returns the hash code of this reference.
        /// </summary>
        /// <returns>The hash code of this reference.</returns>
        public override int GetHashCode() => Value.GetHashCode();

        /// <summary>
        /// Returns true if the values stored in two references are equal.
        /// </summary>
        /// <param name="left">A reference.</param>
        /// <param name="right">Another reference.</param>
        /// <returns>True if the two values are equal.</returns>
        public static bool operator ==(VUnsafeBufferedRef<T> left, VUnsafeBufferedRef<T> right) => left.Equals(right);

        /// <summary>
        /// Returns true if the values stored in two references are unequal.
        /// </summary>
        /// <param name="left">A reference.</param>
        /// <param name="right">Another reference.</param>
        /// <returns>True if the two values are unequal.</returns>
        public static bool operator !=(VUnsafeBufferedRef<T> left, VUnsafeBufferedRef<T> right) => !left.Equals(right);

        public static implicit operator T(VUnsafeBufferedRef<T> reference) => reference.Value;

        /// <summary> Copies the value of a reference to another reference. </summary>
        /// <param name="dst">The destination reference.</param>
        /// <param name="src">The source reference.</param>
        public static void Copy(VUnsafeBufferedRef<T> dst, VUnsafeBufferedRef<T> src)
        {
            if (!dst.IsCreated || !src.IsCreated)
                return;
            unsafe
            {
                UnsafeUtility.MemCpy(dst.data.Ptr, src.data.Ptr, UnsafeUtility.SizeOf<T>());
            }
        }
    }

    public static class VUnsafeBufferedRefExt
    {
        public static void DisposeRefToDefault<T>(this ref VUnsafeBufferedRef<T> reference) where T : unmanaged
        {
            if (reference.IsCreated)
                reference.DisposeUnsafe();
            reference = default;
        }
    }
}