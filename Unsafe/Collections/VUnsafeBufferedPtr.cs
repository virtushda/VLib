﻿using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace VLib
{
    /// <summary> Allocation free ptr wrapper. Inherently resistant to corruption or being construct of a view of uninitialized memory. </summary>
    public struct VUnsafeBufferedPtr : IEquatable<VUnsafeBufferedPtr>
    {
        /// <summary> This must be added to a void* or IntPtr, NOT a T* </summary>
        const int BufferedOffset = 37; // Little prime number nudge
        
        [NativeDisableUnsafePtrRestriction]
        readonly unsafe void* ptr;
        /// <summary> The pointer copy that is expected to be at a specific offset. Random memory is extremely unlikely to produce this same offset. </summary>
        [NativeDisableUnsafePtrRestriction] 
        readonly unsafe void* ptrOffset;
        
        readonly unsafe byte* MainPtrWithOffset
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (byte*)ptr + BufferedOffset;
        }

        public unsafe VUnsafeBufferedPtr(void* ptr) : this()
        {
            VCollectionUtils.CheckPtrNonNull(ptr);
            this.ptr = ptr;
            ptrOffset = MainPtrWithOffset;
        }
        
        public readonly unsafe bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // Constructor does not allow null
                /*// Check for offset, equality and NOT NULL
                if (ptr == null)
                    return false;*/
                // OFFSET AND EQUALITY
                return ptrOffset == MainPtrWithOffset;
            }
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void ConditionalCheckValid()
        {
            if (!IsCreated)
                throw new InvalidOperationException("VUnsafeBufferedPtr is not valid!");
        }

        public unsafe void* Ptr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                ConditionalCheckValid();
                return ptr;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => this = new VUnsafeBufferedPtr(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly unsafe bool TryGetPtr(out void* outPtr)
        {
            bool isValid = IsCreated;
            outPtr = isValid ? ptr : null;
            return isValid;
        }

        public unsafe bool Equals(VUnsafeBufferedPtr other) => ptr == other.ptr && ptrOffset == other.ptrOffset;

        public override bool Equals(object obj) => obj is VUnsafeBufferedPtr other && Equals(other);

        public override unsafe int GetHashCode()
        {
            unchecked
            {
                return (((long) ptr).GetHashCode() * 397) ^ ((long)ptrOffset).GetHashCode();
            }
        }

        public static bool operator ==(VUnsafeBufferedPtr left, VUnsafeBufferedPtr right) => left.Equals(right);
        public static bool operator !=(VUnsafeBufferedPtr left, VUnsafeBufferedPtr right) => !left.Equals(right);
    }
}