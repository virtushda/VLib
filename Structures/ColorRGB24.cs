using System;
using MaxMath;
using Unity.Mathematics;
using UnityEngine;

namespace VLib
{
    /// <summary> Like <see cref="ColorRGB"/> but with 8 bits per channel instead of 32, for a 75% mem reduction </summary>
    public struct ColorRGB24 : IEquatable<ColorRGB24>, IFormattable
    {
        public byte3 rgb;

        public float Redf
        {
            get => rgb.x.ToPercent01();
            set => rgb.x = value.ToByteAsPercent();
        }
        
        public float Greenf
        {
            get => rgb.y.ToPercent01();
            set => rgb.y = value.ToByteAsPercent();
        }

        public float Bluef
        {
            get => rgb.z.ToPercent01();
            set => rgb.z = value.ToByteAsPercent();
        }
        
        public float3 RGBf
        {
            get => new(Redf, Greenf, Bluef);
            set
            {
                Redf = value.x;
                Greenf = value.y;
                Bluef = value.z;
            }
        }
        
        public ColorRGB24(byte3 rgb) => this.rgb = rgb;
        
        public ColorRGB24(byte r, byte g, byte b) => rgb = new byte3(r, g, b);

        public ColorRGB24(float r, float g, float b)
        {
            rgb = new byte3(r.ToByteAsPercent(), g.ToByteAsPercent(), b.ToByteAsPercent());
        }

        public ColorRGB24(Color c)
        {
            rgb = new byte3(c.r.ToByteAsPercent(), c.g.ToByteAsPercent(), c.b.ToByteAsPercent());
        }

        public override string ToString() => $"R:{Redf}, G:{Greenf}, B:{Bluef}";
        public string ToString(string format, IFormatProvider formatProvider) => ToString();

        public override int GetHashCode() => rgb.GetHashCode();

        public override bool Equals(object other) => other is ColorRGB24 other1 && Equals(other1);

        public bool Equals(ColorRGB24 other) => rgb.Equals(other.rgb);

        /*public static ColorRGB24 operator +(ColorRGB24 a, ColorRGB24 b) => new ColorRGB24(((int3) a.rgb + (int3) b.rgb));

        public static ColorRGB24 operator -(ColorRGB24 a, ColorRGB24 b) => new ColorRGB24(a.r - b.r, a.g - b.g, a.b - b.b);

        public static ColorRGB24 operator *(ColorRGB24 a, ColorRGB24 b) => new ColorRGB24(a.r * b.r, a.g * b.g, a.b * b.b);

        public static ColorRGB24 operator *(ColorRGB24 a, float b) => new ColorRGB24(a.r * b, a.g * b, a.b * b);

        public static ColorRGB24 operator *(float b, ColorRGB24 a) => new ColorRGB24(a.r * b, a.g * b, a.b * b);

        public static ColorRGB24 operator /(ColorRGB24 a, float b) => new ColorRGB24(a.r / b, a.g / b, a.b / b);*/

        public static bool operator ==(ColorRGB24 lhs, ColorRGB24 rhs) => math.all(lhs.rgb == rhs.rgb);

        public static bool operator !=(ColorRGB24 lhs, ColorRGB24 rhs) => !(lhs == rhs);

        /*/// <summary>
        ///   <para>Linearly interpolates between colors a and b by t.</para>
        /// </summary>
        /// <param name="a">ColorRGB24 a.</param>
        /// <param name="b">ColorRGB24 b.</param>
        /// <param name="t">Float for combining a and b.</param>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color.Lerp">`Color.Lerp` on docs.unity3d.com</a></footer>
        public static ColorRGB24 Lerp(ColorRGB24 a, ColorRGB24 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new ColorRGB(a.r + (b.r - a.r) * t, 
                a.g + (b.g - a.g) * t, 
                a.b + (b.b - a.b) * t);
        }

        /// <summary>
        ///   <para>Linearly interpolates between colors a and b by t.</para>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="t"></param>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color.LerpUnclamped">`Color.LerpUnclamped` on docs.unity3d.com</a></footer>
        public static ColorRGB24 LerpUnclamped(ColorRGB24 a, ColorRGB24 b, float t) =>
            new ColorRGB(a.r + (b.r - a.r) * t, 
                a.g + (b.g - a.g) * t, 
                a.b + (b.b - a.b) * t);*/

        /// <summary>
        ///   <para>Solid red. RGB is (1, 0, 0, 1).</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-red">`Color.red` on docs.unity3d.com</a></footer>
        public static ColorRGB24 red => new(1f, 0.0f, 0.0f);

        /// <summary>
        ///   <para>Solid green. RGB is (0, 1, 0, 1).</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-green">`Color.green` on docs.unity3d.com</a></footer>
        public static ColorRGB24 green => new(0.0f, 1f, 0.0f);

        /// <summary>
        ///   <para>Solid blue. RGB is (0, 0, 1, 1).</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-blue">`Color.blue` on docs.unity3d.com</a></footer>
        public static ColorRGB24 blue => new(0.0f, 0.0f, 1f);

        /// <summary>
        ///   <para>Solid white. RGB is (1, 1, 1, 1).</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-white">`Color.white` on docs.unity3d.com</a></footer>
        public static ColorRGB24 white => new(1f, 1f, 1f);

        /// <summary>
        ///   <para>Solid black. RGB is (0, 0, 0, 1).</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-black">`Color.black` on docs.unity3d.com</a></footer>
        public static ColorRGB24 black => new(0.0f, 0.0f, 0.0f);

        /// <summary>
        ///   <para>Yellow. RGB is (1, 0.92, 0.016, 1), but the ColorRGB24 is nice to look at!</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-yellow">`Color.yellow` on docs.unity3d.com</a></footer>
        public static ColorRGB24 yellow => new(1f, 0.9215686f, 0.01568628f);

        /// <summary>
        ///   <para>Cyan. RGB is (0, 1, 1, 1).</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-cyan">`Color.cyan` on docs.unity3d.com</a></footer>
        public static ColorRGB24 cyan => new(0.0f, 1f, 1f);

        /// <summary>
        ///   <para>Magenta. RGB is (1, 0, 1, 1).</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-magenta">`Color.magenta` on docs.unity3d.com</a></footer>
        public static ColorRGB24 magenta => new(1f, 0.0f, 1f);

        /// <summary>
        ///   <para>Gray. RGB is (0.5, 0.5, 0.5, 1).</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-gray">`Color.gray` on docs.unity3d.com</a></footer>
        public static ColorRGB24 gray => grey;

        /// <summary>
        ///   <para>English spelling for gray. RGB is the same (0.5, 0.5, 0.5, 1).</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-grey">`Color.grey` on docs.unity3d.com</a></footer>
        public static ColorRGB24 grey => new(0.5f, 0.5f, 0.5f);

        /// <summary>
        ///   <para>Completely transparent. RGB is (0, 0, 0, 0).</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-clear">`Color.clear` on docs.unity3d.com</a></footer>
        public static ColorRGB24 clear => black;

        /// <summary>
        ///   <para>The grayscale value of the color. (Read Only)</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-grayscale">`Color.grayscale` on docs.unity3d.com</a></footer>
        public float grayscale => (float)(0.29899999499321 * rgb.x + 0.587000012397766 * rgb.y + 57.0 / 500.0 * rgb.z);

        // Use ColorRGB conversions in floating point BEFORE using this type to avoid losing more info
        /*/// <summary>
        ///   <para>A linear value of an sRGB color.</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-linear">`Color.linear` on docs.unity3d.com</a></footer>
        public ColorRGB24 linear =>
            new ColorRGB(Mathf.GammaToLinearSpace(r), Mathf.GammaToLinearSpace(g), Mathf.GammaToLinearSpace(b));

        /// <summary>
        ///   <para>A version of the ColorRGB24 that has had the gamma curve applied.</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-gamma">`Color.gamma` on docs.unity3d.com</a></footer>
        public ColorRGB24 gamma => new ColorRGB24(Mathf.LinearToGammaSpace(r), Mathf.LinearToGammaSpace(g), Mathf.LinearToGammaSpace(b));*/

        /// <summary>
        ///   Returns component-wise maximum
        /// </summary>
        public float maxColorComponent => maxmath.cmax(rgb);

        public static implicit operator Vector3(ColorRGB24 c) => (float3)c.rgb;
        public static implicit operator ColorRGB24(Vector3 v) => new(v.x, v.y, v.z);
        public static implicit operator float3(ColorRGB24 c) => c.RGBf;
        public static implicit operator ColorRGB24(float3 v) => new(v.x, v.y, v.z);
        public static explicit operator ColorRGB24(Color v) => new(v.r, v.g, v.b);
        public static implicit operator Color(ColorRGB24 v) => new(v.Redf, v.Greenf, v.Bluef);

        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:  return Redf;
                    case 1:  return Greenf;
                    case 2:  return Bluef;
                    default: throw new IndexOutOfRangeException("Invalid ColorRGB24 index(" + index + ")!");
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        Redf = value;
                        break;
                    case 1:
                        Greenf = value;
                        break;
                    case 2:
                        Bluef = value;
                        break;
                    default: throw new IndexOutOfRangeException("Invalid ColorRGB24 index(" + index + ")!");
                }
            }
        }

        /*public static void RGBToHSV(ColorRGB24 rgbColor, out float H, out float S, out float V)
        {
            if (rgbColor.b > (double)rgbColor.g && rgbColor.b > (double)rgbColor.r)
                RGBToHSVHelper(4f, rgbColor.b, rgbColor.r, rgbColor.g, out H, out S, out V);
            else if (rgbColor.g > (double)rgbColor.r)
                RGBToHSVHelper(2f, rgbColor.g, rgbColor.b, rgbColor.r, out H, out S, out V);
            else
                RGBToHSVHelper(0.0f, rgbColor.r, rgbColor.g, rgbColor.b, out H, out S, out V);
        }

        private static void RGBToHSVHelper(
            float offset,
            float dominantcolor,
            float colorone,
            float colortwo,
            out float H,
            out float S,
            out float V)
        {
            V = dominantcolor;
            if (V != 0.0)
            {
                float num1 = colorone <= (double)colortwo ? colorone : colortwo;
                float num2 = V - num1;
                if (num2 != 0.0)
                {
                    S = num2 / V;
                    H = offset + (colorone - colortwo) / num2;
                }
                else
                {
                    S = 0.0f;
                    H = offset + (colorone - colortwo);
                }

                H /= 6f;
                if (H >= 0.0)
                    return;
                ++H;
            }
            else
            {
                S = 0.0f;
                H = 0.0f;
            }
        }

        /// <summary>
        ///   <para>Creates an RGB colour from HSV input.</para>
        /// </summary>
        /// <param name="H">Hue [0..1].</param>
        /// <param name="S">Saturation [0..1].</param>
        /// <param name="V">Brightness value [0..1].</param>
        /// <param name="hdr">Output HDR colours. If true, the returned colour will not be clamped to [0..1].</param>
        /// <returns>
        ///   <para>An opaque colour with HSV matching the input.</para>
        /// </returns>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color.HSVToRGB">`Color.HSVToRGB` on docs.unity3d.com</a></footer>
        public static ColorRGB24 HSVToRGB(float H, float S, float V) => ColorRGB.HSVToRGB(H, S, V, true);

        /// <summary>
        ///   <para>Creates an RGB colour from HSV input.</para>
        /// </summary>
        /// <param name="H">Hue [0..1].</param>
        /// <param name="S">Saturation [0..1].</param>
        /// <param name="V">Brightness value [0..1].</param>
        /// <param name="hdr">Output HDR colours. If true, the returned colour will not be clamped to [0..1].</param>
        /// <returns>
        ///   <para>An opaque colour with HSV matching the input.</para>
        /// </returns>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color.HSVToRGB">`Color.HSVToRGB` on docs.unity3d.com</a></footer>
        public static ColorRGB24 HSVToRGB(float H, float S, float V, bool hdr)
        {
            ColorRGB24 white = ColorRGB.white;
            if (S == 0.0)
            {
                white.r = V;
                white.g = V;
                white.b = V;
            }
            else if (V == 0.0)
            {
                white.r = 0.0f;
                white.g = 0.0f;
                white.b = 0.0f;
            }
            else
            {
                white.r = 0.0f;
                white.g = 0.0f;
                white.b = 0.0f;
                float num1 = S;
                float num2 = V;
                float f = H * 6f;
                int num3 = (int)Mathf.Floor(f);
                float num4 = f - num3;
                float num5 = num2 * (1f - num1);
                float num6 = num2 * (float)(1.0 - num1 * (double)num4);
                float num7 = num2 * (float)(1.0 - num1 * (1.0 - num4));
                switch (num3)
                {
                    case -1:
                        white.r = num2;
                        white.g = num5;
                        white.b = num6;
                        break;
                    case 0:
                        white.r = num2;
                        white.g = num7;
                        white.b = num5;
                        break;
                    case 1:
                        white.r = num6;
                        white.g = num2;
                        white.b = num5;
                        break;
                    case 2:
                        white.r = num5;
                        white.g = num2;
                        white.b = num7;
                        break;
                    case 3:
                        white.r = num5;
                        white.g = num6;
                        white.b = num2;
                        break;
                    case 4:
                        white.r = num7;
                        white.g = num5;
                        white.b = num2;
                        break;
                    case 5:
                        white.r = num2;
                        white.g = num5;
                        white.b = num6;
                        break;
                    case 6:
                        white.r = num2;
                        white.g = num7;
                        white.b = num5;
                        break;
                }

                if (!hdr)
                {
                    white.r = Mathf.Clamp(white.r, 0.0f, 1f);
                    white.g = Mathf.Clamp(white.g, 0.0f, 1f);
                    white.b = Mathf.Clamp(white.b, 0.0f, 1f);
                }
            }

            return white;
        }*/
    }

    public static class ColorRGB24Ext
    {
        public static Color[] ToColorArray(this ColorRGB24[] colors)
        {
            Color[] c = new Color[colors.Length];
            for (int i = 0; i < colors.Length; i++)
                c[i] = colors[i];
            return c;
        }
    }
}