using System.Diagnostics;
using Unity.Mathematics;

namespace VLib.Structures
{
    [System.Serializable]
    public struct Long2
    {
        public long x;
        public long y;
        
        public long this[int index]
        {
            get
            {
                ConditionalCheckIndex(index);
                return index == 0 ? x : y;
            }
            set
            {
                ConditionalCheckIndex(index);
                if (index == 0)
                    x = value;
                else
                    y = value;
            }
        }
        
        public Long2(long x, long y)
        {
            this.x = x;
            this.y = y;
        }
        
        public long CMin() => x < y ? x : y;
        public long CMax() => x > y ? x : y;
        
        public static implicit operator Long2(long value) => new(value, value);
        public static implicit operator Long2(int2 value) => new(value.x, value.y);
        
        public static explicit operator uint2(Long2 value) => new((uint)value.x, (uint)value.y);
        
        public static Long2 operator +(Long2 left, Long2 right) => new(left.x + right.x, left.y + right.y);
        public static Long2 operator -(Long2 left, Long2 right) => new(left.x - right.x, left.y - right.y);
        public static Long2 operator *(Long2 left, Long2 right) => new(left.x * right.x, left.y * right.y);
        public static Long2 operator /(Long2 left, Long2 right) => new(left.x / right.x, left.y / right.y);
        
        public static Long2 operator +(Long2 left, long right) => new(left.x + right, left.y + right);
        public static Long2 operator -(Long2 left, long right) => new(left.x - right, left.y - right);
        public static Long2 operator *(Long2 left, long right) => new(left.x * right, left.y * right);
        public static Long2 operator /(Long2 left, long right) => new(left.x / right, left.y / right);

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        public void ConditionalCheckIndex(int index)
        {
            if (index < 0)
                UnityEngine.Debug.LogError("Index is less than 0");
            if (index > 1)
                UnityEngine.Debug.LogError("Index is greater than 1");
        }
    }
}