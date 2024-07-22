using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace VLib
{
    /// <summary> <para> A way to refer to an element in the list that won't break completely if the list is deallocated or grows. However, this will break if you remove elements from the list. </para>
    /// <para> This is meant to be used in a buffer pattern where "removals" are done by recycling indices and 'defaulting' list slots. </para> </summary>
    public readonly unsafe struct VUnsafeBufferListElement<T>
        where T : unmanaged
    {
        public readonly VUnsafeBufferList<T> list;
        public readonly int index;
        
        public T ValueCopy
        {
            get
            {
                if (!TryGetValue(out var value))
                {
                    // Report why
                    if (!list.IsCreated)
                        UnityEngine.Debug.LogError("VUnsafeListElementRef: List is not created.");
                    if (!list.IndexValid(index))
                        UnityEngine.Debug.LogError($"VUnsafeListElementRef: Index {index} is out of range in VUnsafeList of '{list.Length}' Length.");
                    return default;
                }
                return value;
            }
        }

        /// <summary> Don't hang onto this for long, will be unsafe! </summary>
        public T* ValuePtr
        {
            get
            {
                if (!TryGetValuePtr(out var valuePtr))
                {
                    // Report why
                    if (!list.IsCreated)
                        throw new UnityException("VUnsafeListElementRef: List is not created.");
                    if (!list.IndexValid(index))
                        throw new UnityException($"VUnsafeListElementRef: Index {index} is out of range in VUnsafeList of '{list.Length}' Length.");
                    throw new UnityException("VUnsafeListElementRef: Failed to get value ref!");
                }
                return valuePtr;
            }
        }

        public bool IsValid => list.IsCreated && list.IndexValid(index);

        public VUnsafeBufferListElement(VUnsafeBufferList<T> list, int index)
        {
            this.list = list;
            this.index = index;
        }

        public bool TryGetValue(out T value)
        {
            value = default;
            return list.IsCreated && list.TryGetValue(index, out value);
        }
        
        public bool TryGetValuePtr(out T* value)
        {
            value = default;
            return list.TryGetElementPtr(index, out value);
        }
    }
    
    public static class VUnsafeBufferListElementRefExtensions
    {
        public static VUnsafeBufferListElement<T> GetElementRef<T>(this VUnsafeBufferList<T> list, int index)
            where T : unmanaged
        {
            return new VUnsafeBufferListElement<T>(list, index);
        }
    }
}