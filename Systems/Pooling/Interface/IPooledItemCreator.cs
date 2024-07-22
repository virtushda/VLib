namespace VLib
{
    public interface IPooledItemCreator<T> : IPool<T>
        where T : new()
    {
        public T CreateNewItem();
    }
}