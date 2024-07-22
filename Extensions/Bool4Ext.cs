using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace VLib
{
    public static class Bool4Ext
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountTrue(this bool4 value)
        {
            int4 count = (int4)value;
            return math.csum(count);
        }
    }
}