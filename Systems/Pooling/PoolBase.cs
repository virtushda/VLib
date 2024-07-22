using System.Threading;
using UnityEngine;

namespace VLib
{
    public abstract class PoolBase<TCollection, TPoolable> : IPool<TPoolable>
    {
        protected const int DefaultInitialCapacity = 8;

        protected TCollection collection;
        protected int initCapacity = DefaultInitialCapacity;

        public int TotalObjectCount => PooledCount + TakenCount;
        
        public abstract int PooledCount { get; }
        
        int takenCount;
        public int TakenCount => takenCount;
        protected ref int TakenCountRef => ref takenCount;
        protected void IncrementTakenCount() => Interlocked.Increment(ref takenCount);
        protected void DecrementTakenCount() => Interlocked.Decrement(ref takenCount);
        
        public PoolBase() => collection = GetNewCollection();

        public PoolBase(int initPoolCapacity)
        {
            initCapacity = initPoolCapacity;
            collection = GetNewCollection(initCapacity);
        }
        
        // Collection Read Methods
        public abstract void SetCollectionCapacity();
        protected abstract TCollection GetNewCollection(int initCapacity = DefaultInitialCapacity);
        protected abstract void AddCollectionItem(TPoolable item);
        /// <summary> Get a poolable from the collection. </summary>
        /// <param name="suggestedIndex">The index is 'suggested' because not all collection types can be read from by index.</param> 
        protected abstract bool TryTakeFromCollection(int suggestedIndex, out TPoolable poolable);

        protected abstract void ClearCollection();

        /// <summary> Override TryTakeFromCollection if you want Fetch() to create a new item if the collection is empty.
        /// I tried to automate this with generics but the constraints required make the code extremely fickle compared to this approach.</summary> 
        public virtual TPoolable Fetch()
        {
            if (TryTakeFromCollection(PooledCount - 1, out var poolable))
                return poolable;

            Debug.LogError("Pool is empty!");
            return default;
        }

        public virtual bool TryFetch(out TPoolable poolable)
        {
            poolable = default;
            return TryTakeFromCollection(PooledCount - 1, out poolable);
        }

        public virtual void Repool(TPoolable objToPool) => AddCollectionItem(objToPool);
        
        public virtual void ClearAll() => ClearCollection();

        ///<summary> Clears all pools and resets the internal counters.
        /// Dumps all pooling to GC. This is a suitable way to "dispose" a static pool that you intend to use again later. </summary>
        public virtual void DumpAll()
        {
            ClearAll();
            TakenCountRef = 0;
        }
    }
}