using System;
using Unity.Mathematics;

namespace VLib
{
    public struct VSortableInt16 : IVSortableValue<short>, IComparable<VSortableInt16>
    {
        public short value;

        public short Value
        {
            get => value;
            set => this.value = value;
        }

        public bool Equals(short other) => value == other;

        public int CompareTo(short other) => value.CompareTo(other);

        public int CompareTo(VSortableInt16 other) => value.CompareTo(other.value);
    }

    //Useful for iterable dictionary-like behaviour in a high-perf circumstance
    public struct VSortableInt16With<T> : IVSortableValue<short>, IComparable<VSortableInt16With<T>>, IEquatable<VSortableInt16With<T>>
        where T : unmanaged
    {
        public short value;

        public short Value
        {
            get => value;
            set => this.value = value;
        }

        public T attachedValue;

        public VSortableInt16With(short value, T attachedValue)
        {
            this.value = value;
            this.attachedValue = attachedValue;
        }

        public bool Equals(short other) => value == other;

        public bool Equals(VSortableInt16With<T> other) => value == other.value;

        public int CompareTo(short other) => value.CompareTo(other);

        public int CompareTo(VSortableInt16With<T> other) => value.CompareTo(other.value);
    }
}