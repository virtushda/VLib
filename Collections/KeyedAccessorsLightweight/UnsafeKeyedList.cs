using System;
using System.Xml;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using VLib;

namespace Libraries.KeyedAccessors.Lightweight
{
    ///<summary> Generates a compact list of keys, where keys are also indexed.  <br/>
    /// Adds, removes, iterations, and contains checks are all very fast. Removes are the least fast, but still use a quick swapback pattern. </summary>
    public struct UnsafeKeyedList<TKey>
        where TKey : unmanaged, IEquatable<TKey>
    {
        // Key and value lists are aligned
        public UnsafeList<TKey> keys;
        public UnsafeHashMap<TKey, int> keyIndexMap;

        public bool IsCreated => keys.IsCreated;
        public int Length => keys.Length;

        public UnsafeKeyedList(Allocator allocator, int initCapacity = 16)
        {
            keys = new UnsafeList<TKey>(initCapacity, allocator);
            keyIndexMap = new UnsafeHashMap<TKey, int>(initCapacity, allocator);
        }

        public void Dispose()
        {
            keys.Dispose();
            keyIndexMap.Dispose();
        }

        public bool Add(TKey key, out int keyIndex)
        {
            if (!keyIndexMap.TryGetValue(key, out keyIndex))
            {
                keyIndex = keys.Length;
                keyIndexMap.Add(key, keyIndex);
                keys.Add(key);
                return true;
            }
            return false;
        }
        
        public bool ContainsKey(TKey key) => keyIndexMap.ContainsKey(key);

        public bool Remove(TKey key)
        {
            if (!keyIndexMap.TryGetValue(key, out var removeKeyIndex))
                return false;
            keyIndexMap.Remove(key);

            // Handle case: Only element
            if (keys.Length <= 1)
            {
                keys.Clear();
                return true;
            }
            
            // Otherwise remove swapback for speed
            // Get last values
            var lastIndex = keys.Length - 1;
            var lastKey = keys[lastIndex];
            // Swapback
            keys.RemoveAtSwapBack(removeKeyIndex);
            // Update index mapping, but only if the last key is not the removed key, setting this would re-add it to the map (was a bug, now fixed)
            if (removeKeyIndex != lastIndex)
                keyIndexMap[lastKey] = removeKeyIndex;

            return true;
        }

        public void Clear()
        {
            keys.Clear();
            keyIndexMap.Clear();
        }
    }
}