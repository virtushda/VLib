using System;
using System.Collections.Generic;

namespace VLib
{
    /// <summary> A straight-forward customizable pool that uses a regular List<T> for storage. </summary>
    public class VPool<T> : VPoolBase<List<T>, T>, IPool<T>
    {
        /// <inheritdoc />
        protected VPool(
            int initPoolCapacity = DefaultInitialCapacity,
            Action<T> depoolPostProcess = null,
            Action<T> repoolPreProcess = null,
            Func<T> creationAction = null,
            Action<T> disposalAction = null) 
            : base(initPoolCapacity, depoolPostProcess, repoolPreProcess, creationAction, disposalAction) { }

        public override int PooledCount => collection.Count;

        protected override List<T> GetNewCollection(int initCapacity = DefaultInitialCapacity) => new(initCapacity);

        protected override void AddCollectionItem(T item) => collection.Add(item);

        protected override bool TryTakeFromCollection(int suggestedIndex, out T poolable)
        {
            if (PooledCount < 1)
            {
                poolable = default;
                return false;
            }
            poolable = collection[suggestedIndex];
            collection.RemoveAt(suggestedIndex);
            return true;
        }

        protected override void ClearCollection()
        {
            TakenCountRef -= collection.Count;
            collection.Clear();
        }
    }
}