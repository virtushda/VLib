using System;
using Unity.Burst;

namespace VLib.Threading
{
    /// <summary> A way to identify a thread in the complex context. <br/>
    /// Fetch using <see cref="VThreadUtil"/>. </summary>
    public readonly struct VThreadID : IEquatable<VThreadID>, IComparable<VThreadID>
    {
        public enum ThreadIDType : byte
        {
            Unknown,
            ManagedThreadID,
            JobsUtilityThreadIndex,
            Burst
        }
        
        public readonly ThreadIDType Type;
        public readonly int ID;
        
        public bool IsValidType => Type != ThreadIDType.Unknown;
        
        public VThreadID(ThreadIDType type, int id)
        {
            Type = type;
            ID = id;
        }

        public bool Equals(VThreadID other) => ID == other.ID && Type == other.Type;

        public int CompareTo(VThreadID other)
        {
            var typeComparison = ((byte)Type).CompareTo((byte)other.Type);
            if (typeComparison != 0)
                return typeComparison;
            return ID.CompareTo(other.ID);
        }
        
        public override int GetHashCode() => HashCode.Combine((byte)Type, ID);

        [BurstDiscard]
        public override string ToString() => $"VThreadID:{Type}|{ID}";
    }
}