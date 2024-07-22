namespace VLib
{
    /// <summary> For type enforcement </summary>
    public interface IPool
    {
        public int PooledCount { get; }

        public void ClearAll();
    }
    
    public interface IPool<T> : IPool
    {
        public T Fetch();

        public bool TryFetch(out T poolable);

        public void Repool(T objToPool);
    }
}