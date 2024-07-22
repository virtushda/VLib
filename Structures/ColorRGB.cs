using System;
using Unity.Mathematics;
using UnityEngine;

namespace VLib
{
    /// <summary> Like <see cref="Color"/> but without the alpha channel for a 25% memory reduction </summary>
    public struct ColorRGB : IEquatable<ColorRGB>, IFormattable
    {
        /// <summary>
        ///   <para>Red component of the color.</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color.r">`Color.r` on docs.unity3d.com</a></footer>
        public float r;

        /// <summary>
        ///   <para>Green component of the color.</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color.g">`Color.g` on docs.unity3d.com</a></footer>
        public float g;

        /// <summary>
        ///   <para>Blue component of the color.</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color.b">`Color.b` on docs.unity3d.com</a></footer>
        public float b;

        public float3 rgb => new float3(r, g, b);

        /// <summary>
        ///   <para>Constructs a new ColorRGB with given r,g,b components.</para>
        /// </summary>
        /// <param name="r">Red component.</param>
        /// <param name="g">Green component.</param>
        /// <param name="b">Blue component.</param>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color">`Color` on docs.unity3d.com</a></footer>
        public ColorRGB(float r, float g, float b)
        {
            this.r = r;
            this.g = g;
            this.b = b;
        }

        public ColorRGB(Color c)
        {
            this.r = c.r;
            this.g = c.g;
            this.b = c.b;
        }

        public override string ToString() => $"R:{r}, G:{g}, B:{b}";
        public string ToString(string format, IFormatProvider formatProvider) => ToString();

        public override int GetHashCode() => ((Vector3)this).GetHashCode();

        public override bool Equals(object other) => other is ColorRGB other1 && Equals(other1);

        public bool Equals(ColorRGB other) => r.Equals(other.r) && g.Equals(other.g) && b.Equals(other.b);

        public static ColorRGB operator +(ColorRGB a, ColorRGB b) => new ColorRGB(a.r + b.r, a.g + b.g, a.b + b.b);

        public static ColorRGB operator -(ColorRGB a, ColorRGB b) => new ColorRGB(a.r - b.r, a.g - b.g, a.b - b.b);

        public static ColorRGB operator *(ColorRGB a, ColorRGB b) => new ColorRGB(a.r * b.r, a.g * b.g, a.b * b.b);

        public static ColorRGB operator *(ColorRGB a, float b) => new ColorRGB(a.r * b, a.g * b, a.b * b);

        public static ColorRGB operator *(float b, ColorRGB a) => new ColorRGB(a.r * b, a.g * b, a.b * b);

        public static ColorRGB operator /(ColorRGB a, float b) => new ColorRGB(a.r / b, a.g / b, a.b / b);

        public static bool operator ==(ColorRGB lhs, ColorRGB rhs) => math.all((float3)lhs == (float3)rhs);

        public static bool operator !=(ColorRGB lhs, ColorRGB rhs) => !(lhs == rhs);

        /// <summary>
        ///   <para>Linearly interpolates between colors a and b by t.</para>
        /// </summary>
        /// <param name="a">ColorRGB a.</param>
        /// <param name="b">ColorRGB b.</param>
        /// <param name="t">Float for combining a and b.</param>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color.Lerp">`Color.Lerp` on docs.unity3d.com</a></footer>
        public static ColorRGB Lerp(ColorRGB a, ColorRGB b, float t)
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
        public static ColorRGB LerpUnclamped(ColorRGB a, ColorRGB b, float t) =>
            new ColorRGB(a.r + (b.r - a.r) * t, 
                         a.g + (b.g - a.g) * t, 
                         a.b + (b.b - a.b) * t);

        /// <summary>
        ///   <para>Solid red. RGB is (1, 0, 0, 1).</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-red">`Color.red` on docs.unity3d.com</a></footer>
        public static ColorRGB red => new ColorRGB(1f, 0.0f, 0.0f);

        /// <summary>
        ///   <para>Solid green. RGB is (0, 1, 0, 1).</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-green">`Color.green` on docs.unity3d.com</a></footer>
        public static ColorRGB green => new ColorRGB(0.0f, 1f, 0.0f);

        /// <summary>
        ///   <para>Solid blue. RGB is (0, 0, 1, 1).</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-blue">`Color.blue` on docs.unity3d.com</a></footer>
        public static ColorRGB blue => new ColorRGB(0.0f, 0.0f, 1f);

        /// <summary>
        ///   <para>Solid white. RGB is (1, 1, 1, 1).</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-white">`Color.white` on docs.unity3d.com</a></footer>
        public static ColorRGB white => new ColorRGB(1f, 1f, 1f);

        /// <summary>
        ///   <para>Solid black. RGB is (0, 0, 0, 1).</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-black">`Color.black` on docs.unity3d.com</a></footer>
        public static ColorRGB black => new ColorRGB(0.0f, 0.0f, 0.0f);

        /// <summary>
        ///   <para>Yellow. RGB is (1, 0.92, 0.016, 1), but the ColorRGB is nice to look at!</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-yellow">`Color.yellow` on docs.unity3d.com</a></footer>
        public static ColorRGB yellow => new ColorRGB(1f, 0.9215686f, 0.01568628f);

        /// <summary>
        ///   <para>Cyan. RGB is (0, 1, 1, 1).</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-cyan">`Color.cyan` on docs.unity3d.com</a></footer>
        public static ColorRGB cyan => new ColorRGB(0.0f, 1f, 1f);

        /// <summary>
        ///   <para>Magenta. RGB is (1, 0, 1, 1).</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-magenta">`Color.magenta` on docs.unity3d.com</a></footer>
        public static ColorRGB magenta => new ColorRGB(1f, 0.0f, 1f);

        /// <summary>
        ///   <para>Gray. RGB is (0.5, 0.5, 0.5, 1).</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-gray">`Color.gray` on docs.unity3d.com</a></footer>
        public static ColorRGB gray => new ColorRGB(0.5f, 0.5f, 0.5f);

        /// <summary>
        ///   <para>English spelling for gray. RGB is the same (0.5, 0.5, 0.5, 1).</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-grey">`Color.grey` on docs.unity3d.com</a></footer>
        public static ColorRGB grey => new ColorRGB(0.5f, 0.5f, 0.5f);

        /// <summary>
        ///   <para>Completely transparent. RGB is (0, 0, 0, 0).</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-clear">`Color.clear` on docs.unity3d.com</a></footer>
        public static ColorRGB clear => new ColorRGB(0.0f, 0.0f, 0.0f);

        /// <summary>
        ///   <para>The grayscale value of the color. (Read Only)</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-grayscale">`Color.grayscale` on docs.unity3d.com</a></footer>
        public float grayscale => (float)(0.29899999499321 * r + 0.587000012397766 * g + 57.0 / 500.0 * b);

        /// <summary>
        ///   <para>A linear value of an sRGB color.</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-linear">`Color.linear` on docs.unity3d.com</a></footer>
        public ColorRGB linear =>
            new ColorRGB(Mathf.GammaToLinearSpace(r), Mathf.GammaToLinearSpace(g), Mathf.GammaToLinearSpace(b));

        /// <summary>
        ///   <para>A version of the ColorRGB that has had the gamma curve applied.</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-gamma">`Color.gamma` on docs.unity3d.com</a></footer>
        public ColorRGB gamma => new ColorRGB(Mathf.LinearToGammaSpace(r), Mathf.LinearToGammaSpace(g), Mathf.LinearToGammaSpace(b));

        /// <summary>
        ///   <para>Returns the maximum ColorRGB component value: Max(r,g,b).</para>
        /// </summary>
        /// <footer><a href="https://docs.unity3d.com/2020.3/Documentation/ScriptReference/30_search.html?q=Color-maxColorComponent">`Color.maxColorComponent` on docs.unity3d.com</a></footer>
        public float maxColorComponent => math.max(math.max(r, g), b);

        public static implicit operator Vector3(ColorRGB c) => new Vector3(c.r, c.g, c.b);
        public static implicit operator ColorRGB(Vector3 v) => new ColorRGB(v.x, v.y, v.z);
        public static implicit operator float3(ColorRGB c) => new float3(c.r, c.g, c.b);
        public static implicit operator ColorRGB(float3 v) => new ColorRGB(v.x, v.y, v.z);
        public static explicit operator ColorRGB(Color v) => new ColorRGB(v.r, v.g, v.b);
        public static implicit operator Color(ColorRGB v) => new Color(v.r, v.g, v.b);

        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:  return r;
                    case 1:  return g;
                    case 2:  return b;
                    default: throw new IndexOutOfRangeException("Invalid ColorRGB index(" + index + ")!");
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        r = value;
                        break;
                    case 1:
                        g = value;
                        break;
                    case 2:
                        b = value;
                        break;
                    default: throw new IndexOutOfRangeException("Invalid ColorRGB index(" + index + ")!");
                }
            }
        }

        public static void RGBToHSV(ColorRGB rgbColor, out float H, out float S, out float V)
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
        public static ColorRGB HSVToRGB(float H, float S, float V) => ColorRGB.HSVToRGB(H, S, V, true);

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
        public static ColorRGB HSVToRGB(float H, float S, float V, bool hdr)
        {
            ColorRGB white = ColorRGB.white;
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
        }
    }
}