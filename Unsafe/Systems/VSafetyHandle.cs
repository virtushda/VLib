using System;
using System.Diagnostics;
using System.Threading;
using Unity.Collections.LowLevel.Unsafe;

namespace VLib
{
    /// <summary> A thread-safe safety mechanism for unmanaged memory. </summary>
    public readonly struct VSafetyHandle : IDisposable, IEquatable<VSafetyHandle>, IComparable<VSafetyHandle>
    {
        [NativeDisableUnsafePtrRestriction]
        internal readonly PinnedMemoryElement<ulong> truthLocation;
        internal readonly ulong safetyIDCopy;
        
        public static implicit operator ulong(VSafetyHandle handle)
        {
            //handle.ConditionalCheckValid();
            return handle.safetyIDCopy;
        }

        public bool IsValid => truthLocation.IsCreated && truthLocation.Value == safetyIDCopy;
        
        internal VSafetyHandle(PinnedMemoryElement<ulong> truthLocation)
        {
            this.truthLocation = truthLocation;
            safetyIDCopy = truthLocation.Value;
        }
        
        public void Dispose() => VSafetyHandleManager.InternalMemoryField.Data.TryDestroy(this);
        
        public bool TryDispose() => VSafetyHandleManager.InternalMemoryField.Data.TryDestroy(this);

        public static VSafetyHandle Create() => VSafetyHandleManager.InternalMemoryField.Data.Create();

        /// <summary> A concurrent safe way to check that the handle is valid and invalidate it at the same time. Part of disposal. </summary>
        internal bool TryInvalidate()
        {
            if (!IsValid)
                return false;
            // View as long so we can use an atomic call
            ref var dataAsLong = ref UnsafeUtility.As<ulong, long>(ref truthLocation.Ref);
            // Swap the value to default atomically
            var previousAsLong = Interlocked.Exchange(ref dataAsLong, default);
            // Check if the previous value was the same as the safety ID, otherwise we're the second caller and the handle is already invalidated
            return UnsafeUtility.As<long, ulong>(ref previousAsLong) == safetyIDCopy;
        }

        /// <summary> Assert that the handle is valid. </summary>
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void ConditionalCheckValid()
        {
            if (!IsValid)
                throw new InvalidOperationException("VSafetyHandle is not valid when it should be!");
        }
        
        /// <summary> Assert that the handle is not valid. </summary>
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void ConditionalCheckNotValid()
        {
            if (IsValid)
                throw new InvalidOperationException("VSafetyHandle is valid when it shouldn't be!");
        }

        public override string ToString() => $"VSafetyHandle: ID:{safetyIDCopy}|Index:{truthLocation.ListIndex}";

        public bool Equals(VSafetyHandle other) => safetyIDCopy == other.safetyIDCopy && truthLocation == other.truthLocation;
        public override bool Equals(object obj) => obj is VSafetyHandle other && Equals(other);
        
        public static bool operator ==(VSafetyHandle left, VSafetyHandle right) => left.Equals(right);
        public static bool operator !=(VSafetyHandle left, VSafetyHandle right) => !left.Equals(right);
        
        public override int GetHashCode() => truthLocation.GetHashCode() ^ safetyIDCopy.GetHashCode();

        public int CompareTo(VSafetyHandle other) => safetyIDCopy.CompareTo(other.safetyIDCopy);
    }
}