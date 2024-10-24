using System;
using System.Diagnostics;
using Unity.Collections.LowLevel.Unsafe;

namespace VLib
{
    public readonly struct VSafetyHandle : IDisposable, IEquatable<VSafetyHandle>, IComparable<VSafetyHandle>
    {
        [NativeDisableUnsafePtrRestriction]
        internal readonly PinnedMemoryElement<ulong> truthLocation;
        internal readonly ulong safetyIDCopy;
        
        public bool IsValid => truthLocation.IsCreated && truthLocation.Value == safetyIDCopy;
        
        internal VSafetyHandle(PinnedMemoryElement<ulong> truthLocation)
        {
            this.truthLocation = truthLocation;
            safetyIDCopy = truthLocation.Value;
        }
        
        public void Dispose() => TryDispose(this);
        
        public bool TryDispose() => TryDispose(this);

        public static VSafetyHandle Create() => VSafetyHandleManager.InternalMemoryField.Data.Create();
        
        public static bool TryDispose(VSafetyHandle handle) => VSafetyHandleManager.InternalMemoryField.Data.TryDestroy(handle);

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void ConditionalCheckValid()
        {
            if (!IsValid)
                throw new InvalidOperationException("VSafetyHandle is not created!");
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