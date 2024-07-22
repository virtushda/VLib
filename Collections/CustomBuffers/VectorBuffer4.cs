using Unity.Mathematics;

namespace VLib
{
    public class VectorBuffer4 : VectorBuffer<float4>
    {
        public VectorBuffer4(float4 defaultValue, int initialCapacity = 8) : base(defaultValue, initialCapacity) { }
        public override string ShaderPropertyName => "_CustomBuffer4";
    }
}