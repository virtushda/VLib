using System.Collections.Generic;

namespace VLib
{
    /// <summary> A dictionary that also keeps a list of keys in the order they were added. Useful for iterating or working directly with the keys. No removal support though (would be slow) </summary> 
    public class KeyListDictionary<TKey, TValue>
    {
        private object locker = new();
        
        private List<TKey> keyList = new();
        private Dictionary<TKey, TValue> dictionary = new();

        public List<TKey> KeyList => keyList;
        public Dictionary<TKey, TValue> Dictionary => dictionary;
        public int Count => keyList.Count;

        public TValue this[TKey key]
        {
            get => dictionary[key];
            set => dictionary[key] = value;
        }

        public bool Add(TKey key, TValue value)
        {
            bool added = dictionary.TryAdd(key, value);
            if (added)
                keyList.Add(key);
            return added;
        }

        public void AddSet(TKey key, TValue value)
        {
            if (!Add(key, value)) 
                dictionary[key] = value;
        }

        public bool ConcurrentRead(TKey key, out TValue value)
        {
            // TODO: This could be optimized if needed
            lock (locker)
            {
                return dictionary.TryGetValue(key, out value);
            }
        }

        public void ConcurrentAddSet(TKey key, TValue value)
        {
            lock (locker)
            {
                AddSet(key, value);
            }
        }

        public void Clear()
        {
            keyList.Clear();
            dictionary.Clear();
        }
    }
}