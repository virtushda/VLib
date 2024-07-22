using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;

namespace VLib
{
    public class SimplePool<T> : PoolBase<List<T>, T>, IPool<T>
    {
        protected SimplePool(int initPoolCapacity) : base(initPoolCapacity) { }

        protected SimplePool() { }

        public override int PooledCount => collection.Count;

        protected override List<T> GetNewCollection(int initCapacity = DefaultInitialCapacity) => new(initCapacity);

        protected override void AddCollectionItem(T item)
        {
            collection.Add(item);
            DecrementTakenCount();
        }

        public override void SetCollectionCapacity()
        {
            collection.Capacity = initCapacity;
            var lengthDecrease = math.max(0, collection.Count - initCapacity);
            TakenCountRef -= lengthDecrease;
        }

        protected override bool TryTakeFromCollection(int suggestedIndex, out T poolable)
        {
            if (PooledCount < 1)
            {
                poolable = default;
                return false;
            }

            poolable = collection[suggestedIndex];
            collection.RemoveAt(suggestedIndex);
            IncrementTakenCount();
            return true;
        }

        protected override void ClearCollection()
        {
            TakenCountRef -= collection.Count;
            collection.Clear();
        }
    }
}