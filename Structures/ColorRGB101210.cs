﻿using System;
using System.Runtime.InteropServices;
using MaxMath;
using Unity.Mathematics;
using UnityEngine;

namespace VLib
{
    /// <summary> Like <see cref="ColorRGB"/> with 10 bits for red, 12 bits for green and 10 bits for blue. </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct ColorRGB101210 : IEquatable<ColorRGB101210>, IFormattable
    {
        ///<summary> R: 10 bits <br/> G: 12 bits <br/> B: 10 bits </summary>
        [FieldOffset(0)]
        public uint data;

        public ushort RedRaw
        {
            get => (ushort) BitUtility.ReadNBitsFromRef(ref data, 0, 10);
            set => BitUtility.WriteNBitsToRef(ref data, 0, 10, value);
        }
        public ushort GreenRaw
        {
            get => (ushort) BitUtility.ReadNBitsFromRef(ref data, 10, 12);
            set => BitUtility.WriteNBitsToRef(ref data, 10, 12, value);
        }
        public ushort BlueRaw
        {
            get => (ushort) BitUtility.ReadNBitsFromRef(ref data, 22, 10);
            set => BitUtility.WriteNBitsToRef(ref data, 22, 10, value);
        }
        
        public float Redf
        {
            get => BitUtility.Convert_UpTo32BitsValue_To_Float01(RedRaw, 10);
            set => RedRaw = (ushort) BitUtility.Convert_Float01_To_UpTo32BitsValue(value, 10);
        }
        public float Greenf
        {
            get => BitUtility.Convert_UpTo32BitsValue_To_Float01(GreenRaw, 12);
            set => GreenRaw = (ushort) BitUtility.Convert_Float01_To_UpTo32BitsValue(value, 12);
        }
        public float Bluef
        {
            get => BitUtility.Convert_UpTo32BitsValue_To_Float01(BlueRaw, 10);
            set => BlueRaw = (ushort) BitUtility.Convert_Float01_To_UpTo32BitsValue(value, 10);
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

        public ColorRGB101210(float r, float g, float b)
        {
            data = default;
            Redf = r;
            Greenf = g;
            Bluef = b;
        }

        public ColorRGB101210(Color c)
        {
            data = default;
            Redf = c.r;
            Greenf = c.g;
            Bluef = c.b;
        }

        public override string ToString() => $"R:{Redf}, G:{Greenf}, B:{Bluef}";
        public string ToString(string format, IFormatProvider formatProvider) => ToString();

        public override int GetHashCode() => data.GetHashCode();

        public override bool Equals(object other) => other is ColorRGB101210 other1 && Equals(other1);

        public bool Equals(ColorRGB101210 other) => data.Equals(other.data);

        /*public static ColorRGB24 operator +(ColorRGB24 a, ColorRGB24 b) => new ColorRGB24(((int3) a.rgb + (int3) b.rgb));

        public static ColorRGB24 operator -(ColorRGB24 a, ColorRGB24 b) => new ColorRGB24(a.r - b.r, a.g - b.g, a.b - b.b);

        public static ColorRGB24 operator *(ColorRGB24 a, ColorRGB24 b) => new ColorRGB24(a.r * b.r, a.g * b.g, a.b * b.b);

        public static ColorRGB24 operator *(ColorRGB24 a, float b) => new ColorRGB24(a.r * b, a.g * b, a.b * b);

        public static ColorRGB24 operator *(float b, ColorRGB24 a) => new ColorRGB24(a.r * b, a.g * b, a.b * b);

        public static ColorRGB24 operator /(ColorRGB24 a, float b) => new ColorRGB24(a.r / b, a.g / b, a.b / b);*/

        public static bool operator ==(ColorRGB101210 lhs, ColorRGB101210 rhs) => lhs.data == rhs.data;

        public static bool operator !=(ColorRGB101210 lhs, ColorRGB101210 rhs) => !(lhs == rhs);

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
        public static ColorRGB101210 red => new(1f, 0.0f, 0.0f);

        /// <summary>
        ///   <para>Solid green. RGB is (0, 1, 0, 1).</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-green">`Color.green` on docs.unity3d.com</a></footer>
        public static ColorRGB101210 green => new(0.0f, 1f, 0.0f);

        /// <summary>
        ///   <para>Solid blue. RGB is (0, 0, 1, 1).</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-blue">`Color.blue` on docs.unity3d.com</a></footer>
        public static ColorRGB101210 blue => new(0.0f, 0.0f, 1f);

        /// <summary>
        ///   <para>Solid white. RGB is (1, 1, 1, 1).</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-white">`Color.white` on docs.unity3d.com</a></footer>
        public static ColorRGB101210 white => new(1f, 1f, 1f);

        /// <summary>
        ///   <para>Solid black. RGB is (0, 0, 0, 1).</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-black">`Color.black` on docs.unity3d.com</a></footer>
        public static ColorRGB101210 black => new(0.0f, 0.0f, 0.0f);

        /// <summary>
        ///   <para>Yellow. RGB is (1, 0.92, 0.016, 1), but the ColorRGB24 is nice to look at!</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-yellow">`Color.yellow` on docs.unity3d.com</a></footer>
        public static ColorRGB101210 yellow => new(1f, 0.9215686f, 0.01568628f);

        /// <summary>
        ///   <para>Cyan. RGB is (0, 1, 1, 1).</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-cyan">`Color.cyan` on docs.unity3d.com</a></footer>
        public static ColorRGB101210 cyan => new(0.0f, 1f, 1f);

        /// <summary>
        ///   <para>Magenta. RGB is (1, 0, 1, 1).</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-magenta">`Color.magenta` on docs.unity3d.com</a></footer>
        public static ColorRGB101210 magenta => new(1f, 0.0f, 1f);

        /// <summary>
        ///   <para>Gray. RGB is (0.5, 0.5, 0.5, 1).</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-gray">`Color.gray` on docs.unity3d.com</a></footer>
        public static ColorRGB101210 gray => grey;

        /// <summary>
        ///   <para>English spelling for gray. RGB is the same (0.5, 0.5, 0.5, 1).</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-grey">`Color.grey` on docs.unity3d.com</a></footer>
        public static ColorRGB101210 grey => new(0.5f, 0.5f, 0.5f);

        /// <summary>
        ///   <para>Completely transparent. RGB is (0, 0, 0, 0).</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-clear">`Color.clear` on docs.unity3d.com</a></footer>
        public static ColorRGB101210 clear => black;

        /// <summary>
        ///   <para>The grayscale value of the color. (Read Only)</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-grayscale">`Color.grayscale` on docs.unity3d.com</a></footer>
        //public float grayscale => (float)(0.29899999499321 * data.x + 0.587000012397766 * data.y + 57.0 / 500.0 * data.z);

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
        public float MaxColorComponent => math.max(Redf, math.max(Greenf, Bluef));

        public static implicit operator Vector3(ColorRGB101210 c) => c.RGBf;
        public static implicit operator ColorRGB101210(Vector3 v) => new(v.x, v.y, v.z);
        public static implicit operator float3(ColorRGB101210 c) => c.RGBf;
        public static implicit operator ColorRGB101210(float3 v) => new(v.x, v.y, v.z);
        public static explicit operator ColorRGB101210(Color v) => new(v.r, v.g, v.b);
        public static implicit operator Color(ColorRGB101210 v) => new(v.Redf, v.Greenf, v.Bluef);
        public static implicit operator uint(ColorRGB101210 v) => v.data;
        public static explicit operator ColorRGB101210(uint v) => new() { data = v };

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
    
    public static class ColorRGB101210Ext
    {
        public static Color[] ToColorArray(this ColorRGB101210[] colors)
        {
            Color[] c = new Color[colors.Length];
            for (int i = 0; i < colors.Length; i++)
                c[i] = colors[i];
            return c;
        }
        
        public static ColorRGB101210[] ToColorRGB101210Array(this Color[] colors)
        {
            ColorRGB101210[] c = new ColorRGB101210[colors.Length];
            for (int i = 0; i < colors.Length; i++)
                c[i] = (ColorRGB101210)colors[i];
            return c;
        }
    }
}