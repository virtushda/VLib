﻿using Unity.Mathematics;
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
    }
}