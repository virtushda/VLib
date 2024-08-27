using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Debug = UnityEngine.Debug;

namespace VLib
{
    /// <summary> Essentially an unsafe lower-level version of <see cref="NativeReference{T}"/>
    /// Do not allow this to be copied, as the ptr could go rogue if only one copy disposes, and another copy tries to write to it. </summary>
    /// <typeparam name="T"></typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] {typeof(int)})]
    public unsafe struct VUnsafeKeyedRef<T> : IEquatable<VUnsafeKeyedRef<T>>, IDisposable
        where T : unmanaged
    {
        public static implicit operator bool(in VUnsafeKeyedRef<T> unsafeRef) => unsafeRef.IsCreated;
        
        /// <summary> Data ptr </summary>
        [NativeDisableUnsafePtrRestriction] T* ptr;

        /// <summary> A special pointer that should be used to turn off this struct, by setting the pointer to zero. </summary>
        [NativeDisableUnsafePtrRestriction] uint* keyPtr;

        internal AllocatorManager.AllocatorHandle m_AllocatorLabel;

        /// <summary>
        /// Initializes and returns an instance of VUnsafeRef.
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <param name="options">Whether newly allocated bytes should be zeroed out.</param>
        public VUnsafeKeyedRef(AllocatorManager.AllocatorHandle allocator, uint* keyPtr, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            Allocate(allocator, keyPtr, out this);
            if (options == NativeArrayOptions.ClearMemory)
            {
                if (ptr == null)
                {
                    Debug.LogError("Ptr NULL for VUnsafeBufferedRef!");
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
        public VUnsafeKeyedRef(T value, uint* keyPtr, AllocatorManager.AllocatorHandle allocator)
        {
            Allocate(allocator, keyPtr, out this);
            *ptr = value;
        }
        
        /// <summary>
        /// Returns an unallocated instance
        /// </summary>
        /// <param name="valuePtr">The fixed pointer to the value in question</param>
        /// <param name="keyPtr">The fixed pointer to the key</param>
        public VUnsafeKeyedRef(T* valuePtr, uint* keyPtr)
        {
            ptr = valuePtr;
            this.keyPtr = keyPtr;
            m_AllocatorLabel = Allocator.None;
        }

        static void Allocate(AllocatorManager.AllocatorHandle allocator, uint* keyPtr, out VUnsafeKeyedRef<T> reference)
        {
            //CollectionHelper.CheckAllocator(allocator);
            reference = default;
            AllocateMemory(ref reference.ptr, Allocator.Persistent);
            reference.keyPtr = keyPtr;
            reference.m_AllocatorLabel = allocator;
        }

        public readonly void* Ptr => ptr;
        public readonly T* TPtr => ptr;

        /// <summary> The value stored in this reference. </summary>
        public T Value
        {
            readonly get => *ptr;
            set => *ptr = value;
        }

        public readonly ref T ValueRef => ref UnsafeUtility.AsRef<T>(ptr);

        /// <summary> Whether this reference has been allocated (and not yet deallocated). </summary>
        public readonly bool IsCreated => (IntPtr)keyPtr != IntPtr.Zero && *keyPtr == 1;

        public readonly bool TryGetPtr(out T* ptrOut)
        {
            if (IsCreated)
            {
                ptrOut = ptr;
                return true;
            }
            ptrOut = null;
            return false;
        }
        
        /// <summary>Releases all resources (memory and safety handles). Inherently safe, will not throw exception if already disposed.</summary>
        public void Dispose()
        {
            if (m_AllocatorLabel == Allocator.None)
                return;
            
            if (ptr != null)
            {
                DisposeMemory(ref ptr, m_AllocatorLabel);
                m_AllocatorLabel = default;
            }
            else
                Debug.LogWarning("You're trying to dispose a VUnsafeRef that has already been disposed! (It holds a null ptr)");
        }

        /// <summary>
        /// Copy the value of another reference to this reference.
        /// </summary>
        /// <param name="reference">The reference to copy from.</param>
        public void CopyFrom(VUnsafeKeyedRef<T> reference)
        {
            Copy(this, reference);
        }

        /// <summary>
        /// Copy the value of this reference to another reference.
        /// </summary>
        /// <param name="reference">The reference to copy to.</param>
        public readonly void CopyTo(VUnsafeKeyedRef<T> reference)
        {
            Copy(reference, this);
        }

        /// <summary>
        /// Returns true if the value stored in this reference is equal to the value stored in another reference.
        /// </summary>
        /// <param name="other">A reference to compare with.</param>
        /// <returns>True if the value stored in this reference is equal to the value stored in another reference.</returns>
        public readonly bool Equals(VUnsafeKeyedRef<T> other)
        {
            return ptr == other.ptr;
        }

        /// <summary>
        /// Returns true if the value stored in this reference is equal to an object.
        /// </summary>
        /// <remarks>Can only be equal if the object is itself a VUnsafeRef.</remarks>
        /// <param name="obj">An object to compare with.</param>
        /// <returns>True if the value stored in this reference is equal to the object.</returns>
        public readonly override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            return obj is VUnsafeRef<T> && Equals((VUnsafeRef<T>)obj);
        }

        /// <summary>
        /// Returns the hash code of this reference.
        /// </summary>
        /// <returns>The hash code of this reference.</returns>
        public readonly override int GetHashCode()
        {
            return IsCreated ? (int)(IntPtr)ptr : 0;
        }


        /// <summary>
        /// Returns true if the values stored in two references are equal.
        /// </summary>
        /// <param name="left">A reference.</param>
        /// <param name="right">Another reference.</param>
        /// <returns>True if the two values are equal.</returns>
        public static bool operator ==(VUnsafeKeyedRef<T> left, VUnsafeKeyedRef<T> right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Returns true if the values stored in two references are unequal.
        /// </summary>
        /// <param name="left">A reference.</param>
        /// <param name="right">Another reference.</param>
        /// <returns>True if the two values are unequal.</returns>
        public static bool operator !=(VUnsafeKeyedRef<T> left, VUnsafeKeyedRef<T> right)
        {
            return !left.Equals(right);
        }

        public static implicit operator T(VUnsafeKeyedRef<T> reference) => reference.Value;

        /// <summary>
        /// Copies the value of a reference to another reference.
        /// </summary>
        /// <param name="dst">The destination reference.</param>
        /// <param name="src">The source reference.</param>
        public static void Copy(VUnsafeKeyedRef<T> dst, VUnsafeKeyedRef<T> src)
        {
            if (!dst.IsCreated || !src.IsCreated)
                return;
            UnsafeUtility.MemCpy(dst.ptr, src.ptr, UnsafeUtility.SizeOf<T>());
        }
        
        #region Alloc Utils

        /// <summary> The internal memory allocation method of VUnsafeRef </summary>
        public static void AllocateMemory(ref T* ptrRef, AllocatorManager.AllocatorHandle allocator)
        {
            if (ptrRef != null)
                throw new ArgumentException("Incoming ptrRef is not null!");
            ptrRef = (T*) UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), allocator.ToAllocator);
        }

        /// <summary> The internal memory disposal method of VUnsafeRef </summary>
        public static void DisposeMemory(ref T* ptr, AllocatorManager.AllocatorHandle allocator)
        {
            if (ptr != null)
            {
                UnsafeUtility.Free(ptr, allocator.ToAllocator);
            }
            ptr = null;
        }
        
        #endregion
    }
}