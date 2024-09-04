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
        
        public readonly RectNative GetBounds() => new(position, radius * 2);

        public readonly float Area() => math.PI * radius * radius;
        
        public readonly bool Contains(float2 point) => math.distancesq(position, point) <= radius * radius;
    }
}