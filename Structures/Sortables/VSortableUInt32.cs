using System;

namespace VLib
{
    public struct VSortableUInt32 : IVSortableValue<uint>, IComparable<VSortableUInt32>
    {
        public uint value;

        public uint Value
        {
            get => value;
            set => this.value = value;
        }

        public bool Equals(uint other) => value == other;

        public int CompareTo(uint other) => value.CompareTo(other);

        public int CompareTo(VSortableUInt32 other) => value.CompareTo(other.value);
    }

    //Useful for iterable dictionary-like behaviour in a high-perf circumstance
    public struct VSortableUInt32With<T> : IVSortableValue<uint>, IComparable<VSortableUInt32With<T>>, IEquatable<VSortableUInt32With<T>>
    {
        public uint value;

        public uint Value
        {
            get => value;
            set => this.value = value;
        }

        public T attachedValue;

        public VSortableUInt32With(uint value, T attachedValue)
        {
            this.value = value;
            this.attachedValue = attachedValue;
        }

        public bool Equals(uint other) => value == other;

        public bool Equals(VSortableUInt32With<T> other) => value == other.value;

        public int CompareTo(uint other) => value.CompareTo(other);

        public int CompareTo(VSortableUInt32With<T> other) => value.CompareTo(other.value);
    }
}