using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace VLib
{
    public static class RandomExt
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Choose<T>(this ref Random rand, T a, T b) => rand.NextBool() ? a : b;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Choose<T>(this ref Random rand, params T[] options)
        {
            if (options == null || options.Length == 0)
                throw new System.ArgumentException("Options array cannot be null or empty", nameof(options));
            return options[rand.NextInt(0, options.Length)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Lerp(this ref Random rand, float a, float b) => math.lerp(a, b, rand.NextFloat());
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Variate(this ref Random rand, float x, float variance) => rand.NextFloat(x - variance, x + variance);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Variate(this float x, ref Random rand, float variance) => rand.NextFloat(x - variance, x + variance);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color Variate(this ref Random rand, Color color, float variance) => new(
            math.saturate(rand.Variate(color.r, variance)), 
            math.saturate(rand.Variate(color.g, variance)), 
            math.saturate(rand.Variate(color.b, variance)),
            color.a);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Chance(this ref Random rand, float chance01) => chance01 > 0f && rand.NextFloat() < chance01;
        
        public static bool ChancePerSecond(this ref Random rand, float chancePerSecond, float deltaTime)
        {
            if (chancePerSecond <= 0f || deltaTime <= 0f)
                return false;

            if (chancePerSecond >= 1f)
                return true;

            var stepChance = 1f - math.pow(1f - chancePerSecond, deltaTime);
            return rand.NextFloat() < stepChance;
        }
    }
}