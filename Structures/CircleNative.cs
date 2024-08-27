using Unity.Mathematics;

namespace VLib
{
    public struct CircleNative
    {
        public float2 position;
        public float radius;
        
        public CircleNative(float2 position, float radius)
        {
            this.position = position;
            this.radius = radius;
        }
    }
}