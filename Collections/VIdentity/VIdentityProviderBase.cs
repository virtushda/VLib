using System;
using UnityEngine;

namespace VLib
{
    // TODO: This could be burstified
    public abstract class VIdentityProviderBase<T> : IVIdentityProvider<T>
        where T : unmanaged, IComparable<T>, IEquatable<T>
    {
        readonly object lockObj = new();
        public object LockObj => lockObj;
        
        public abstract T MinValue { get; }
        
        // Sorted list seems to be the best performance balance...
        protected VSortedList<T> free = new(256);

        protected T nextValue;
        public T NextValue => nextValue;

        public int PoolCount() => free.Count;

        public T FetchID() => PoolCount() > 0 ? GetPooledID() : GetNewID();

        public void ReturnID(T returnedID)
        {
            if (ValueIsHighestClaimed(returnedID))
            {
                // Attempt to walk 'nextvalue' backwards and free 'free' ids to save memory
                var id = returnedID;
                int idIndexInFree = -1;
                
                do
                {
                    nextValue = DecrementValue(nextValue);
                    
                    // Do pass this is always -1, next pass depends on while statement
                    if (idIndexInFree >= 0)
                    {
                        free.RemoveAt(idIndexInFree);
                    }
                    id = DecrementValue(id);
                    
                }  // If ID is highest claimed AND known to be free
                while (ValueIsHighestClaimed(id) && (idIndexInFree = free.IndexOfComparableMatch(id)) >= 0); // Ensure backstep is safe!
            }
            else
            {
                // Otherwise add to known free values
                free.TryAddExclusiveStrict(returnedID);
            }
        }

        public bool TryClaimID(T id)
        {
            // Guard against attempts to claim values below minimum
            if (id.CompareTo(MinValue) < 0)
                return false;
            
            var comparison = id.CompareTo(nextValue);
            if (comparison < 0) // Less than next value
            {
                var binaryResult = free.IndexOfComparableMatch(id);
                if (binaryResult >= 0) // Is free
                {
                    free.RemoveAt(binaryResult);
                    return true;
                }
                return false;
            }
            if (comparison > 0) // Greater than next value
            {
                // Add all intermediate values to free list directly
                var nextValueStart = nextValue;
                nextValue = IncrementValue(id);
                
                // Fill up to claimed ID
                for (T i = nextValueStart; i.CompareTo(id) < 0; i = IncrementValue(i))
                {
                    // Direct add to sorted list, all these values MUST be higher or something wrong
                    free.list.Add(i);
                }

                return true;
            }

            // ID must be next value
            GetNewID();
            return true;
        }

        public void Reset()
        {
            free.Clear();
            ResetNextValue();
        }

        T GetPooledID()
        {
            int lastIndexInFree = free.Count - 1;
            T value = free[lastIndexInFree];
            free.RemoveAt(lastIndexInFree);
            return value;
        }

        T GetNewID()
        {
            var newID = nextValue;
            nextValue = IncrementValue(nextValue);
            if (nextValue.Equals(MaxPossibleValue()) && free.Count < 1)
                Debug.LogError($"{this.GetType()} has run out of space!");
            return newID;
        }

        bool ValueIsHighestClaimed(T value) => value.Equals(DecrementValue(nextValue));

        T IncrementValue(T value) => OffsetValue(value, 1);

        T DecrementValue(T value) => OffsetValue(value, -1);
        
        protected abstract void ResetNextValue();

        protected abstract T OffsetValue(T value, int offset);

        protected abstract T MaxPossibleValue();
    }
}