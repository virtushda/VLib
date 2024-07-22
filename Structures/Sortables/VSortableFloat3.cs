using System;
using Unity.Mathematics;

namespace VLib
{
    public struct VSortableFloat3 : IVSortableValue<float3>, IComparable<VSortableFloat3>
    {
        public float3 value;
        public float3 Value { get => value; set => this.value = value; }

        public bool Equals(float3 other) => math.all(value == other);

        public int CompareTo(float3 other) => value.CompareTo(other);

        public int CompareTo(VSortableFloat3 other) => value.CompareTo(other.value);
    }
    
    public struct VSortableFloat3With<T> : IVSortableValue<float3>, IComparable<VSortableFloat3With<T>>, IEquatable<VSortableFloat3With<T>>
        where T : unmanaged
    {
        public float3 value;
        public float3 Value { get => value; set => this.value = value; }

        public T attachedValue;

        public VSortableFloat3With(float3 value, T attachedValue)
        {
            this.value = value;
            this.attachedValue = attachedValue;
        }

        public bool Equals(float3 other) => math.all(value == other);
        
        public bool Equals(VSortableFloat3With<T> other) => math.all(value == other.value);
 
        public int CompareTo(float3 other) => value.CompareTo(other);

        public int CompareTo(VSortableFloat3With<T> other) => value.CompareTo(other.value);
    }
}