#if UNITY_EDITOR
#define EXTRA_SAFE_MODE
#endif

using System;
using System.Diagnostics;

namespace VLib
{
    /// <summary> This struct is specifically made for <see cref="VUnsafeParallelPinnedMemory{T}"/> and is not appropriate for ANY other use. </summary>
    public readonly unsafe struct PinnedMemoryElement<T> : IEquatable<PinnedMemoryElement<T>>
        where T : unmanaged
    {
        readonly int listIndex;
#if EXTRA_SAFE_MODE
        // Use a buffered ref (stores a copy of the pointer offset by a constant) to detect possible cases where this value type is pointing at garbage memory.
        // The pointer may not be null in such a case, but will be pointing to a random position in memory and can cause a very low-level crash.
        readonly VUnsafeBufferedRef<T> defensivePointer;
        readonly T* tPtr => defensivePointer.TPtr;
#else
        readonly T* tPtr;
#endif
        
#if EXTRA_SAFE_MODE
        public bool IsCreated => defensivePointer.IsValid;
#else
        public bool IsCreated => tPtr != null;
#endif

        public int ListIndex => listIndex;
        
        public T Value
        {
            get
            {
                CheckCreated();
                return *tPtr;
            }
            set
            {
                CheckCreated();
                *tPtr = value;
            }
        }
        
        public ref T Ref
        {
            get
            {
                CheckCreated();
                return ref *tPtr;
            }
        }
        
        public T* Ptr
        {
            get
            {
                CheckCreated();
                return tPtr;
            }
        }
        
        internal PinnedMemoryElement(int listIndex, T* tPtr)
        {
            this.listIndex = listIndex;
#if EXTRA_SAFE_MODE
            defensivePointer = new VUnsafeBufferedRef<T>(tPtr);
#else
            this.tPtr = tPtr;
#endif
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void CheckCreated()
        {
            if (!IsCreated)
                throw new InvalidOperationException("PinnedMemory is not created!");
        }

        public override string ToString() => $"PinnedMemoryElement: {listIndex} - {Value}";

        public bool Equals(PinnedMemoryElement<T> other)
        {
#if EXTRA_SAFE_MODE
            var ptrsEqual = defensivePointer.Equals(other.defensivePointer);
            if (!ptrsEqual)
                return false;
            if (listIndex != other.listIndex)
                throw new InvalidOperationException("PinnedMemoryElement list index mismatch!");
            return true;
#else
            return tPtr == other.tPtr;
#endif
        }

        public override bool Equals(object obj) => obj is PinnedMemoryElement<T> other && Equals(other);

        public static bool operator ==(PinnedMemoryElement<T> left, PinnedMemoryElement<T> right) => left.Equals(right);
        public static bool operator !=(PinnedMemoryElement<T> left, PinnedMemoryElement<T> right) => !left.Equals(right);
        
        public override int GetHashCode() => (int) tPtr;
    }
}