using System;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace VLib
{
    public readonly unsafe struct IntPtr<T> where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction] public readonly T* Ptr;
        
        public IntPtr(T* ptr)
        {
            VCollectionUtils.CheckPtrNonNull(ptr);
            Ptr = ptr;
        }

        public IntPtr(IntPtr ptr)
        {
            VCollectionUtils.CheckPtrNonNull(ptr);
            Ptr = (T*) ptr.ToPointer();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntPtr AsIntPtr() => new (Ptr);

        public static implicit operator IntPtr(IntPtr<T> ptrT) => ptrT.AsIntPtr();
        public static IntPtr<T> operator +(IntPtr<T> ptrT, int offset) => new (ptrT.Ptr + offset);
        public static IntPtr<T> operator -(IntPtr<T> ptrT, int offset) => new (ptrT.Ptr - offset);
    }
    
    public static class IntPtrExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe IntPtr<T> AsTyped<T>(this IntPtr ptr) 
            where T : unmanaged
        {
            return new IntPtr<T>((T*) ptr.ToPointer());
        }
    }
}