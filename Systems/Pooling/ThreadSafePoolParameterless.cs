namespace VLib
{
    /// <summary> A thread-safe pool for objects with parameterless constructors.  </summary> 
    public class ThreadSafePoolParameterless<T> : ThreadSafePoolBase<T>, IPooledItemCreator<T>
        where T : new()
    {
        public ThreadSafePoolParameterless() { }

        protected override bool TryTakeFromCollection(int suggestedIndex, out T poolable)
        {
            if (base.TryTakeFromCollection(suggestedIndex, out poolable))
                return true;
            poolable = CreateNewItem();
            return true;
        }

        public T CreateNewItem()
        {
            IncrementTakenCount();
            return new T();
        }
    }
}