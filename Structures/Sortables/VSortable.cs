using System;

namespace VLib
{
    public interface IVSortable<T> : IComparable<T>, IEquatable<T>
    {
        T Value { get; set; }
    }
    
    public interface IVSortableValue<T> : IVSortable<T>
        where T : unmanaged, IEquatable<T> { }
}