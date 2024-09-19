using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace VLib
{
    /// <summary>
    /// Allows multiple keys of the same value
    /// </summary>
    /// <typeparam name="K"></typeparam>
    /// <typeparam name="V"></typeparam>
    [GenerateTestsForBurstCompatibility]
    public struct VUnsafeSortedList<K, V> : IAllocating
        where K : unmanaged, IComparable<K>, IEquatable<K>
        where V : unmanaged
    {
        VUnsafeSortedList<K> keys;
        VUnsafeList<V> values;
        public VUnsafeSortedList<K> Keys => keys;
        public VUnsafeList<V> Values => values;

        public bool IsCreated => keys.IsCreated && values.IsCreated;

        public int Length
        {
            get => keys.Length;
            set
            {
                keys.Length = value;
                values.Length = value;
            }
        }

        public int Capacity
        {
            get => keys.Capacity;
            set
            {
                keys.Capacity = value;
                values.Capacity = value;
            }
        }
        
        public VUnsafeSortedList(int initCapacity, Allocator allocator)
        {
            keys = new VUnsafeSortedList<K>(initCapacity, allocator);
            values = new VUnsafeList<V>(initCapacity, allocator);
        }

        /// <summary> Prefer extension 'DisposeRef' for safety and simplicity. </summary>
        public void Dispose()
        {
            keys.Dispose();
            keys = default;
            values.Dispose();
            values = default;
        }
        
        public V Read(K key)
        {
            this.ConditionalCheckIsCreated();
            if (TryGetIndex(key, out int index))
                return values[index];
            throw new KeyNotFoundException($"Key {key} not found.");
        }

        /// <summary> Can add duplicates. </summary>
        public void Add(K key, V value)
        {
            this.ConditionalCheckIsCreated();
            bool exists = TryGetIndex(key, out int insertIndex);
            if (exists)
            {
                //Insert directly into the internal list, otherwise it'll search twice
                keys.list.Insert(insertIndex, key);
                values.Insert(insertIndex, value);
            }
            else
            {
                keys.list.Insert(~insertIndex, key);
                values.Insert(~insertIndex, value);
            }
        }

        /// <summary> If the value is already in the collection, it will be rejected. </summary>
        public bool TryAddExclusive(K key, V value)
        {
            this.ConditionalCheckIsCreated();
            bool exists = TryGetIndex(key, out int insertIndex);
            if (exists)
            {
                if (keys[insertIndex].Equals(key))
                    return false;

                keys.Insert(insertIndex, key);
                values.Insert(insertIndex, value);
            }
            else
            {
                keys.Insert(~insertIndex, key);
                values.Insert(~insertIndex, value);
            }

            return true;
        }

        /// <summary> If the object is already in the collection OR an IComparer implementation decides the object already exists in this collection, it will be rejected. </summary>
        public bool TryAddExclusiveStrict(K key, V value)
        {
            this.ConditionalCheckIsCreated();
            if (TryGetIndex(key, out int insertIndex))
                return false;

            insertIndex = ~insertIndex;
            keys.Insert(insertIndex, key);
            values.Insert(insertIndex, value);
            return true;
        }

        /// <summary> Has editor-only assertions to check sorting, runtime is not verified! </summary>
        public void TryInsertUnsafe(int index, K key, V value)
        {
            this.ConditionalCheckIsCreated();
            keys.Insert(index, key);
            values.Insert(index, value);
        }

        /// <summary> Does not check for equality, only for IComparable{T} == 0. </summary>
        public bool ReplaceValueStrict(K key, V value)
        {
            this.ConditionalCheckIsCreated();
            if (TryGetIndex(key, out int replaceIndex))
            {
                values[replaceIndex] = value;
                return true;
            }
            return false;
        }
        
        /// <summary> Does not check for equality, only for IComparable{T} == 0. This is faster. </summary>
        public bool RemoveStrict(K key)
        {
            this.ConditionalCheckIsCreated();
            if (TryGetIndex(key, out int index))
            {
                keys.RemoveAt(index);
                values.RemoveAt(index);
                return true;
            }
            return false;
        }

        /// <summary> Checks for IComparable{T} == 0 and equality. Slower, but necessary to if you're adding multiple keys that are the same according to IComparable{T}. </summary>
        public bool RemoveEquals(K key)
        {
            this.ConditionalCheckIsCreated();
            if (!keys.FindKeyEquals(key, out int index))
                return false;
            RemoveAt(index);
            return true;
        }

        public void RemoveAt(int index)
        {
            this.ConditionalCheckIsCreated();
            VCollectionUtils.ConditionalCheckIndexValid(index, Length);
            keys.RemoveAt(index);
            values.RemoveAt(index);
        }

        public void Clear()
        {
            this.ConditionalCheckIsCreated();
            keys.Clear();
            values.Clear();
        }

        public void CopyTo(V[] array, int arrayIndex) => throw new NotImplementedException("Just access the list with .Values");

        /// <summary> Moderate speed, has to perform a binary search. </summary>
        public bool ContainsKey(K key)
        {
            this.ConditionalCheckIsCreated();
            return keys.Contains(key);
        }

        // If needed, implement as extension so that IEquatable{T} can be a constraint.
        /*/// <summary> Has to search the whole value collection linearly. </summary>
        public bool ContainsValueSlow(V value)
        {
            this.ConditionalCheckIsCreated();
            VUnsafeListExtensions.Contains(values, value);
            return values.Contains(value);
        }*/

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool VerifyIndex(int index) => index >= 0 && index < keys.Count;

        /// <summary> If you want safety, use TryGetKey </summary> 
        public K GetKeyAtIndex(int index) => keys[index];
        
        /// <summary> If you want safety, use TryGetValueAtIndex </summary>
        public V GetValueAtIndex(int index) => values[index];
        
        public bool TryGetValueAtIndex(int index, out V value) => values.TryGetValue(index, out value);

        /// <summary> Returns true if the key exists, either way returns the index where it thinks the key does/should exist. If the key does not exist, the index will need to be flipped with ~. </summary>
        public bool TryGetIndex(K key, out int index)
        {
            index = keys.IndexOfComparableMatch(key);
            return index >= 0;
        }
        
        public bool TryGetKey(int index, out K key)
        {
            if (VerifyIndex(index))
            {
                key = keys[index];
                return true;
            }

            key = default;
            return false;
        }

        /// <summary> Get value associated with first matching key with no safety checks.
        /// Collection supports multiple identical keys for advanced behaviour, be careful how you use it!!! </summary> 
        public V GetValueByKey(K key) => values[keys.IndexOfComparableMatch(key)];
        
        public bool TryGetValue(K key, out V value)
        {
            if (TryGetIndex(key, out int index))
            {
                value = values[index];
                return true;
            }

            value = default;
            return false;
        }

        public bool TryGetValue(K key, out V value, out int index)
        {
            if (TryGetIndex(key, out index))
            {
                value = values[index];
                return true;
            }

            value = default;
            return false;
        }
        
        public bool TrySetValue(K key, V value)
        {
            if (!TryGetIndex(key, out int index))
                return false;
            values[index] = value;
            return true;

        }
        
        public bool TrySetValueAtIndex(int index, V value)
        {
            if (!VerifyIndex(index))
                return false;
            values[index] = value;
            return true;
        }
        
        /*public (K key, V value) GetRandomKeyValuePair(bool remove)
        {
            int index = Random.Range(0, Count);
            
            K key = keys[index];
            V val = values[index];
            
            if (remove)
            {
                keys.RemoveAt(index);
                values.RemoveAt(index);
            }

            return (key, val);
        }

        public class VSimRangeException : UnityException
        {
            public VSimRangeException(int index, int maxCount)
            {
                if (index < 0)
                    throw new ArgumentOutOfRangeException($"Index {index} is less than 0!");
                if (index >= maxCount)
                    throw new ArgumentOutOfRangeException($"Index {index} greater than max index {maxCount - 1}!");
                throw new SystemException("Value does not appear to be out of range...");
            }
        }*/

        //public IEnumerator<V> GetEnumerator() => values.GetEnumerator();

        //IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}