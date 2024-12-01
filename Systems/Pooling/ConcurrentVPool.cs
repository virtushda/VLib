using System;
using System.Collections.Concurrent;

namespace VLib
{
    /// <summary> A simple VPool variant backed by a concurrent collection for automatic thread-safety. </summary>
    public class ConcurrentVPool<T> : VPoolBase<ConcurrentQueue<T>, T>
    {
        /// <inheritdoc />
        public ConcurrentVPool
            (int initPoolCapacity = DefaultInitialCapacity, 
                Action<T> depoolPostProcess = null, 
                Action<T> repoolPreProcess = null, 
                Func<T> creationAction = null, 
                Action<T> disposalAction = null)
            : base(initPoolCapacity, depoolPostProcess, repoolPreProcess, creationAction, disposalAction) { }

        public override int PooledCount => collection.Count;

        protected override ConcurrentQueue<T> GetNewCollection(int initCapacity = DefaultInitialCapacity) => new();

        protected override void AddCollectionItem(T item) => collection.Enqueue(item);
        
        protected override bool TryTakeFromCollection(int suggestedIndex, out T poolable) => collection.TryDequeue(out poolable);
        
        protected override void ClearCollection() => collection.Clear();
    }
}