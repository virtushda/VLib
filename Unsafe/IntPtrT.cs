using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VLib
{
    public readonly unsafe struct IntPtr<T> where T : unmanaged
    {
        public readonly T* Ptr;
        public IntPtr(T* ptr)
        {
            Ptr = ptr;
        }
        public static implicit operator IntPtr(IntPtr<T> ptrT) => new (ptrT.Ptr);
    }
}
