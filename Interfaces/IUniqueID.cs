using System;

namespace VLib
{
    public interface IUniqueID8U// : IComparable<byte>
    {
        byte UniqueID { get; }
    }
    
    public interface IUniqueID16U// : IComparable<ushort>
    {
        ushort UniqueID { get; }
    }
    
    public interface IUniqueID32U// : IComparable<uint>
    {
        uint UniqueID { get; }
    }

    public interface IUniqueID64// : IComparable<long>
    {
        long UniqueID { get; }
    }
    
    public interface IUniqueID64U// : IComparable<ulong>
    {
        ulong UniqueID { get; }
    }
}