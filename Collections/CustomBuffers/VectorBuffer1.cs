namespace VLib
{
    public class VectorBuffer1 : VectorBuffer<float>
    {
        public VectorBuffer1(float defaultValue, int initialCapacity = 8) : base(defaultValue, initialCapacity) { }
        public override string ShaderPropertyName => "_CustomBuffer1";
    }
}