using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace VLib
{
    /// <summary> Establishes a global ColorRGB buffer accessible on the GPU </summary>
    public class SharedGlobalColorBuffer : VectorBuffer<ColorRGB>
    {
        int precision;
        Dictionary<int3, int> colorsToIndices;
        int length;
        int softLengthCap;
        string shaderPropName;

        public int Precision => precision;
        public int Length => length;

        public override string ShaderPropertyName => shaderPropName;
        
        /// <summary> Establishes a global ColorRGB buffer accessible on the GPU </summary>
        /// <param name="shaderPropertyName">Name of the buffer on the GPU</param>
        /// <param name="defaultValue">Value to initialize new buffer members with by default</param>
        /// <param name="precision">Steps between 0-1. Colors will be rounded to the nearest 'step' for index sharing and memory optimization.</param>
        /// <param name="softLengthCap">If the collection grows above this size, it will still function, but it will report errors.</param>
        public SharedGlobalColorBuffer(string shaderPropertyName, ColorRGB defaultValue, ushort precision = 100, int initialCapacity = 64, int softLengthCap = 2048) 
            : base(defaultValue, initialCapacity)
        {
            colorsToIndices = new(initialCapacity);
            shaderPropName = shaderPropertyName;
            this.precision = precision;
            length = 0;
            this.softLengthCap = softLengthCap;

            int defaultColorIndex = GetIndexForColor(defaultValue);
            if (defaultColorIndex != 0)
                Debug.LogError($"Default color index was '{defaultColorIndex}', but should be ZERO! Tell Seth at once! He will wish to dump this problem on Mau ASAP!");
        }

        /// <summary>Gets an index for a color in the global buffer, whether the color is new or known.</summary>
        public int GetIndexForColor(ColorRGB color)
        {
            var preciseColor = ColorToPrecisionColor(color);
            
            if (colorsToIndices.TryGetValue(preciseColor, out int index))
                return index;

            return AddColor(color, preciseColor);
        }

        /// <summary>Try to read the internal color by index</summary>
        public unsafe bool TryGetColorByIndex(int index, out ColorRGB color)
        {
            if (index < length)
            {
                color = (*CPUBuffer)[index];
                return true;
            }
            color = native->defaultValue;
            return false;
        }

        /// <summary>Converts a color to an integer representation based on the input precision.</summary>
        public int3 ColorToPrecisionColor(ColorRGB color)
        {
            return (int3) math.round(color.rgb * precision);
        }

        public void EnsureUpToDateOnGPU(bool forceIfNotDirty = false)
        {
            if (forceIfNotDirty || IsDirty)
            {
                this.UpdateGPUBufferIfDirty();
                Shader.SetGlobalBuffer(shaderPropName, gpuBuffer);
            }
        }

        int AddColor(ColorRGB color, int3 preciseColor)
        {
            var nextIndex = length;

            this[nextIndex] = color;
            
            colorsToIndices.Add(preciseColor, nextIndex);
            length++;
            
            if (length >= softLengthCap)
                Debug.LogError($"Global shared buffer is over warning threshold. Length: {length}");

            return nextIndex;
        }
        
        public long MemoryFootprintBytes()
        {
            long footprint = colorsToIndices.MemoryFootprintBytes();
            footprint += 20;
            return footprint;
        }
    }
}