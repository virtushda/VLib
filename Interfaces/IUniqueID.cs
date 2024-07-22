using System;

namespace VLib
{
    public interface IUniqueID8U : IComparable<byte>
    {
        byte ID { get; set; }
    }
    
    public interface IUniqueID16U : IComparable<ushort>
    {
        ushort ID { get; set; }
    }
    
    public interface IUniqueID32U : IComparable<uint>
    {
        uint ID { get; set; }
    }

    public interface IUniqueID64 : IComparable<long>
    {
        long ID { get; set; }
    }
    
    public interface IUniqueID64U : IComparable<ulong>
    {
        ulong ID { get; set; }
    }
}