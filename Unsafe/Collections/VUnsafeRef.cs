#if UNITY_EDITOR || DEVELOPMENT_BUILD || PKSAFE
#define SAFETY
#endif

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
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
    public unsafe struct VUnsafeRef<T> : IEquatable<VUnsafeRef<T>>, IDisposable
        where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        T* ptr;

        internal AllocatorManager.AllocatorHandle m_AllocatorLabel;
        public AllocatorManager.AllocatorHandle AllocatorLabel => m_AllocatorLabel;
        
        /// <summary>
        /// Initializes and returns an instance of VUnsafeRef.
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <param name="options">Whether newly allocated bytes should be zeroed out.</param>
        public VUnsafeRef(AllocatorManager.AllocatorHandle allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            Allocate(allocator, out this, options == NativeArrayOptions.ClearMemory);
        }

        /// <summary>
        /// Initializes and returns an instance of VUnsafeRef.
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <param name="value">The initial value.</param>
        public VUnsafeRef(T value, AllocatorManager.AllocatorHandle allocator)
        {
            Allocate(allocator, out this);
            *ptr = value;
        }

        static void Allocate(AllocatorManager.AllocatorHandle allocator, out VUnsafeRef<T> reference, bool clearMemory = true)
        {
            //CollectionHelper.CheckAllocator(allocator);
            reference = default;
            AllocateMemory(ref reference.ptr, Allocator.Persistent, clearMemory);
            reference.m_AllocatorLabel = allocator;
        }

        public readonly void* Ptr => ptr;
        public readonly T* TPtr => ptr;
        /// <summary>
        /// The value stored in this reference.
        /// </summary>
        /// <param name="value">The new value to store in this reference.</param>
        /// <value>The value stored in this reference.</value>
        public T Value
        {
            readonly get
            {
#if SAFETY
                if (!IsCreated)
                    throw new InvalidOperationException("VUnsafeRef has not been allocated!");
#endif
                return *ptr;
            }
            set
            {
#if SAFETY
                if (!IsCreated)
                    throw new InvalidOperationException("VUnsafeRef has not been allocated!");
#endif
                *ptr = value;
            }
        }

        public readonly ref T ValueRef
        {
            get
            {
#if SAFETY
                if (!IsCreated)
                    throw new InvalidOperationException("VUnsafeRef has not been allocated!");
#endif
                return ref UnsafeUtility.AsRef<T>(ptr);
            }
        }

        /// <summary>
        /// Whether this reference has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this reference has been allocated (and not yet deallocated).</value>
        public readonly bool IsCreated => ptr != null;
        public static implicit operator bool(in VUnsafeRef<T> unsafeRef) => unsafeRef.IsCreated;
        
        /// <summary>Releases all resources (memory and safety handles). Inherently safe, will not throw exception if already disposed.</summary>
        public void Dispose()
        {
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
        public void CopyFrom(VUnsafeRef<T> reference)
        {
            Copy(this, reference);
        }

        /// <summary>
        /// Copy the value of this reference to another reference.
        /// </summary>
        /// <param name="reference">The reference to copy to.</param>
        public readonly void CopyTo(VUnsafeRef<T> reference)
        {
            Copy(reference, this);
        }

        /// <summary>
        /// Returns true if the value stored in this reference is equal to the value stored in another reference.
        /// </summary>
        /// <param name="other">A reference to compare with.</param>
        /// <returns>True if the value stored in this reference is equal to the value stored in another reference.</returns>
        public readonly bool Equals(VUnsafeRef<T> other)
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
        public static bool operator ==(VUnsafeRef<T> left, VUnsafeRef<T> right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Returns true if the values stored in two references are unequal.
        /// </summary>
        /// <param name="left">A reference.</param>
        /// <param name="right">Another reference.</param>
        /// <returns>True if the two values are unequal.</returns>
        public static bool operator !=(VUnsafeRef<T> left, VUnsafeRef<T> right)
        {
            return !left.Equals(right);
        }

        public static implicit operator T(VUnsafeRef<T> reference) => reference.Value;

        /// <summary>
        /// Copies the value of a reference to another reference.
        /// </summary>
        /// <param name="dst">The destination reference.</param>
        /// <param name="src">The source reference.</param>
        public static void Copy(VUnsafeRef<T> dst, VUnsafeRef<T> src)
        {
            if (!dst.IsCreated || !src.IsCreated)
            {
                Debug.LogError("Cannot copy from or to an uncreated VUnsafeRef!");
                return;
            }

            UnsafeUtility.MemCpy(dst.ptr, src.ptr, UnsafeUtility.SizeOf<T>());
        }
        
        #region Alloc Utils

        /// <summary> The internal memory allocation method of VUnsafeRef </summary>
        public static void AllocateMemory(ref T* ptrRef, AllocatorManager.AllocatorHandle allocator, bool clearMemory = true)
        {
            if (ptrRef != null)
                throw new ArgumentException("Incoming ptrRef is not null!");
            ptrRef = (T*) UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), allocator.ToAllocator);
            if (clearMemory)
            {
                if (ptrRef == null)
                {
                    Debug.LogError("PtrRef NULL for VUnsafeBufferedRef!");
                    return;
                }
                UnsafeUtility.MemClear(ptrRef, UnsafeUtility.SizeOf<T>());
            }
        }

        /// <summary> The internal memory disposal method of VUnsafeRef </summary>
        public static void DisposeMemory(ref T* ptr, AllocatorManager.AllocatorHandle allocator)
        {
            if (ptr != null)
                UnsafeUtility.Free(ptr, allocator.ToAllocator);
            ptr = null;
        }
        
        /// <summary> Returns a new VUnsafeRef held in unmanaged memory. </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <param name="options">Whether newly allocated bytes should be zeroed out.</param>
        /// <returns>A pointer to the new VUnsafeRef.</returns>
        public static VUnsafeRef<T>* Create(AllocatorManager.AllocatorHandle allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            var refData = AllocatorManager.Allocate<VUnsafeRef<T>>(allocator);
            *refData = new VUnsafeRef<T>(allocator, options);
            return refData;
        }

        /// <summary> Destroys the VUnsafeRef held in unmanaged memory. </summary>
        /// <param name="refData">The list to destroy.</param>
        public static void Destroy(VUnsafeRef<T>* refData)
        {
            if (refData == null)
            {
                Debug.LogError("Cannot destroy a null ptr!");
                return;
            }
            var allocator = refData->AllocatorLabel;
            refData->Dispose();
            AllocatorManager.Free(allocator, refData);
        }
        
        #endregion
    }
}