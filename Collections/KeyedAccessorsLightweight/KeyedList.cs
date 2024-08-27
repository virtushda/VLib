using System;
using System.Collections.Generic;
using Unity.Collections;

namespace Libraries.KeyedAccessors.Lightweight
{
    ///<summary> Generates a compact list of keys, where keys are also indexed.  <br/>
    /// Adds, removes, iterations, and contains checks are all very fast. Removes are the least fast, but still use a quick swapback pattern. <br/>
    /// Copied from <see cref="UnsafeKeyedList{TKey}"/></summary>
    public class KeyedList<TKey>
        where TKey : IEquatable<TKey>
    {
        // Key and value lists are aligned
        public List<TKey> keys;
        public Dictionary<TKey, int> keyIndexMap;

        public bool IsCreated => keys != null;
        public int Length => keys.Count;

        public KeyedList(int initCapacity = 16)
        {
            keys = new List<TKey>(initCapacity);
            keyIndexMap = new Dictionary<TKey, int>(initCapacity);
        }

        public bool Add(TKey key, out int keyIndex)
        {
            if (!keyIndexMap.TryGetValue(key, out keyIndex))
            {
                keyIndex = keys.Count;
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
            if (keys.Count <= 1)
            {
                keys.Clear();
                return true;
            }
            
            // Otherwise remove swapback for speed
            // Get last values
            var lastIndex = keys.Count - 1;
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