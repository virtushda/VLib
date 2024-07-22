using MaxMath;
using Unity.Mathematics;

namespace VLib
{
    public static class HalfExt
    {
        // Has issues surrounding comparison of negative values, 
        public static bool EqualsRoughly(this half a, half b, float epsilon) => math.abs((float)a - (float)b) <= epsilon;
        
        public static bool EqualsRoughly(this half4 a, half4 b, float epsilon)
        {
            float4 aFloat = a;
            float4 bFloat = b;
            return math.all(math.abs(aFloat - bFloat) <= epsilon);
        }
    }
}