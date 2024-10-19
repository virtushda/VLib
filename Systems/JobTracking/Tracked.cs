using System;
using UnityEngine;

namespace VLib
{
    /// <summary> Wrapped for native collections that integrates with the Native Sentinel Seth Shit, Simplifies chaining unrelated jobs.
    /// Multiply to create compound job handle.
    /// Add to create lists of Tracked-T. </summary>
    public unsafe struct Tracked<TVal> : IDisposable, ITracked, IComparable<Tracked<TVal>>
        where TVal : unmanaged, IDisposable
    {
        public static implicit operator bool(Tracked<TVal> tracked) => tracked.IsCreated;
        public static implicit operator TVal(Tracked<TVal> tracked) => tracked.Value;
        public static implicit operator ulong(Tracked<TVal> tracked) => tracked.ID;
        
        RefStruct<TVal> valueHolder;

        /// <summary> Only checks that the container is created. </summary>
        public bool IsCreated => IsCreatedReadOnly;
        /// <summary> Only checks that the container is created. (Readonly can help avoid struct copies) </summary>
        public readonly bool IsCreatedReadOnly => valueHolder.IsCreated;

        public readonly TVal Value
        {
            get
            {
/*#if UNITY_EDITOR
                if (!IsCreated)
                    throw new UnityException("CRITICAL: Something is calling '.Value' on a Tracked<T> that is not created, in a build this would not be checked!");
#endif*/
                return valueHolder.ValueCopy;
            }
        }
        
        public readonly ref TVal ValueRef
        {
            get
            {
/*#if UNITY_EDITOR
                if (!IsCreated)
                    throw new UnityException("CRITICAL: Something is calling '.ValueRef' on a Tracked<T> that is not created, in a build this would not be checked!");
#endif*/
                return ref valueHolder.ValueRef;
            }
        }
        
        public readonly TVal* ValuePtr
        {
            get
            {
/*#if UNITY_EDITOR
                if (!IsCreated)
                    throw new UnityException("CRITICAL: Something is calling 'ValuePtrUnsafe' on a Tracked<T> that is not created, in a build this would be a rogue pointer!");
#endif*/
                return valueHolder.ValuePtr;
            }
        }
        
        // Removed for now with addition of RefStruct
        /// <summary> Safety checks in editor AND build. </summary>
        //public readonly TVal* ValuePtrSafe => IsCreated ? valueHolder.TPtr : throw new UnityException("Tracked is not created!");

        /// <summary> Does not verify! If the internal value holder is not setup, you'll get zero, an invalid ID! </summary>
        public readonly ulong ID => valueHolder.SafetyID; // IsCreated ? ((IntPtr) valueHolder.Ptr).ToInt64() : throw new UnityException("Tracked is not created!");

        public Tracked(in TVal value) : this() => valueHolder = RefStruct<TVal>.Create(value);

        /// <summary> Recommend to use <see cref="ITrackedExt.DisposeTrackedToDefault(x,bool)"/> wherever possible. This simply satisfies IDisposable </summary>
        public void Dispose() => Dispose(true);

        public void Dispose(bool reportException)
        {
            //Gotta protect this call, some code is awkward and disposes this before it's initialized...
            try
            {
                if (valueHolder.IsCreated)
                {
                    TrackedCollectionManager.CompleteAllJobsFor(ID);
                    valueHolder.ValueRef.DisposeRefToDefault();
                }
            }
            catch (Exception e)
            {
                if (reportException)
                {
                    Debug.LogError("Caught exception while disposing internal value... Logging exception and continuing...");
                    Debug.LogException(e);
                }
                else
                    Debug.LogWarning($"Caught intentionally muted exception while disposing internal value, msg: {e.Message}");
            }
            valueHolder.DisposeRefToDefault();
        }

        public int CompareTo(Tracked<TVal> other) => ID.CompareTo(other.ID);
    }
}