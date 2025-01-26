using System;
using System.Threading;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace VLib
{
    /// <summary> The base for a flexible and performant managed object pool. <br/>
    /// There includes support for: <br/>
    /// - Changing the collection type. (Including to concurrent types) <br/>
    /// - Custom action to run immediately after depooling an object. <br/>
    /// - Custom action to run immediately before repooling an object. <br/>
    /// - Custom action to generate a new object. <br/>
    /// - Custom action to dispose an object. </summary>
    public abstract class VPoolBase<TCollection, TPoolable> : IPool<TPoolable>
    {
        protected const int DefaultInitialCapacity = 8;

        protected TCollection collection;
        protected int initCapacity = DefaultInitialCapacity;

        /// <summary> Pooled + Taken </summary>
        public int TotalObjectCount => PooledCount + TakenCount;

        /// <summary> The number of objects dormant in the pool. </summary>
        public abstract int PooledCount { get; }
        
        int takenCount;

        /// <summary> The number of objects retrieved from this pool. </summary>
        public int TakenCount => takenCount;
        protected ref int TakenCountRef => ref takenCount;
        protected void IncrementTakenCount() => Interlocked.Increment(ref takenCount); // Interlocked to support thread-safe variants by default
        protected void DecrementTakenCount() => Interlocked.Decrement(ref takenCount); // Interlocked to support thread-safe variants by default
        
        #region Processing Actions

        /// <summary> Custom functionality to run on an element that has just been depooled </summary>
        protected Action<TPoolable> depoolPostProcess;
        /// <summary> Custom functionality to run on an element that is about to be repooled </summary>
        protected Action<TPoolable> repoolPreProcess;
        /// <summary> Custom functionality to generate a new element </summary>
        protected Func<TPoolable> createAction;
        /// <summary> Custom functionality to run on an element that is about to be disposed </summary>
        protected Action<TPoolable> disposeAction;
        
        #endregion

        /// <summary> Create a new pool with no special capabilities. (Simple constructor) </summary>
        public VPoolBase() => collection = GetNewCollection();
        
        /// <summary> Create a new pool. </summary>
        /// <param name="initPoolCapacity">The initial capacity of the pool.</param>
        /// <param name="depoolPostProcess"> <see cref="depoolPostProcess"/> </param> // Inherit doc not working here, 'see' will do...
        /// <param name="repoolPreProcess"> <see cref="repoolPreProcess"/> </param>
        /// <param name="creationAction"> <see cref="createAction"/> </param>
        /// <param name="disposalAction"> <see cref="disposeAction"/> </param>
        public VPoolBase(
            int initPoolCapacity,
            Action<TPoolable> depoolPostProcess = null,
            Action<TPoolable> repoolPreProcess = null,
            Func<TPoolable> creationAction = null,
            Action<TPoolable> disposalAction = null)
        {
            initCapacity = initPoolCapacity;
            collection = GetNewCollection(initCapacity);
            
            this.depoolPostProcess = depoolPostProcess;
            this.repoolPreProcess = repoolPreProcess;
            createAction = creationAction;
            disposeAction = disposalAction;
        }
        
        #region Collection Management
        
        protected abstract TCollection GetNewCollection(int initCapacity = DefaultInitialCapacity);
        protected abstract void AddCollectionItem(TPoolable item);
        /// <summary> Get a poolable from the collection. </summary>
        /// <param name="suggestedIndex">The index is 'suggested' because not all collection types can be read from by index.</param>
        /// <returns>True if an element was returned.</returns>
        protected abstract bool TryTakeFromCollection(int suggestedIndex, out TPoolable poolable);
        /// <summary> Only responsible for actually clearing the collection! Any disposal action should be handled with <see cref="disposeAction"/> </summary>
        protected abstract void ClearCollection();
        
        #endregion

        /// <summary> Pulls from the pool, OR if a <see cref="createAction"/> is specified (optional in the constructor), a new element can be generated on-the-fly. </summary> 
        public TPoolable Depool(bool runPostProcessAction = true)
        {
            // Try depool
            if (!TryTakeFromCollection(PooledCount - 1, out var poolable))
            {
                // If depool fails, try to create a new item
                if (createAction == null)
                {
                    Debug.LogError("Pool is empty!");
                    return default;
                } 
                
                poolable = createAction.Invoke();
            }

            if (runPostProcessAction)
                depoolPostProcess?.Invoke(poolable);
            IncrementTakenCount();
            return poolable;
        }

        public void Repool(TPoolable objToPool, bool runPreProcessAction = true)
        {
            if (runPreProcessAction)
                repoolPreProcess?.Invoke(objToPool);
            AddCollectionItem(objToPool);
            DecrementTakenCount();
        }

        /// <summary> Disposes pool elements until the pooled count == <see cref="length"/>. This can be used to clamp the size of the pool. </summary>
        public void DisposePastLength(int length)
        {
            length = math.max(0, length);
            int pooledCountCache;
            while ((pooledCountCache = PooledCount) > length) // Guard against overrides of TryTakeFromCollection that can create new items
            {
                // Pull object
                if (!TryTakeFromCollection(pooledCountCache - 1, out var poolable))
                {
                    Debug.LogError("Failed to take from collection during DisposePastLength");
                    break;
                }
                disposeAction?.Invoke(poolable);
            }
        }
        
        ///<summary> Pulls and disposes all pooled elements. </summary>
        public virtual void ClearPooled()
        {
            DisposePastLength(0);
            BurstAssert.True(PooledCount == 0); // Pool should be empty
        }

        /// <summary> Clears the pool and resets the 'taken' counter. <br/>
        /// Dumps all pooling to GC unless overriden. This is a suitable way to "dispose" a static pool that you intend to use again later. </summary>
        public void DumpAll()
        {
            ClearPooled();
            Interlocked.Exchange(ref takenCount, 0);
        }
        
        public ScopedUser GetScopedUser() => new(this);
        
        /// <summary> A scope that automatically repools an object when disposed. </summary>
        public readonly struct ScopedUser : IDisposable
        {
            readonly VPoolBase<TCollection, TPoolable> pool;
            public readonly TPoolable Value;

            public ScopedUser(VPoolBase<TCollection, TPoolable> pool)
            {
                this.pool = pool;
                Value = pool.Depool();
            }

            public void Dispose() => pool.Repool(Value);
        }
    }
}