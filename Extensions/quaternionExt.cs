using Unity.Mathematics;

namespace VLib
{
    public static class quaternionExt
    {
        public static float3 Right(in this quaternion q) => math.mul(q, math.right());
        public static float3 Up(in this quaternion q) => math.mul(q, math.up());
        public static float3 Forward(in this quaternion q) => math.mul(q, math.forward());
    }
}