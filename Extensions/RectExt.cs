using Unity.Mathematics;
using UnityEngine;

namespace VLib
{
    public static class RectExt
    {
        public static RectInt ExpandToRectInt(this Rect r, float expansion)
        {
            r.xMin -= expansion;
            r.yMin -= expansion;
            r.xMax += expansion;
            r.yMax += expansion;

            return new RectInt((int)r.xMin, (int)r.yMin, (int)(r.xMax + .999999f), (int)(r.yMax + .999999f));
        }

        public static (float3 start, float3 end) EdgeBottom3D(this Rect r, bool useXZ = true, float thirdValue = 0)
        {
            float3 start = new float3(r.xMax, useXZ ? thirdValue : r.yMin, useXZ ? r.yMin : thirdValue);
            float3 end = new float3(r.xMin, useXZ ? thirdValue : r.yMin, useXZ ? r.yMin : thirdValue);
            return (start, end);
        }

        public static (float3 start, float3 end) EdgeLeft3D(this Rect r, bool useXZ = true, float thirdValue = 0)
        {
            float3 start = new float3(r.xMin, useXZ ? thirdValue : r.yMin, useXZ ? r.yMin : thirdValue);
            float3 end = new float3(r.xMin, useXZ ? thirdValue : r.yMax, useXZ ? r.yMax : thirdValue);
            return (start, end);
        }

        public static (float3 start, float3 end) EdgeRight3D(this Rect r, bool useXZ = true, float thirdValue = 0)
        {
            float3 start = new float3(r.xMax, useXZ ? thirdValue : r.yMax, useXZ ? r.yMax : thirdValue);
            float3 end = new float3(r.xMax, useXZ ? thirdValue : r.yMin, useXZ ? r.yMin : thirdValue);
            return (start, end);
        }

        public static (float3 start, float3 end) EdgeTop3D(this Rect r, bool useXZ = true, float thirdValue = 0)
        {
            float3 start = new float3(r.xMin, useXZ ? thirdValue : r.yMax, useXZ ? r.yMax : thirdValue);
            float3 end = new float3(r.xMax, useXZ ? thirdValue : r.yMax, useXZ ? r.yMax : thirdValue);
            return (start, end);
        }
    }
}