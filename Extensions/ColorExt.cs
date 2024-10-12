using Unity.Mathematics;
using UnityEngine;

namespace VLib
{
    public static class ColorExt
    {
        public static float4 RGBAToFloat4(this Color c) => new float4(c.r, c.g, c.b, c.a);
        public static float3 RGBToFloat3(this Color c) => new float3(c.r, c.g, c.b);

        /// <summary> Normalizes the brightness of a color, helps when lerping between colors to keep a roughly constant brightness. </summary>
        /// <param name="brightnessTarget">How "bright" should the normalized color be? 1 is default</param>
        public static Color GetNormalizedToBrightness(this Color c, float brightnessTarget = 1)
        {
            float multiplier = 9999; // Support HDR
            if (c.r > 0)
                multiplier = math.min(multiplier, brightnessTarget / c.r);
            if (c.g > 0)
                multiplier = math.min(multiplier, brightnessTarget / c.g);
            if (c.b > 0)
                multiplier = math.min(multiplier, brightnessTarget / c.b);

            float alphaBefore = c.a;
            Color cNorm = c * multiplier;
            cNorm.a = alphaBefore;

            return cNorm;
        }

        public static Color Normalized(this Color col)
        {
            var max = math.max(math.max(col.r, col.g), col.b);
            var mult = 1f / max;
            return new Color(col.r * mult, col.g * mult, col.b * mult, col.a);
        }

        public static Color WithAlpha(this Color col, float alpha) => new Color(col.r, col.g, col.b, alpha);
        public static Color WithAlphaMult(this Color col, float alpha) => new Color(col.r, col.g, col.b, col.a * alpha);
    }
}