//using System.Runtime.Remoting.Messaging;

namespace VLib
{
    /// <summary> Automatic implementation for anything that can be created with a parameter-less constructor </summary>
    public class ParameterlessPool<T> : SimplePool<T>, IPooledItemCreator<T>
        where T : new()
    {
        public ParameterlessPool() { }
        
        public ParameterlessPool(int initPoolCapacity) : base(initPoolCapacity) { }

        public virtual T CreateNewItem()
        {
            IncrementTakenCount();
            return new T();
        }

        protected override bool TryTakeFromCollection(int suggestedIndex, out T poolable)
        {
            if (base.TryTakeFromCollection(suggestedIndex, out poolable))
                return true;
            
            poolable = CreateNewItem();
            return true;
        }
    }
}