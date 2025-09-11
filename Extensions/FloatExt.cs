using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace VLib
{
    public static class FloatExt
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ToByteFromFloat01(this float value_min0_max1) => (byte) math.round(math.saturate(value_min0_max1) * byte.MaxValue);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ToByteCeilFromFloat01(this float value_min0_max1) => (byte) math.ceil(math.saturate(value_min0_max1) * byte.MaxValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToUshortAsPercent(this float value_min0_max1) => (ushort) math.round(math.saturate(value_min0_max1) * ushort.MaxValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float PercentToNormalized(this float value) => value * 0.01f;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float NormalizedToPercent(this float value) => value * 100f;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToUshortRounded(this float value) => (ushort) math.round(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ExpandToVec2(this float value)
        {
            return new Vector2(value, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ExpandToVec3(this float value)
        {
            return new Vector3(value, value, value);
        }

        public static string AsTimeToPrint(this float seconds) => ((double) seconds).AsTimeToPrint();

        public static float LerpTo(this float valueA, float valueB, float lerpValue) => math.lerp(valueA, valueB, lerpValue);

        /// <summary>Unity.Mathematics CNoise functions output in a range of -1.1 to 1.1, so this function normalizes the output of those functions</summary>
        public static float NormalizeCNoise(this float noiseValue)
        {
            const float expectedRange = 1.1f;
            const float rangeMult = 1f / (expectedRange * 2f);
            return (noiseValue + expectedRange) * rangeMult;
        }
    }
}