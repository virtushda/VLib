using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using VLib.Unsafe.Utility;

namespace Libraries.KeyedAccessors.Lightweight
{
    ///<summary> Generates a compact list of values, where values can be added or removed based on a given key. <br/>
    /// Has key->value mapping behaviour as well. <br/>
    /// Adds, removes, iterations, and contains checks are all very fast. Removes are the least fast, but still use a quick swapback pattern. <br/>
    /// Not copy-safe! </summary>
    public struct UnsafeKeyedMap<TKey, TValue> : IDisposable
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        // Key and value lists are aligned
        public UnsafeList<TKey> keys;
        public UnsafeList<TValue> values;
        public UnsafeHashMap<TKey, int> keyIndexMap;

        public readonly bool IsCreated => keys.IsCreated;
        public readonly int Length => keys.Length;

        public UnsafeKeyedMap(Allocator allocator, int initCapacity = 16, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
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
        /// Don't hold onto the ref for long! (You are responsible for thread safety)</summary>
        public ref TValue TryGetValueRef(TKey key, out bool success)
        {
            if (!keyIndexMap.TryGetValue(key, out var keyIndex))
            {
                success = false;
                return ref VUnsafeUtil.NullRef<TValue>();
            }
            success = true;
            return ref values.ElementAt(keyIndex);
        }
        
        public ref TValue ElementAt(int index) => ref values.ElementAt(index);

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

        public ReadOnly AsReadOnly() => new ReadOnly(this);
        
        public struct ReadOnly
        {
            UnsafeKeyedMap<TKey, TValue> map;
            
            public readonly bool IsCreated => map.IsCreated;
            public readonly int Length => map.Length;
            
            public ReadOnly(UnsafeKeyedMap<TKey, TValue> map) => this.map = map;
            
            public TKey ReadKey(int index) => map.keys[index];
            
            public TValue ReadValue(int index) => map.values[index];
            
            /// <summary> This technically allows edits, but can also be more efficient that copying if the value type is large. Show some honor and use it right. </summary>
            public ref TKey KeyElementAt(int index) => ref map.keys.ElementAt(index);
            
            /// <summary> This technically allows edits, but can also be more efficient that copying if the value type is large. Show some honor and use it right. </summary>
            public ref TValue ValueElementAt(int index) => ref map.values.ElementAt(index);
            
            public bool ContainsKey(TKey key) => map.ContainsKey(key);
            
            public bool TryGetValueCopy(TKey key, out TValue value) => map.TryGetValueCopy(key, out value);
            
            public bool TryGetIndex(TKey key, out int keyIndex) => map.TryGetIndex(key, out keyIndex);
            
            /// <summary> This technically allows edits, but can also be more efficient that copying if the value type is large. Show some honor and use it right. </summary>
            public ref TValue GetValueRef(TKey key) => ref map.GetValueRef(key);
            
            /// <summary> This technically allows edits, but can also be more efficient that copying if the value type is large. Show some honor and use it right. </summary>
            public ref TValue TryGetValueRef(TKey key, out bool success) => ref map.TryGetValueRef(key, out success);
        }
    }
}