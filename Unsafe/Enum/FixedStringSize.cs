using System;

namespace VLib
{
    [Flags]
    public enum FixedStringSize : byte
    {
        Unknown = 0,
        _32 = 1,
        _64 = 1 << 1,
        _128 = 1 << 2,
        _512 = 1 << 3,
        _4096 = 1 << 4
    }
}