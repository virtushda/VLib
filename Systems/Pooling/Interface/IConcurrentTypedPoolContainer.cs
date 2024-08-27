using System;
using System.Collections.Concurrent;

namespace VLib
{
    /// <summary> Interface for implementing support for a pool of typed pools, backed by a concurrent dictionary. </summary>
    public interface IConcurrentTypedPoolContainer
    {
        public ConcurrentDictionary<Type, IPool> Map { get; }
    }
}