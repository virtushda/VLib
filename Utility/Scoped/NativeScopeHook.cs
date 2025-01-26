using System;
using Unity.Collections;
using UnityEngine;

namespace VLib.Scoped
{
    /// <summary> UNTESTED <br/>
    /// A dynamic using statement to support methods which can dynamically allocate IDisposables. </summary>
    public struct NativeScopeHook<T> : IDisposable 
        where T : unmanaged, IDisposable
    {
        VUnsafeRef<Holder> reference;

        struct Holder
        {
            T value;
            bool hasValue;
            
            public bool HasValue => hasValue;

            public bool TryHook(T value)
            {
                if (hasValue)
                    return false;
                this.value = value;
                hasValue = true;
                return true;
            }
            
            public void Dispose()
            {
                if (!hasValue)
                    return;
                value.DisposeRefToDefault();
                hasValue = false;
            }
        }
        
        NativeScopeHook(VUnsafeRef<Holder> reference) => this.reference = reference;

        public void Dispose()
        {
            if (!reference.IsCreated)
            {
                Debug.LogError("NativeUsingHook disposed twice!");
                return;
            }
            reference.ValueRef.Dispose();
            reference.Dispose();
        }
        
        public static NativeScopeHook<T> Alloc(Allocator allocator = Allocator.Temp) => new(new VUnsafeRef<Holder>(default, allocator));
    }

}