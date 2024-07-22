using System;

namespace VLib
{
    public interface ICmdPropStruct : ICmdTransform
    {
        int SizeOf { get; }
    }
    
    /*public interface ICmdPropStructIDable<T> : ICmdPropStruct, IUniqueID16, IComparable<ICmdPropStructIDable<T>>
        where T : unmanaged, IComparable<T>, IEquatable<T> { }
    
    public struct CmdPropStructIDableComparison<T, U> : IComparison<ushort>
        where */
}