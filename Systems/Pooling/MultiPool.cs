using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace VLib
{
    /// <summary> Thread-safe pool of pools, internal pools may or may not be thread-safe! </summary>
    public class MultiPool : IConcurrentDictionaryPoolContainer, ISafeDisposable
    {
        /// <summary> Expect TPool to be an IPool of type! </summary>
        public ConcurrentDictionary<Type, IPool> Map { get; private set; } = new();

        protected MultiPool() { }
        
        public void SafeDispose()
        {
            this.ClearAllPools();
            Map = null;
        }

        // Implemented in interfaces for now
        /*//
        public virtual bool TryGetPool<TPool, TPoolable>(out TPool pool)
            where TPool : IPool<TPoolable>
        {
            return this.TryGetExistingPool<MultiPool, TPool, TPoolable>(out pool);
        }
        
        public virtual bool RemovePool(Type key) => this.TryRemovePool()*/
    }
}