using System;
using UnityEngine;

namespace VLib
{
    [Serializable]
    public struct ColorSetRGBCount
    {
        public byte count;
        public ColorSetRGB colors;
        
        public ColorSetRGBCount(ColorSetRGB colors, byte count)
        {
            this.colors = colors;
            this.count = count;
        }

        public ColorSetRGBCount(Color[] colors)
        {
            this.colors = new ColorSetRGB(colors);
            count = (byte) colors.Length;
        }
        
        public ColorRGB this[int index]
        {
            get
            {
                if (index < 0 || index >= count)
                {
                    Debug.LogError($"ColorSetRGBCount ERROR: Index out of range. Index: {index}, Count: {count}");
                    return ColorRGB.clear;
                }
                return colors[index];
            }
            set
            {
                if (index < 0 || index >= count)
                {
                    Debug.LogError($"ColorSetRGBCount ERROR: Index out of range. Index: {index}, Count: {count}");
                    return;
                }
                colors[index] = value;
            }
        }
    }
}