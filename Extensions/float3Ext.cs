using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace VLib
{
    public static class float3Ext
    {
        public static int CompareTo(this float3 value, float3 other)
        {
            //Batch comparisons
            bool3 lessThan = value < other;
            bool3 greaterThan = value > other;
            
            //Sort by X
            if (lessThan.x)
                return -1;
            if (greaterThan.x)
                return 1;

            //Then Y
            if (lessThan.y)
                return -1;
            if (greaterThan.y)
                return 1;

            //Then Z
            if (lessThan.z)
                return -1;
            if (greaterThan.z)
                return 1;
            
            return 0;
        }

        public static bool IsNanOrInf(this in float3 value) => any(isnan(value)) || any(isinf(value));
        
        public static float3 GetUniqueOrbitPosition(this float3 center, ulong uniqueID, float time, float minRadius, float maxRadius, float rotationSpeed, float radiusSpeed,
            out float angle, out float radius)
        {
            // Rehash the uniqueID to get well-distributed pseudorandom values
            var hashedValue = uniqueID.Rehash();
    
            // Extract two different hash components
            var hashForAngle = (uint)(hashedValue & 0xFFFFFFFF);
            var hashForRadius = (uint)(hashedValue >> 32);
            
            var offsetAngle = hashForAngle / (float)uint.MaxValue * 100f;
            var offsetRadius = hashForRadius / (float)uint.MaxValue * 100f;
    
            // Sample perlin noise for angle
            var angleNoise = saturate(Mathf.PerlinNoise(time * rotationSpeed, offsetAngle));
            angle = angleNoise * PI2;
    
            // Sample perlin noise for radius
            var radiusNoise = saturate(Mathf.PerlinNoise(time * radiusSpeed, offsetRadius));
            radius = lerp(minRadius, maxRadius, radiusNoise);
            
            return center + new float3(cos(angle), 0f, sin(angle)) * radius;
        }
    }
}