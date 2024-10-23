using System;
using UnityEngine;

namespace VLib
{
    ///<summary> RGB, 8 bits per channel, set of 4. </summary>
    [Serializable]
    public struct ColorSetRGB24
    {
        public ColorRGB24 color0;
        public ColorRGB24 color1;
        public ColorRGB24 color2;
        public ColorRGB24 color3;

        public static ColorSetRGB24 White => new ColorSetRGB24(ColorRGB24.white);

        public ColorSetRGB24(ColorRGB24 color)
        {
            color0 = color;
            color1 = color;
            color2 = color;
            color3 = color;
        }
        
        public ColorSetRGB24(ColorRGB24[] colors)
        {
            color0 = color1 = color2 = color3 = ColorRGB24.white;
            
            if (colors == null)
            {
                Debug.LogError($"Colorset Constructor ERROR: Input array is NULL.");
                this = new ColorSetRGB24(ColorRGB24.clear);
                return;
            }

            if (colors.Length < 1)
            {
                Debug.LogError($"Colorset Constructor ERROR: Input array has length 0.");
                this = new ColorSetRGB24(ColorRGB24.clear);
                return;
            }

            for (int i = 0; i < colors.Length && i < 4; i++)
                this[i] = colors[i];
        }

        public ColorSetRGB24(Color[] colors)
        {
            color0 = color1 = color2 = color3 = ColorRGB24.white;
            
            if (colors == null)
            {
                Debug.LogError($"Colorset Constructor ERROR: Input array is NULL.");
                this = new ColorSetRGB24(ColorRGB24.white);
                return;
            }

            if (colors.Length < 1)
            {
                Debug.LogError($"Colorset Constructor ERROR: Input array has length 0.");
                this = new ColorSetRGB24(ColorRGB24.white);
                return;
            }

            for (int i = 0; i < colors.Length && i < 4; i++)
                this[i] = (ColorRGB24)colors[i];
        }
        
        public ColorSetRGB24(ColorRGB24 color0, ColorRGB24 color1, ColorRGB24 color2, ColorRGB24 color3)
        {
            this.color0 = color0;
            this.color1 = color1;
            this.color2 = color2;
            this.color3 = color3;
        }

        public ColorRGB24 this[int index]
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
    }
}