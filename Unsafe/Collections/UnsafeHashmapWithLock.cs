using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace VLib
{
    /// <summary> <para> Be careful not to operate on copies of this! </para>
    /// <para>A hashmap and a spinlock duct-taped together.</para></summary>
    public struct UnsafeHashmapWithLock<TKey, TValue> : IDisposable
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        public UnsafeParallelHashMap<TKey, TValue> map;
        public BurstSpinLockReadWrite spinLock;

        public UnsafeHashmapWithLock(GlobalBurstTimer burstTimer, int capacity, Allocator allocator = Allocator.Persistent)
        {
            map = new UnsafeParallelHashMap<TKey, TValue>(capacity, allocator);
            spinLock = new BurstSpinLockReadWrite(allocator, burstTimer);
        }

        public void Dispose()
        {
            if (spinLock.LockedAny)
                Debug.LogError("UnsafeHashmapWithLock was disposed while still locked!");
            
            map.DisposeRefToDefault();
            spinLock.DisposeRefToDefault();
        }
    }
}