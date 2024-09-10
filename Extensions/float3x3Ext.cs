using Unity.Mathematics;

namespace VLib
{
    public static class float3x3Ext
    {
        public static float3x3 Lerp(in this float3x3 a, in float3x3 b, float t)
        {
            var newTransform = new float3x3();
            newTransform.c0 = math.lerp(a.c0, b.c0, t);
            newTransform.c1 = math.lerp(a.c1, b.c1, t);
            newTransform.c2 = math.lerp(a.c2, b.c2, t);
            return newTransform;
        }
    }
}