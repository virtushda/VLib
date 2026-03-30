using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace VLib.Utility
{
    /// <summary> A more flexible lazy initializer. Has a value factory and an optional initializer action. </summary>
    public class VLazy<T>
        where T : class
    {
        readonly object lockObject = new();
        readonly Func<T> valueFactory;

        T _value;
        T Value
        {
            get => Volatile.Read(ref _value);
            set => Volatile.Write(ref _value, value);
        }

        /// <summary> Setup a lazy initializing value. </summary>
        /// <param name="valueFactory"> The factory method that creates the value. <br/>
        /// If this is null, it will use Activator.CreateInstance, or throw an exception if the type has no parameterless constructor. </param>
        public VLazy(Func<T> valueFactory)
        {
            this.valueFactory = valueFactory;

            // If no value factory, create one that uses Activator.CreateInstance
            // Throw exception if this is not possible
            if (valueFactory == null)
            {
                var type = typeof(T);
                if (type.GetConstructor(Type.EmptyTypes) == null)
                    throw new InvalidOperationException($"Type {type.FullName} has no parameterless constructor, so it cannot be used with VLazy<T> without a value factory.");
                this.valueFactory = () => (T) Activator.CreateInstance(type);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetExistingValue(out T existingValue)
        {
            existingValue = Value;
            return existingValue != null;
        }

        /// <summary> Fetches or creates the value, with an optional preprocess action. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetValue(Action<T> preprocess)
        {
            // Fast check outside lock
            if (Value == null)
            {
                // If null, enter lock to avoid race condition
                lock (lockObject)
                {
                    // Check null again inside lock to avoid repeat work
                    if (Value == null)
                    {
                        // Create the value in a separate method
                        // Avoid assigning the reference to the value until it's fully initialized so the outer null check doesn't see a half-initialized value
                        if (preprocess == null)
                            Value = Create();
                        else
                        {
                            var newValue = Create();
                            preprocess(newValue);
                            Value = newValue;
                        }
                    }
                }
            }

            return Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        T Create() => valueFactory();

        /// <summary> Nulls the internal value, forcing a new value to be created next time. </summary>
        public void Clear()
        {
            lock (lockObject)
                Value = null;
        }
    }
}