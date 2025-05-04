using System.Collections.Generic;
using Unity.Collections;

namespace Libraries.KeyedAccessors.Lightweight
{
    ///<summary> A managed version of <see cref="UnsafeKeyedMap{TKey,TValue}"/> </summary>
    public class KeyedListPair<TKey, TValue>
    {
        // Key and value lists are aligned
        public List<TKey> keys;
        public List<TValue> values;
        public Dictionary<TKey, int> keyIndexMap;

        public int Length => keys.Count;

        public KeyedListPair() : this(16){}

        public KeyedListPair(int initCapacity = 16)
        {
            keys = new List<TKey>(initCapacity);
            values = new List<TValue>(initCapacity);
            keyIndexMap = new Dictionary<TKey, int>(initCapacity);
        }

        public bool Add(TKey key, TValue value, out int keyIndex)
        {
            if (!keyIndexMap.TryGetValue(key, out keyIndex))
            {
                keyIndex = keys.Count;
                keyIndexMap.Add(key, keyIndex);
                keys.Add(key);
                values.Add(value);
                return true;
            }
            return false;
        }

        public void AddUpdate(TKey key, TValue value)
        {
            if (!Add(key, value, out var keyIndex))
                values[keyIndex] = value;
        }
        
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (!keyIndexMap.TryGetValue(key, out var keyIndex))
            {
                value = default;
                return false;
            }
            value = values[keyIndex];
            return true;
        }
        
        public bool Remove(TKey key, out TValue removedValue)
        {
            if (!keyIndexMap.Remove(key, out var removeKeyIndex))
            {
                removedValue = default;
                return false;
            }

            // Handle case: Only element
            if (keys.Count <= 1)
            {
                removedValue = values[0];
                keys.Clear();
                values.Clear();
                return true;
            }
            
            removedValue = values[removeKeyIndex];
            // Otherwise remove swapback for speed
            // Get last values
            var lastIndex = keys.Count - 1;
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