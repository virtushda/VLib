using System;
using System.Diagnostics;

namespace VLib
{
    /// <summary> This struct is specifically made for <see cref="VUnsafeParallelPinnedMemory{T}"/> and is not appropriate for ANY other use. </summary>
    public readonly unsafe struct PinnedMemoryElement<T> : IEquatable<PinnedMemoryElement<T>>
        where T : unmanaged
    {
        readonly int listIndex;
        readonly T* tPtr;
        
        public bool IsCreated => tPtr != null;

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
        
        internal PinnedMemoryElement(int listIndex, T* tPtr)
        {
            this.listIndex = listIndex;
            this.tPtr = tPtr;
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void CheckCreated()
        {
            if (!IsCreated)
                throw new InvalidOperationException("PinnedMemory is not created!");
        }

        public override string ToString() => $"PinnedMemoryElement: {listIndex} - {Value}";

        public bool Equals(PinnedMemoryElement<T> other) => tPtr == other.tPtr;
        public override bool Equals(object obj) => obj is PinnedMemoryElement<T> other && Equals(other);

        public static bool operator ==(PinnedMemoryElement<T> left, PinnedMemoryElement<T> right) => left.Equals(right);
        public static bool operator !=(PinnedMemoryElement<T> left, PinnedMemoryElement<T> right) => !left.Equals(right);
        
        public override int GetHashCode() => (int) tPtr;
    }
}