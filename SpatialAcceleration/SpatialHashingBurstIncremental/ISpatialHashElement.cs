using Unity.Mathematics;

namespace VLib.SpatialAcceleration
{
    public interface ISpatialHashElement
    {
        float2 Position { get; }
        float HalfSize { get; }
    }
}