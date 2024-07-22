using System;
using System.Collections.Concurrent;

namespace VLib
{
    public abstract class ThreadSafePoolBase<T> : PoolBase<ConcurrentBag<T>, T>
    {
        public override int PooledCount => collection.Count;
        protected override ConcurrentBag<T> GetNewCollection(int initCapacity = DefaultInitialCapacity) => new();

        public override void SetCollectionCapacity() => throw new NotImplementedException("ConcurrentBag does not have a capacity property");

        protected override void AddCollectionItem(T item)
        {
            collection.Add(item);
            DecrementTakenCount();
        }

        protected override void ClearCollection()
        {
            TakenCountRef -= collection.Count;
            collection.Clear();
        }

        protected override bool TryTakeFromCollection(int suggestedIndex, out T poolable)
        {
            if (!collection.TryTake(out poolable))
                return false;
            IncrementTakenCount();
            return true;
        }
    }
}