using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace VLib
{
    public static class RandomExt
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Choose<T>(this ref Random rand, T a, T b) => rand.NextBool() ? a : b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Lerp(this ref Random rand, float a, float b) => math.lerp(a, b, rand.NextFloat());
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Variate(this ref Random rand, float x, float variance) => rand.NextFloat(x - variance, x + variance);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Variate(this float x, ref Random rand, float variance) => rand.NextFloat(x - variance, x + variance);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Chance(this ref Random rand, float chance) => chance > 0f && rand.NextFloat() < chance;
    }
}