using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace VLib
{
    public class VArrayPool
    {
        protected static readonly object globalPoolsLock = new();
        protected static List<IArrayPool> globalPools = new();

        public static void DisposeAllArrayPools()
        {
            IArrayPool[] globalPoolsSnapshot;
            lock (globalPoolsLock)
            {
                globalPoolsSnapshot = globalPools.ToArray();
                globalPools.Clear();
            }

            for (var i = globalPoolsSnapshot.Length - 1; i >= 0; i--)
            {
                var globalPool = globalPoolsSnapshot[i];
                globalPool?.Dispose(false);
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
        const int MaxPowerOfTwoBucketSize = 8192;
        const int LinearBucketStep = 4000;
        const int MaxPooledArrayLength = 128000;

        static readonly object sharedInstanceLock = new();
        static VArrayPool<T> sharedInstance;
        public static VArrayPool<T> Shared
        {
            get
            {
                var instance = Volatile.Read(ref sharedInstance);
                if (instance != null && !instance.IsDisposed)
                    return instance;

                lock (sharedInstanceLock)
                {
                    instance = sharedInstance;
                    if (instance == null || instance.IsDisposed)
                        sharedInstance = instance = new VArrayPool<T>();
                    return instance;
                }
            }
        }

        readonly object arraysLock = new();
        readonly Dictionary<int, Stack<T[]>> arraysByBucket = new();
        readonly HashSet<T[]> trackedArrays = new();
        readonly HashSet<T[]> pooledArrays = new();
        int rentCounter;
        int disposedState;

        bool IsDisposed => Volatile.Read(ref disposedState) != 0;

        VArrayPool()
        {
            lock (globalPoolsLock)
            {
                globalPools.Add(this);
            }
        }

        public void Dispose(bool autoGlobalRemoval)
        {
            Interlocked.Exchange(ref disposedState, 1);

            if (autoGlobalRemoval)
            {
                lock (globalPoolsLock)
                {
                    globalPools.Remove(this);
                }
            }

            lock (arraysLock)
            {
                rentCounter = 0;
                arraysByBucket.Clear();
                trackedArrays.Clear();
                pooledArrays.Clear();
            }

            lock (sharedInstanceLock)
            {
                if (ReferenceEquals(sharedInstance, this))
                    sharedInstance = null;
            }
        }

        public T[] Rent(int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            if (length == 0)
                return Array.Empty<T>();

            lock (arraysLock)
            {
                ThrowIfDisposed();
                if (!TryGetBucketSize(length, out var bucketSize))
                    return new T[length];

                if (arraysByBucket.TryGetValue(bucketSize, out var bucket) && bucket.Count > 0)
                {
                    var pooledArray = bucket.Pop();
                    pooledArrays.Remove(pooledArray);
                    rentCounter++;
                    return pooledArray;
                }

                var newArray = new T[bucketSize];
                trackedArrays.Add(newArray);
                rentCounter++;
                return newArray;
            }
        }

        public void Return(T[] array)
        {
            if (array == null)
                return;

            lock (arraysLock)
            {
                if (IsDisposed)
                    return;
                if (!trackedArrays.Contains(array))
                    return;
                if (!pooledArrays.Add(array))
                    return;

                if (!arraysByBucket.TryGetValue(array.Length, out var bucket))
                    arraysByBucket.Add(array.Length, bucket = new Stack<T[]>());
                bucket.Push(array);

                if (rentCounter > 0)
                    rentCounter--;
            }
        }

        public void ReleaseAllPooled()
        {
            lock (arraysLock)
            {
                if (IsDisposed)
                    return;
                if (rentCounter != 0)
                    Debug.LogError($"Releasing all pooled arrays, but rent count is still: {rentCounter}");

                foreach (var pooledArray in pooledArrays)
                    trackedArrays.Remove(pooledArray);

                arraysByBucket.Clear();
                pooledArrays.Clear();
            }
        }

        void ThrowIfDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException($"{nameof(VArrayPool<T>)}<{typeof(T).Name}>");
        }

        static bool TryGetBucketSize(int requestedLength, out int bucketSize)
        {
            if (requestedLength <= MaxPowerOfTwoBucketSize)
            {
                bucketSize = Mathf.NextPowerOfTwo(requestedLength);
                return true;
            }

            if (requestedLength <= MaxPooledArrayLength)
            {
                bucketSize = ((requestedLength + LinearBucketStep - 1) / LinearBucketStep) * LinearBucketStep;
                return true;
            }

            bucketSize = requestedLength;
            return false;
        }
    }
}
