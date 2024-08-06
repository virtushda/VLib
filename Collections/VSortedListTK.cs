using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

namespace VLib
{
    /// <summary>
    /// Allows multiple keys of the same value
    /// </summary>
    /// <typeparam name="K"></typeparam>
    /// <typeparam name="V"></typeparam>
    [Serializable]
    public class VSortedList<K, V>// : IReadOnlyList<V>
        where K : IComparable<K>
    {
        [SerializeField] VSortedList<K> keys;
        [SerializeField] List<V> values;
        public VSortedList<K> Keys => keys;
        public List<V> Values => values;

        public int Count => keys.Count;

        /*public V this[int index]
        {
            get
            {
                if (VerifyIndex(index))
                    return values[index];
                else
                    throw new VSimRangeException(index, keys.Count);
            }
            set
            {
                if (VerifyIndex(index))
                    values[index] = value;
                else
                    throw new VSimRangeException(index, keys.Count);
            }
        }*/

        // Ambiguous, indexxer was always used
        /*public V this[K key]
        {
            get
            {
                if (TryGetIndex(key, out int index))
                    return values[index];
                else
                    throw new KeyNotFoundException($"Key {key} not found.");
            }
            set
            {
                if (TryGetIndex(key, out int index))
                    values[index] = value;
                else
                    throw new KeyNotFoundException($"Key {key} not found.");
            }
        }*/

        public VSortedList()
        {
            keys = new VSortedList<K>();
            values = new List<V>();
        }

        public VSortedList(int initCapacity)
        {
            keys = new VSortedList<K>(initCapacity);
            values = new List<V>(initCapacity);
        }

        public VSortedList(int initCapacity, IComparer<K> comparer)
        {
            keys = new VSortedList<K>(initCapacity, comparer);
            values = new List<V>(initCapacity);
        }

        public V Read(K key)
        {
            if (TryGetIndex(key, out int index))
                return values[index];
            throw new KeyNotFoundException($"Key {key} not found.");
        }

        /// <summary> Can add duplicates, and anything an IComparer says is 'equal'. </summary>
        public void Add(K key, V value)
        {
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

        /// <summary> If the object is already in the collection, it will be rejected. </summary>
        public bool TryAddExclusive(K key, V value)
        {
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
            keys.Insert(index, key);
            values.Insert(index, value);
        }
        
        public bool ReplaceValueStrict(K key, V value)
        {
            if (TryGetIndex(key, out int replaceIndex))
            {
                values[replaceIndex] = value;
                return true;
            }
            return false;
        }

        public bool Remove(K key)
        {
            if (TryGetIndex(key, out int index))
            {
                keys.RemoveAt(index);
                values.RemoveAt(index);
                return true;
            }
            return false;
        }

        public void RemoveAt(int index)
        {
            if (VerifyIndex(index) == false)
                throw new VSimRangeException(index, keys.Count);

            keys.RemoveAt(index);
            values.RemoveAt(index);
        }

        public void Clear()
        {
            keys.Clear();
            values.Clear();
        }

        public void CopyTo(V[] array, int arrayIndex) => throw new NotImplementedException("Just access the list with .Values");

        public bool ContainsKey(K key)
        {
            return keys.Contains(key);
        }

        public bool ContainsValue(V value)
        {
            return values.Contains(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool VerifyIndex(int index) => index >= 0 && index < keys.Count;

        /// <summary> If you want safety, use TryGetKey </summary> 
        public K GetKeyAtIndex(int index) => keys[index];
        
        /// <summary> If you want safety, use TryGetValueAtIndex </summary>
        public V GetValueAtIndex(int index) => values[index];
        
        public bool TryGetValueAtIndex(int index, out V value) => values.TryGet(index, out value);

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
        
        public (K key, V value) GetRandomKeyValuePair(bool remove)
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
        }

        //public IEnumerator<V> GetEnumerator() => values.GetEnumerator();

        //IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}