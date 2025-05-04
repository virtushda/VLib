using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace VLib
{
    public static class float4Ext
    {
        public static int CompareTo(this float4 value, float4 other)
        {
            //Batch comparisons
            var lessThan = value < other;
            var greaterThan = value > other;
            
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
            
            //Then W
            if (lessThan.w)
                return -1;
            if (greaterThan.w)
                return 1;
            
            return 0;
        }
    }
}