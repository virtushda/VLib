using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace VLib
{
    public static class Float2Ext
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rect ToRect(this float2 vec, float sizeX, float sizeY, float margin)
        {
            return new Rect(vec.x - sizeX / 2 - margin,
                vec.y - sizeY / 2 - margin,
                sizeX + margin * 2,
                sizeY + margin * 2);
        }

        /// <summary>
        /// This is kind of confusing, just use ToFloat3_SupplyX/Y/Z instead
        /// Index:
        /// 0: vec3.x
        /// 1: vec3.y
        /// 2: vec3.z
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ToFloat3(this float2 vec, int indexX, int indexY, int indexZ, float thirdValue = 0)
        {
            float3 vec3 = new float3();
            vec3[indexX] = vec.x;
            vec3[indexY] = vec.y;
            vec3[indexZ] = thirdValue;
            return vec3;
        }

        /// <summary> Turn a float2 into a float3, where the float2 becomes float3.yz and you supply 'x' </summary>
        public static float3 ToFloat3_X(this float2 vec, float newXValue = 0) => new(newXValue, vec);
        
        /// <summary> Turn a float2 into a float3, where the float2 becomes float3.xz and you supply 'y' </summary>
        public static float3 ToFloat3_Y(this float2 vec, float newYValue = 0) => new(vec.x, newYValue, vec.y);
        
        /// <summary> Turn a float2 into a float3, where the float2 becomes float3.xy and you supply 'z' </summary>
        public static float3 ToFloat3_Z(this float2 vec, float newZValue = 0) => new(vec, newZValue);

        [GenerateTestsForBurstCompatibility]
        public static float Average(this float2 vec) => math.csum(vec * .5f);
    }
}