using Unity.Mathematics;

namespace VLib.SpatialAcceleration
{
    public interface ISpatialHashElement
    {
        float2 SpatialHashPosition { get; }
        float SpatialHashHalfSize { get; }
    }
}