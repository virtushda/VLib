using System;

namespace VLib
{
    /// <summary> A thread-safe pool for objects with parameterless constructors.  </summary> 
    public class ConcurrentVPoolParameterless<T> : ConcurrentVPool<T>
        where T : new()
    {
        public ConcurrentVPoolParameterless() : this(DefaultInitialCapacity) { }
        
        /// <inheritdoc />
        public ConcurrentVPoolParameterless
            (int initPoolCapacity = DefaultInitialCapacity, 
                Action<T> depoolPostProcess = null, 
                Action<T> repoolPreProcess = null, 
                Action<T> disposalAction = null)
            : base(initPoolCapacity, depoolPostProcess, repoolPreProcess, () => new T(), disposalAction) { }
    }
}