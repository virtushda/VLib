using System;
using UnityEngine;

namespace VLib
{
    /// <summary>Implementation for IConcurrentDictionaryPoolContainer</summary>
    public static class IConcurrentTypedPoolExtensions
    {
        public static bool TryGetExistingPool<TContainer, TPool, TPoolable>(this TContainer dictionaryPool, out TPool fetchedPool)
            where TContainer : IConcurrentTypedPoolContainer
            where TPool : IPool<TPoolable>
        {
            if (dictionaryPool.Map.TryGetValue(typeof(TPoolable), out var poolObject))
            {
                fetchedPool = (TPool) poolObject;
                return true;
            }
            fetchedPool = default;
            return false;
        }
        
        public static TPool GetOrCreatePool<TContainer, TPool, TPoolable>(this TContainer dictionaryPool)
            where TContainer : IConcurrentTypedPoolContainer
            where TPool : IPool<TPoolable>, new()
        {
            if (dictionaryPool.Map.TryGetValue(typeof(TPoolable), out var poolObject))
                return (TPool) poolObject;
            var fetchedPool = new TPool();
            dictionaryPool.Map.TryAdd(typeof(TPoolable), fetchedPool);
            return fetchedPool;
        }
        
        public static void GetOrCreatePoolAuto<TContainer, TPool, TPoolable>(this TContainer dictionaryPool, out TPool fetchedPool, out TPoolable autoHelper) 
            where TContainer : IConcurrentTypedPoolContainer
            where TPool : IPool<TPoolable>, new()
            where TPoolable : new()
        {
            autoHelper = default;
            fetchedPool = GetOrCreatePool<TContainer, TPool, TPoolable>(dictionaryPool);
        }
        
        public static bool TryRemovePool<TContainer>(this TContainer dictionaryPool, Type key)
            where TContainer : IConcurrentTypedPoolContainer
        {
            if (!dictionaryPool.Map.TryGetValue(key, out var pool))
                return false;
            pool.ClearAll();
            if (!dictionaryPool.Map.TryRemove(key, out _))
            {
                Debug.LogError($"Failed to remove pool of type {key} from pools map");
                return false;
            }
            return true;
        }
        
        public static void ClearAllPools<TContainer>(this TContainer dictionaryPool)
            where TContainer : IConcurrentTypedPoolContainer
        {
            if (dictionaryPool != null && dictionaryPool.Map != null)
            {
                foreach (var pool in dictionaryPool.Map.Values)
                    pool?.ClearAll();
                dictionaryPool.Map.Clear();
            }
        }
    }
}