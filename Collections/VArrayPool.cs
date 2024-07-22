using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;

namespace VLib
{
    public class VArrayPool
    {
        protected static List<IArrayPool> globalPools = new();

        public static void DisposeAllArrayPools()
        {
            for (var i = globalPools.Count - 1; i >= 0; i--)
            {
                var globalPool = globalPools[i];
                globalPool?.Dispose(false);
                globalPools.RemoveAt(i);
            }
        }
    }

    public interface IArrayPool
    {
        void ReleaseAllPooled();

        void Dispose(bool autoGlobalRemoval);
    }
    
    public class VArrayPool<T> : VArrayPool, IArrayPool
    {
        static VArrayPool<T> sharedInstance;
        public static VArrayPool<T> Shared => sharedInstance ??= new VArrayPool<T>();

        readonly object arraysLock = new();
        List<T[]> arrays;
        volatile int rentCounter;

        public VArrayPool()
        {
            lock (arraysLock)
            {
                globalPools.Add(this);
                arrays = new List<T[]>();
            }
        }

        public void Dispose(bool autoGlobalRemoval)
        {
            ReleaseAllPooled();
            lock (arraysLock)
            {
                if (autoGlobalRemoval)
                    globalPools.Remove(this);
                sharedInstance = null;
            }
        }

        public T[] Rent(int length)
        {
            Interlocked.Increment(ref rentCounter);
            if (TryFindArrayOfAtLeastLength(length, out var array))
                return array;
            return new T[length];
        }

        public void Return(T[] array)
        {
            lock (arraysLock)
            {
                if (!arrays.Contains(array))
                {
                    arrays.Add(array);
                    Interlocked.Decrement(ref rentCounter);
                }
            }
        }

        public void ReleaseAllPooled()
        {
            if (rentCounter != 0)
                Debug.LogError($"Releasing all pooled arrays, but rent count is still: {rentCounter}");
            lock (arraysLock)
            {
                rentCounter = 0;
                arrays.Clear();
            }
        }

        bool TryFindArrayOfAtLeastLength(int length, out T[] array)
        {
            Profiler.BeginSample("TryFindArrayOfAtLeastLength");
            lock (arraysLock)
            {
                for (int i = 0; i < arrays.Count; i++)
                {
                    var arrayAtIndex = arrays[i];
                    if (arrayAtIndex.Length >= length)
                    {
                        array = arrayAtIndex;
                        arrays.RemoveAtSwapBack(i);
                        return true;
                    }
                }
            }
            Profiler.EndSample();

            array = null;
            return false;
        }
    }
}