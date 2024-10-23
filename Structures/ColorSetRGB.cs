using System;
using UnityEngine;

namespace VLib
{
    [Serializable]
    public struct ColorSetRGB
    {
        public ColorRGB color0;
        public ColorRGB color1;
        public ColorRGB color2;
        public ColorRGB color3;

        public static ColorSetRGB White => new ColorSetRGB(ColorRGB.white);

        public ColorSetRGB(ColorRGB color)
        {
            color0 = color;
            color1 = color;
            color2 = color;
            color3 = color;
        }
        
        public ColorSetRGB(ColorRGB[] colors)
        {
            color0 = color1 = color2 = color3 = ColorRGB.white;
            
            if (colors == null)
            {
                Debug.LogError($"Colorset Constructor ERROR: Input array is NULL.");
                this = new ColorSetRGB(ColorRGB.clear);
                return;
            }

            if (colors.Length < 1)
            {
                Debug.LogError($"Colorset Constructor ERROR: Input array has length 0.");
                this = new ColorSetRGB(ColorRGB.clear);
                return;
            }

            for (int i = 0; i < colors.Length && i < 4; i++)
                this[i] = colors[i];
        }

        public ColorSetRGB(Color[] colors)
        {
            color0 = color1 = color2 = color3 = ColorRGB.white;
            
            if (colors == null)
            {
                Debug.LogError($"Colorset Constructor ERROR: Input array is NULL.");
                this = new ColorSetRGB(ColorRGB.white);
                return;
            }

            if (colors.Length < 1)
            {
                Debug.LogError($"Colorset Constructor ERROR: Input array has length 0.");
                this = new ColorSetRGB(ColorRGB.white);
                return;
            }

            for (int i = 0; i < colors.Length && i < 4; i++)
                this[i] = (ColorRGB)colors[i];
        }
        
        public ColorSetRGB(ColorRGB color0, ColorRGB color1, ColorRGB color2, ColorRGB color3)
        {
            this.color0 = color0;
            this.color1 = color1;
            this.color2 = color2;
            this.color3 = color3;
        }

        public ColorRGB this[int index]
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