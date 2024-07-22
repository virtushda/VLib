using System;
using Unity.Mathematics;

namespace VLib
{
    public interface ICmdTransform : IEquatable<float4x4>
    {
        float4x4 Matrix { get; set; }
    }
}