using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace VLib
{
    public static class LongExt
    {
        public static string AsTimeToPrint(this long seconds) => ((double) seconds).AsTimeToPrint();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUnityMathematicsHash(this long value) => (uint) (value ^ (value >> 32));
    }
}