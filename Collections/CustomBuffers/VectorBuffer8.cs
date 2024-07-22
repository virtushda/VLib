using Unity.Mathematics;

namespace VLib
{
    public class VectorBuffer8 : VectorBuffer<float4x2>
    {
        public VectorBuffer8(float4x2 defaultValue, int initialCapacity = 8) : base(defaultValue, initialCapacity) { }
        public override string ShaderPropertyName => "_CustomBuffer8";
    }
}