namespace VLib
{
    /// <summary> For type enforcement </summary>
    public interface IPool
    {
        public int PooledCount { get; }

        public void ClearPooled();
    }
    
    public interface IPool<T> : IPool
    {
        public T Depool(bool runPostProcessAction = true);

        public void Repool(T objToPool, bool runPreProcessAction = true);
    }
}