using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Debug = UnityEngine.Debug;

namespace VLib
{
    /// <summary> Essentially an unsafe lower-level version of <see cref="NativeReference{T}"/>
    /// Double buffers its pointer to be able to detect memory corruption! </summary>
    /// <typeparam name="T"></typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
    public unsafe struct VUnsafeBufferedRef<T> : IEquatable<VUnsafeBufferedRef<T>>
        where T : unmanaged
    {
        /// <summary> This must be added to a void* or IntPtr, NOT a T* </summary>
        const int BufferedOffset = 337; // Little prime number nudge
        
        [NativeDisableUnsafePtrRestriction]
        T* ptr;
        /// <summary> The pointer copy that is expected to be at a specific offset. Random memory is extremely unlikely to produce this same offset. </summary>
        [NativeDisableUnsafePtrRestriction]
        IntPtr ptrBuffered;

        public T* TPtr
        {
            get
            {
                if (TryGetTPtr(out _))
                    return ptr;
                throw new InvalidOperationException("Failed to fetch double buffered pointer! Potential corruption?");
            }
            private set // 
            {
                // Set buffered pointer
                ptr = value;
                if (value is not null)
                    ptrBuffered = ((IntPtr) value) + BufferedOffset;
                else
                    ptrBuffered = IntPtr.Zero;
            }
        }

        public T* TPtrNoSafety
        {
            get
            {
                ConditionalCheckValid();
                return ptr;
            }
        }

        /// <summary> Try grab the ptr without triggering any log errors. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetTPtr(out T* tPtr)
        {
            if (IsValid)
            {
                tPtr = ptr;
                return true;
            }

            tPtr = default;
            return false;
        }

        internal AllocatorManager.AllocatorHandle m_AllocatorLabel;

        /// <summary>
        /// Initializes and returns an instance of VUnsafeRef.
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <param name="options">Whether newly allocated bytes should be zeroed out.</param>
        public VUnsafeBufferedRef(AllocatorManager.AllocatorHandle allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            Allocate(allocator, out this);
            if (options == NativeArrayOptions.ClearMemory)
            {
                var ptr = TPtr;
                if (ptr == null)
                {
                    Debug.LogError("Ptr NULL for VUnsafeBufferedRef!");
                    return;
                }
                UnsafeUtility.MemClear(TPtr, UnsafeUtility.SizeOf<T>());
            }
        }

        /// <summary>
        /// Initializes and returns an instance of VUnsafeRef.
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <param name="value">The initial value.</param>
        public VUnsafeBufferedRef(T value, AllocatorManager.AllocatorHandle allocator)
        {
            Allocate(allocator, out this);
            Value = value;
        }

        /// <summary> Use this collection as a wrapper for pointer safety, don't dispose it! </summary>
        public VUnsafeBufferedRef(T* value)
        {
#if UNITY_EDITOR
            if (value is null)
                Debug.LogError("VUnsafeBufferedRef cannot be initialized with a null pointer!");
            if (value == (T*) IntPtr.Zero)
                Debug.LogError("VUnsafeBufferedRef cannot be initialized with a zero pointer!");
#endif
            
            this = default;
            m_AllocatorLabel = Allocator.None;
            TPtr = value;
        }

        /// <summary> This type does not ALWAYS allocate, it can be used as a pointer safety wrapper! </summary>
        static void Allocate(AllocatorManager.AllocatorHandle allocator, out VUnsafeBufferedRef<T> reference)
        {
            if (allocator.ToAllocator <= Allocator.None)
                throw new ArgumentException("Allocator must not be Allocator.None or Allocator.Invalid!");
            reference = default;
            reference.TPtr = (T*) UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), allocator.ToAllocator);
            reference.m_AllocatorLabel = allocator;
        }

        //public void* Ptr => ptr;
        //public T* TPtr => ptr;
        
        /// <summary>
        /// The value stored in this reference.
        /// </summary>
        /// <param name="value">The new value to store in this reference.</param>
        /// <value>The value stored in this reference.</value>
        public T Value
        {
            get => *TPtr;
            set => *TPtr = value;
        }
        
        public ref T ValueRef => ref UnsafeUtility.AsRef<T>(TPtr);

        /// <summary> Does not check validity, just checks that internal pointer isn't null. Corruption can make this return true! </summary>
        public bool IsCreated => ptr != null;

        /// <summary> Whether this reference IS VALID and has been allocated (and not yet deallocated). <br/>
        /// WARNING: This can return true on a wrapper, but does not mean that this type is allocating memory. <br/> </summary>
        /// <value>True if this reference has been allocated (and not yet deallocated).</value>
        public bool IsValid
        {
            get
            {
                // Check for offset, equality and NOT NULL
                
                // NOT NULL
                if (!IsCreated)
                    return false;
                
                // OFFSET AND EQUALITY
                var offsetPtr = ((IntPtr) ptr) + BufferedOffset;
                // Offset should protect from needing an equals ZERO check
                return offsetPtr == ptrBuffered;
            }
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void ConditionalCheckValid()
        {
            if (!IsValid)
                throw new InvalidOperationException("VUnsafeBufferedRef is not valid!");
        }

        /// <summary> NOTE: Extension <see cref="NativeCollectionExtUnsafe.DisposeRefToDefault{T}"/> is ideal to use.
        /// Releases all resources (memory and safety handles). Inherently safe, will not throw exception if already disposed.</summary>
        public void DisposeUnsafe()
        {
            if (TryGetTPtr(out var tPtr) && tPtr != null)
            {
                // A wrapper or already disposed?
                if (m_AllocatorLabel.ToAllocator is Allocator.Invalid or Allocator.None)
                {
                    Debug.LogError("The NativeArray can not be Disposed because it was not allocated with a valid allocator. Are you disposing twice? Or disposing a wrapper?");
                    return;
                }
                UnsafeUtility.Free(tPtr, m_AllocatorLabel.ToAllocator);
                m_AllocatorLabel = default;
                TPtr = null;
            }
        }

        /// <summary>
        /// Copy the value of another reference to this reference.
        /// </summary>
        /// <param name="reference">The reference to copy from.</param>
        public void CopyFrom(VUnsafeBufferedRef<T> reference)
        {
            Copy(this, reference);
        }

        /// <summary>
        /// Copy the value of this reference to another reference.
        /// </summary>
        /// <param name="reference">The reference to copy to.</param>
        public void CopyTo(VUnsafeBufferedRef<T> reference)
        {
            Copy(reference, this);
        }

        /// <summary>
        /// Returns true if the value stored in this reference is equal to the value stored in another reference.
        /// </summary>
        /// <param name="other">A reference to compare with.</param>
        /// <returns>True if the value stored in this reference is equal to the value stored in another reference.</returns>
        public bool Equals(VUnsafeBufferedRef<T> other)
        {
            var valid = IsValid;
            var otherValid = other.IsValid;
            if (valid != otherValid)
                return false;
            // Now valid == otherValid
            if (!valid)
                return true;
            // Now both are definitely valid
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
        public static bool operator ==(VUnsafeBufferedRef<T> left, VUnsafeBufferedRef<T> right)
        {
            return left.Equals(right);
        }
        /// <summary>
        /// Returns true if the values stored in two references are unequal.
        /// </summary>
        /// <param name="left">A reference.</param>
        /// <param name="right">Another reference.</param>
        /// <returns>True if the two values are unequal.</returns>
        public static bool operator !=(VUnsafeBufferedRef<T> left, VUnsafeBufferedRef<T> right)
        {
            return !left.Equals(right);
        }

        public static implicit operator T(VUnsafeBufferedRef<T> reference) => reference.Value;

        /// <summary>
        /// Copies the value of a reference to another reference.
        /// </summary>
        /// <param name="dst">The destination reference.</param>
        /// <param name="src">The source reference.</param>
        public static void Copy(VUnsafeBufferedRef<T> dst, VUnsafeBufferedRef<T> src)
        {
            if (!dst.IsValid || !src.IsValid)
                return;
            UnsafeUtility.MemCpy(dst.ptr, src.ptr, UnsafeUtility.SizeOf<T>());
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