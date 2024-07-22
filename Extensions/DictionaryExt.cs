using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace VLib
{
    public static class DictionaryExt
    {
        public static void AddOrSet<TKey, TValue, TValueInherited>(this IDictionary<TKey, TValue> keyValues, TKey key, TValueInherited value)
            where TValueInherited : TValue
        {
            if (!keyValues.TryAdd(key, value))
                keyValues[key] = value;
        }

        /// <summary>Gets or creates the value for the input key.</summary>
        public static TValue GetOrAddNew<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
            where TValue : new()
        {
            if (!dictionary.TryGetValue(key, out var fetchedValue))
            {
                fetchedValue = new TValue();
                dictionary.Add(key, fetchedValue);
            }
            return fetchedValue;
        }
        
        public static long MemoryFootprintBytes<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, bool trySizeElements = false)
        {
            long footprint = 0;
            int capacity = Mathf.NextPowerOfTwo(dictionary.Count);

            int ptrSize = Marshal.SizeOf<IntPtr>();
            bool keysManaged = false, valuesManaged = false;

            // Get size of dictionary object if native or managed...
            try
            {
                footprint += Marshal.SizeOf(typeof(TKey)) * capacity;
            }
            catch
            {
                footprint += ptrSize * capacity;
                keysManaged = true;
            }

            try
            {
                footprint += Marshal.SizeOf(typeof(TValue)) * capacity;
            }
            catch
            {
                footprint += ptrSize * capacity;
                valuesManaged = true;
            }

            // Elements
            if (trySizeElements && dictionary.Count > 0)
            {
                if (keysManaged)
                {
                    var keys = dictionary.Keys;
                    
                    foreach (var key in keys)
                    {
                        if (key is IMemoryReporter memoryReporter)
                            footprint += memoryReporter.ReportBytes();
                    }
                }
                if (valuesManaged)
                {
                    var values = dictionary.Values;
                    
                    foreach (var value in values)
                    {
                        if (value is IMemoryReporter memoryReporter)
                            footprint += memoryReporter.ReportBytes();
                    }
                }
            }
            
            return footprint;
        }
        
        public static bool HasNullKeys<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
        {
            foreach(var key in dictionary.Keys)
            {
                if(key == null)
                {
                    return true;
                }
            }
       
            return false;
        }
 
 
        public static void RemoveNullKeys<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
        {
            if(!dictionary.HasNullKeys())
            {
                return;
            }
 
            foreach(var key in dictionary.Keys.ToArray())
            {
                if(key == null)
                {
                    dictionary.Remove(key);
                }
            }
        }
    }
}