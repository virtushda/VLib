﻿using System;
using System.Xml;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using VLib;

namespace Libraries.KeyedAccessors.Lightweight
{
    ///<summary> Generates a compact list of values, where values can be added or removed based on a given key. <br/>
    /// Has key->value mapping behaviour as well. <br/>
    /// Adds, removes, iterations, and contains checks are all very fast. Removes are the least fast, but still use a quick swapback pattern. </summary>
    public struct UnsafeKeyedListPair<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        // Key and value lists are aligned
        public UnsafeList<TKey> keys;
        public UnsafeList<TValue> values;
        public UnsafeHashMap<TKey, int> keyIndexMap;

        public readonly bool IsCreated => keys.IsCreated;
        public readonly int Length => keys.Length;

        public UnsafeKeyedListPair(Allocator allocator, int initCapacity = 16)
        {
            keys = new UnsafeList<TKey>(initCapacity, allocator);
            values = new UnsafeList<TValue>(initCapacity, allocator);
            keyIndexMap = new UnsafeHashMap<TKey, int>(initCapacity, allocator);
        }

        public void Dispose()
        {
            keys.Dispose();
            values.Dispose();
            keyIndexMap.Dispose();
        }

        /// <summary> Adds the key and value, returning true if it could add. Out parameter keyIndex is populated either way. </summary>
        public bool Add(TKey key, TValue value, out int keyIndex)
        {
            if (!keyIndexMap.TryGetValue(key, out keyIndex))
            {
                keyIndex = keys.Length;
                keyIndexMap.Add(key, keyIndex);
                keys.Add(key);
                values.Add(value);
                return true;
            }
            return false;
        }

        /// <summary> Add or Update existing </summary>
        /// <returns> True if added. </returns>
        public bool AddUpdate(TKey key, TValue value, out int index)
        {
            if (Add(key, value, out index))
                return true;
            values[index] = value;
            return false;
        }
        
        public bool Update(TKey key, TValue value, bool suppressError = false)
        {
            if (!keyIndexMap.TryGetValue(key, out var keyIndex))
            {
                if (!suppressError)
                    Debug.LogError($"Key {key} not found in list!");
                return false;
            }
            values[keyIndex] = value;
            return true;
        }
        
        public bool ContainsKey(TKey key) => keyIndexMap.ContainsKey(key);
        
        public ref TValue GetValueRef(TKey key)
        {
            var keyIndex = keyIndexMap[key];
            return ref values.ElementAt(keyIndex);
        }
        
        public bool TryGetIndex(TKey key, out int keyIndex) => keyIndexMap.TryGetValue(key, out keyIndex);

        public bool TryGetValueCopy(TKey key, out TValue value)
        {
            if (!keyIndexMap.TryGetValue(key, out var keyIndex))
            {
                value = default;
                return false;
            }
            value = values.ElementAt(keyIndex);
            return true;
        }

        ///<summary> A spooky version of try get, do NOT use the ref if success is false.
        /// Don't hold onto the ref for long! (You are responsible for thread safety).
        /// Returns a ref to the first element if the key could not be found, so make sure the collection isn't empty!</summary>
        public ref TValue TryGetValueRef(TKey key, out bool success)
        {
            if (!keyIndexMap.TryGetValue(key, out var keyIndex))
            {
                success = false;
                return ref values.ElementAt(0);
            }
            success = true;
            return ref values.ElementAt(keyIndex);
        }

        public bool RemoveSwapback(TKey key, out TValue removedValue)
        {
            if (!keyIndexMap.TryGetValue(key, out var removeKeyIndex))
            {
                removedValue = default;
                return false;
            }
            keyIndexMap.Remove(key);

            // Handle case: Only element
            if (keys.Length <= 1)
            {
                removedValue = values[0];
                keys.Clear();
                values.Clear();
                return true;
            }
            
            removedValue = values[removeKeyIndex];
            // Otherwise remove swapback for speed
            // Get last values
            var lastIndex = keys.Length - 1;
            var lastKey = keys[lastIndex];
            // Swapback
            keys.RemoveAtSwapBack(removeKeyIndex);
            values.RemoveAtSwapBack(removeKeyIndex);
            // Update index mapping, but only if the last key is not the removed key, setting this would re-add it to the map (was a bug, now fixed)
            if (removeKeyIndex != lastIndex)
                keyIndexMap[lastKey] = removeKeyIndex;

            return true;
        }

        public void Clear()
        {
            keys.Clear();
            values.Clear();
            keyIndexMap.Clear();
        }
    }
}