using System.Buffers;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace VLib.Collections
{
    /// <summary> Provides generic shared object lists, these lists can be typed into <see cref="PooledObjectList{T}"/> in order to reuse all internal arrays between all types. </summary>
    public static class PooledObjectListPool
    {
        static long nextWrapperID;
        static ConcurrentQueue<PooledObjectListObj> listHolderPool;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Initialize()
        {
            nextWrapperID = 0;
            listHolderPool = new();
        }

        public static PooledObjectList<T> Rent<T>(int minimumLength)
            where T : class
        {
#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                Debug.LogError("SharedObjectListPool can only be used in play mode!");
                return default;
            }
#endif
            
            var id = Interlocked.Increment(ref nextWrapperID);
            
#if UNITY_EDITOR
            if (id >= long.MaxValue)
            {
                Debug.LogError("SharedObjectListPool ID overflow!");
                return default;
            }
#endif
            
            var rawArray = ArrayPool<object>.Shared.Rent(minimumLength);
            var listHolder = listHolderPool.TryDequeue(out var holder) ? holder : new PooledObjectListObj();
            listHolder.Initialize(id, rawArray);
            return new PooledObjectList<T>(listHolder, id);
        }
        
        public static bool Return<T>(PooledObjectList<T> list)
            where T : class
        {
            if (!list.IsValid)
                return false;
            // Dispose internal list holder
            if (!list.pooledList.TryDispose())
                return false;
            // Recycle it
            listHolderPool.Enqueue(list.pooledList);
            return true;
        }
    }
}