using System;
using System.Diagnostics;
using Unity.Collections.LowLevel.Unsafe;
using VLib.Unsafe.Utility;

namespace VLib
{
    /// <summary> This structure points at existing memory and uses a <see cref="VSafetyHandle"/> to ensure that disposal is recognized by all copies. <br/>
    /// This is designed to be compatible with an ECS pattern, where slots in a buffer are handed out. <br/>
    /// This can only be used in play mode, but is effectively a safe pointer. </summary>
    public struct SafePtr<T> : IAllocating, IEquatable<SafePtr<T>>, IComparable<SafePtr<T>>
        where T : unmanaged
    {
        // Data
        [NativeDisableUnsafePtrRestriction]
        unsafe T* ptr;
        // Security
        readonly VSafetyHandle safetyHandle;

        public readonly bool IsCreated => safetyHandle.IsValid;
        public static implicit operator bool(SafePtr<T> safePtr) => safePtr.IsCreated;
        
        public readonly ulong SafetyID => safetyHandle.safetyIDCopy;
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public readonly void ConditionalCheckValid() => safetyHandle.ConditionalCheckValid();
        
        public T ValueCopy
        {
            readonly get
            {
                safetyHandle.ConditionalCheckValid();
                return ValueRefUnsafe;
            }
            set
            {
                safetyHandle.ConditionalCheckValid();
                ValueRefUnsafe = value;
            }
        }

        public readonly ref T ValueRef
        {
            get
            {
                safetyHandle.ConditionalCheckValid();
                return ref ValueRefUnsafe;
            }
        }
        
        readonly unsafe ref T ValueRefUnsafe => ref *ptr;

        public unsafe SafePtr(T* ptr)
        {
            VCollectionUtils.CheckPtrNonNull(ptr);
            this.ptr = ptr;
            safetyHandle = VSafetyHandle.Create();
        }

        /// <summary> Construct a safe ptr directly from an existing pointer and safety handle. </summary>
        public unsafe SafePtr(T* ptr, VSafetyHandle safetyHandle)
        {
            // Pointer validity state much match safety handle validity state
            BurstAssert.True(ptr != null == safetyHandle.IsValid);
            //VCollectionUtils.CheckPtrNonNull(ptr);
            //safetyHandle.ConditionalCheckValid();
            this.ptr = ptr;
            this.safetyHandle = safetyHandle;
        }

        public static unsafe SafePtr<T> Create(ref T reference) => new((T*) UnsafeUtility.AddressOf(ref reference));

        /// <summary> This disposes the reference for ALL holders. If you want to invalidate your reference only, just set it to default. </summary>
        public void Dispose() => TryDispose();

        /// <summary> This disposes the reference for ALL holders. If you want to invalidate your reference only, just set it to default. </summary>
        public bool TryDispose() => safetyHandle.TryDispose();

        public bool TryGetValue(out T value)
        {
            if (!IsCreated)
            {
                value = default;
                return false;
            }
            value = ValueRefUnsafe;
            return true;
        }

        public readonly ref T TryGetRef(out bool success)
        {
            if (!IsCreated)
            {
                success = false;
                return ref VUnsafeUtil.NullRef<T>();
            }
            success = true;
            return ref ValueRefUnsafe;
        }

        public override string ToString() => $"SafePtr:{typeof(T)}|{safetyHandle}";

        public bool Equals(SafePtr<T> other) => safetyHandle.Equals(other.safetyHandle);
        public override bool Equals(object obj) => obj is SafePtr<T> other && Equals(other);

        public static bool operator ==(SafePtr<T> left, SafePtr<T> right) => left.Equals(right);
        public static bool operator !=(SafePtr<T> left, SafePtr<T> right) => !left.Equals(right);

        public override int GetHashCode() => safetyHandle.GetHashCode();

        public int CompareTo(SafePtr<T> other)
        {
            bool isCreated = IsCreated;
            bool otherIsCreated = other.IsCreated;
            if (!isCreated || !otherIsCreated)
            {
                if (isCreated)
                    return 1;
                if (otherIsCreated)
                    return -1;
                return 0;
            }
            return safetyHandle.CompareTo(other.safetyHandle);
        }
    }

    public static class SafePtrExt
    {
        /*/// <summary> Disposes internal IDisposable, sets internal value reference to 'default', disposes the safeptr. Does not set the safeptr reference itself to default, no access on a copy. </summary>
        public static void DisposeSelfAndDefaultInternalRef<T>(this SafePtr<T> safePtr) 
            where T : unmanaged, IDisposable
        {
            if (safePtr.IsCreated)
            {
                // INTERNAL VALUE
                ref var internalValueRef = ref safePtr.TryGetRef(out var success);
                if (success)
                {
                    try
                    {
                        internalValueRef.Dispose();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to dispose internal value! {e}");
                    }
                    internalValueRef = default;
                }
                else
                    Debug.LogError("Failed to get value from ref struct!");
                
                // SafePtr ITSELF
                safePtr.Dispose();
            }
        }

        /// <summary> Disposes internal IDisposable, sets internal value reference to 'default', disposes the SafePtr, sets the reference to the SafePtr itself to 'default'. </summary>
        public static void DisposeFullToDefault<T>(ref this SafePtr<T> SafePtr) 
            where T : unmanaged, IDisposable
        {
            SafePtr.DisposeSelfAndDefaultInternalRef();
            SafePtr = default;
        }*/
    }
}