//using System.Runtime.Remoting.Messaging;

using System;

namespace VLib
{
    /// <summary> Automatic implementation for anything that can be created with a parameter-less constructor </summary>
    public class ParameterlessVPool<T> : VPool<T>
        where T : new()
    {
        /// <summary> Parameterless constructor for very simple cases. Check the other constructor for more advanced options. </summary>
        public ParameterlessVPool() : this(16) { }

        /// <inheritdoc />
        public ParameterlessVPool(
            int initPoolCapacity, 
            Action<T> depoolPostProcess = null, 
            Action<T> repoolPreProcess = null, 
            Action<T> disposalAction = null)
            : base(initPoolCapacity, depoolPostProcess, repoolPreProcess, () => new T(), disposalAction)
        { }
    }
}