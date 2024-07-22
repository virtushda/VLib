using Unity.Mathematics;

namespace VLib
{
    public class VectorBuffer2 : VectorBuffer<float2>
    {
        public VectorBuffer2(float2 defaultValue, int initialCapacity = 8) : base(defaultValue, initialCapacity) { }
        public override string ShaderPropertyName => "_CustomBuffer2";
    }
}