using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace VLib.Structures
{
    [Serializable, StructLayout(LayoutKind.Explicit)]
    public unsafe struct Float8
    {
        [FieldOffset(0)] public float4 _0_3;
        [FieldOffset(16)] public float4 _4_7;

        public Float8(float _0, float _1, float _2, float _3, float _4, float _5, float _6, float _7)
        {
            _0_3 = new float4(_0, _1, _2, _3);
            _4_7 = new float4(_4, _5, _6, _7);
        }

        public Float8(float value)
        {
            _0_3 = new float4(value);
            _4_7 = new float4(value);
        }

        public float this[int index]
        {
            get
            {
                ConditionalCheckIndex(index);
                return index switch
                {
                    0 => _0_3.x,
                    1 => _0_3.y,
                    2 => _0_3.z,
                    3 => _0_3.w,
                    4 => _4_7.x,
                    5 => _4_7.y,
                    6 => _4_7.z,
                    7 => _4_7.w,
                    _ => throw new IndexOutOfRangeException($"Index {index} out of valid range 0-7.")
                };
            }
            set
            {
                ConditionalCheckIndex(index);
                switch (index)
                {
                    case 0:
                        _0_3.x = value;
                        break;
                    case 1:
                        _0_3.y = value;
                        break;
                    case 2:
                        _0_3.z = value;
                        break;
                    case 3:
                        _0_3.w = value;
                        break;
                    case 4:
                        _4_7.x = value;
                        break;
                    case 5:
                        _4_7.y = value;
                        break;
                    case 6:
                        _4_7.z = value;
                        break;
                    case 7:
                        _4_7.w = value;
                        break;
                }
            }
        }
        
        public static Float8 operator +(Float8 baseValue, float addValue)
        {
            baseValue._0_3 += addValue;
            baseValue._4_7 += addValue;
            return baseValue;
        }
        
        public static Float8 operator +(Float8 baseValue, Float8 addValue)
        {
            baseValue._0_3 += addValue._0_3;
            baseValue._4_7 += addValue._4_7;
            return baseValue;
        }
        
        public static Float8 operator -(Float8 baseValue, float subtractValue)
        {
            baseValue._0_3 -= subtractValue;
            baseValue._4_7 -= subtractValue;
            return baseValue;
        }
        
        public static Float8 operator -(Float8 baseValue, Float8 subtractValue)
        {
            baseValue._0_3 -= subtractValue._0_3;
            baseValue._4_7 -= subtractValue._4_7;
            return baseValue;
        }
        
        public static Float8 operator *(Float8 baseValue, float multiplyValue)
        {
            baseValue._0_3 *= multiplyValue;
            baseValue._4_7 *= multiplyValue;
            return baseValue;
        }
        
        public static Float8 operator *(Float8 baseValue, Float8 multiplyValue)
        {
            baseValue._0_3 *= multiplyValue._0_3;
            baseValue._4_7 *= multiplyValue._4_7;
            return baseValue;
        }
        
        public static Float8 operator /(Float8 baseValue, float divideValue)
        {
            baseValue._0_3 /= divideValue;
            baseValue._4_7 /= divideValue;
            return baseValue;
        }
        
        public static Float8 operator /(Float8 baseValue, Float8 divideValue)
        {
            baseValue._0_3 /= divideValue._0_3;
            baseValue._4_7 /= divideValue._4_7;
            return baseValue;
        }

        // Conditional check index
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        public void ConditionalCheckIndex(int index)
        {
            if (index < 0)
                UnityEngine.Debug.LogError("Index is less than 0");
            if (index > 7)
                UnityEngine.Debug.LogError("Index is greater than 7");
        }
    }
}