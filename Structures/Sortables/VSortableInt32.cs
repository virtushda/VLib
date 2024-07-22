using System;
using Unity.Mathematics;

namespace VLib
{
    public struct VSortableInt32 : IVSortableValue<int>, IComparable<VSortableInt32>
    {
        public int value;

        public int Value
        {
            get => value;
            set => this.value = value;
        }

        public bool Equals(int other) => value == other;

        public int CompareTo(int other) => value.CompareTo(other);

        public int CompareTo(VSortableInt32 other) => value.CompareTo(other.value);
    }

    //Useful for iterable dictionary-like behaviour in a high-perf circumstance
    public struct VSortableInt32With<T> : IVSortableValue<int>, IComparable<VSortableInt32With<T>>, IEquatable<VSortableInt32With<T>>
        where T : unmanaged
    {
        public int value;

        public int Value
        {
            get => value;
            set => this.value = value;
        }

        public T attachedValue;

        public VSortableInt32With(int value, T attachedValue)
        {
            this.value = value;
            this.attachedValue = attachedValue;
        }

        public bool Equals(int other) => value == other;

        public bool Equals(VSortableInt32With<T> other) => value == other.value;

        public int CompareTo(int other) => value.CompareTo(other);

        public int CompareTo(VSortableInt32With<T> other) => value.CompareTo(other.value);
    }
}