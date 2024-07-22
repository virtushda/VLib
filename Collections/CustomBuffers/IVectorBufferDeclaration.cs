namespace VLib
{
    public interface IVectorBufferDeclaration<out T>
    {
        public abstract T DefaultValue { get; }
    }

    public interface IVectorBufferDeclaration
    {
        public IVectorBufferDeclaration Clone();
        public IVectorBuffer CreateBuffer(int initCapacity);
    }
}