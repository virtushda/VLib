using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace VLib
{
    static class Int4Ext
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AnyComponentMatches(this int4 lhs, int valueToMatch)
        {
            return math.any(lhs == (int4)valueToMatch);
        }
    }
}