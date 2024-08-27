using System;
using System.Collections.Concurrent;

namespace VLib
{
    /// <summary> Thread-safe pool of pools, internal pools may or may not be thread-safe! </summary>
    public class TypedMultiPool : IConcurrentTypedPoolContainer, ISafeDisposable
    {
        /// <summary> Expect TPool to be an IPool of type! </summary>
        public ConcurrentDictionary<Type, IPool> Map { get; private set; } = new();

        protected TypedMultiPool() { }
        
        public void SafeDispose()
        {
            this.ClearAllPools();
            Map = null;
        }
    }
}