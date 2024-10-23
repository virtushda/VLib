using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace VLib
{
    ///<summary> RGB, 8 bits per channel, set of 4. </summary>
    [Serializable]
    public struct ColorRGB101210Set
    {
        public ColorRGB101210 color0;
        public ColorRGB101210 color1;
        public ColorRGB101210 color2;
        public ColorRGB101210 color3;
        public byte count;

        public static ColorRGB101210Set White => new (ColorRGB101210.white);

        public ColorRGB101210Set(ColorRGB101210 color, int newCount = 4)
        {
            color0 = color;
            color1 = color;
            color2 = color;
            color3 = color;
            count = (byte)math.clamp(newCount, 0, 4);
        }
        
        public ColorRGB101210Set(ColorRGB101210[] colors)
        {
            color0 = color1 = color2 = color3 = ColorRGB101210.white;
            
            if (colors == null)
            {
                Debug.LogError($"Colorset Constructor ERROR: Input array is NULL.");
                this = new ColorRGB101210Set(ColorRGB101210.clear);
                return;
            }

            if (colors.Length < 1)
            {
                Debug.LogError($"Colorset Constructor ERROR: Input array has length 0.");
                this = new ColorRGB101210Set(ColorRGB101210.clear);
                return;
            }
            
            count = (byte)math.clamp(colors.Length, 0, 4);

            for (int i = 0; i < colors.Length && i < 4; i++)
                this[i] = colors[i];
        }

        public ColorRGB101210Set(Color[] colors)
        {
            color0 = color1 = color2 = color3 = ColorRGB101210.white;
            
            if (colors == null)
            {
                Debug.LogError($"Colorset Constructor ERROR: Input array is NULL.");
                this = new ColorRGB101210Set(ColorRGB101210.white);
                return;
            }

            if (colors.Length < 1)
            {
                Debug.LogError($"Colorset Constructor ERROR: Input array has length 0.");
                this = new ColorRGB101210Set(ColorRGB101210.white);
                return;
            }
            
            count = (byte)math.clamp(colors.Length, 0, 4);

            for (int i = 0; i < colors.Length && i < 4; i++)
                this[i] = (ColorRGB101210)colors[i];
        }
        
        public ColorRGB101210Set(ColorRGB101210? color0, ColorRGB101210? color1, ColorRGB101210? color2, ColorRGB101210? color3)
        {
            count = 0;
            if (color0 != null)
            {
                this.color0 = color0.Value;
                ++count;
            }
            else
                this.color0 = ColorRGB101210.white;
            if (color1 != null)
            {
                this.color1 = color1.Value;
                ++count;
            }
            else
                this.color1 = ColorRGB101210.white;
            if (color2 != null)
            {
                this.color2 = color2.Value;
                ++count;
            }
            else
                this.color2 = ColorRGB101210.white;
            if (color3 != null)
            {
                this.color3 = color3.Value;
                ++count;
            }
            else
                this.color3 = ColorRGB101210.white;
        }

        public ColorRGB101210 this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return color0;
                    case 1: return color1;
                    case 2: return color2;
                    case 3: return color3;
                    default:
                        Debug.LogError($"Index '{index}' is outside invalid range [0 - 3]!");
                        return default;
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        color0 = value;
                        return;
                    case 1:
                        color1 = value;
                        return;
                    case 2:
                        color2 = value;
                        return;
                    case 3:
                        color3 = value;
                        return;
                    default:
                        Debug.LogError($"Index '{index}' is outside invalid range [0 - 3]!");
                        return;
                }
            }
        }

        ///<summary> stores the four packed color values (int-size each) into each component of the vector4 </summary>
        public Vector4 ToVector4()
        {
            // Reinterpret, do NOT cast, we are storing ints in float types so that we can use Vector4 type as a bus
            var color0AsFloat = UnsafeUtility.As<ColorRGB101210, float>(ref color0);
            var color1AsFloat = UnsafeUtility.As<ColorRGB101210, float>(ref color1);
            var color2AsFloat = UnsafeUtility.As<ColorRGB101210, float>(ref color2);
            var color3AsFloat = UnsafeUtility.As<ColorRGB101210, float>(ref color3);
            
            return new Vector4(color0AsFloat, color1AsFloat, color2AsFloat, color3AsFloat);
        }
    }
}