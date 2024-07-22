using Unity.Mathematics;

namespace VLib
{
    public class VectorBuffer16 : VectorBuffer<float4x4>
    {
        public VectorBuffer16(float4x4 defaultValue, int initialCapacity = 8) : base(defaultValue, initialCapacity) { }
        public override string ShaderPropertyName => "_CustomBuffer16";
    }
}