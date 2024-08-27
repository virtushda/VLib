using System;
using System.Collections.Concurrent;

namespace VLib
{
    public class MultiPoolParameterless : IConcurrentTypedPoolContainer, ISafeDisposable
    {
        public ConcurrentDictionary<Type, IPool> Map { get; } = new();

        /// <summary>Good for static implementations, </summary>
        public void SafeDispose()
        {
            this.ClearAllPools();
        }
    }
}