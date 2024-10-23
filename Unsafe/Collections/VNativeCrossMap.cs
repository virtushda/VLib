using System;
using Unity.Collections;

namespace VLib.Libraries.VLib.Unsafe.Collections
{
    /// <summary> Two hashmaps that point at each other, enforcing uniqueness and consistent two-way mapping. </summary>
    [GenerateTestsForBurstCompatibility]
    public struct VNativeCrossMap<T1, T2> : IDisposable, IAllocating
        where T1 : unmanaged, IEquatable<T1>
        where T2 : unmanaged, IEquatable<T2>
    {
        NativeHashMap<T1, T2> map;
        NativeHashMap<T2, T1> reverseMap;
        
        public readonly bool IsCreated => map.IsCreated && reverseMap.IsCreated;
        
        public NativeHashMap<T1, T2>.ReadOnly MapReadOnly => map.AsReadOnly();
        public NativeHashMap<T2, T1>.ReadOnly ReverseMapReadOnly => reverseMap.AsReadOnly();
        
        public VNativeCrossMap(int capacity, Allocator allocator)
        {
            map = new NativeHashMap<T1, T2>(capacity, allocator);
            reverseMap = new NativeHashMap<T2, T1>(capacity, allocator);
        }
        
        public void Dispose()
        {
            map.DisposeRefToDefault();
            reverseMap.DisposeRefToDefault();
        }

        public readonly int Count() => map.Count;

        public bool Add(T1 t1, T2 t2)
        {
            if (!map.TryAdd(t1, t2))
                return false;
            reverseMap.Add(t2, t1);
            return true;
        }
        
        public bool Remove(T1 t1)
        {
            if (!map.TryGetValue(t1, out var t2))
                return false;
            map.Remove(t1);
            reverseMap.Remove(t2);
            return true;
        }
        
        public bool RemoveReverse(T2 t2)
        {
            if (!reverseMap.TryGetValue(t2, out var t1))
                return false;
            reverseMap.Remove(t2);
            map.Remove(t1);
            return true;
        }
        
        public bool TryGetKey(T2 t2, out T1 t1) => reverseMap.TryGetValue(t2, out t1);
        
        public bool TryGetValue(T1 t1, out T2 t2) => map.TryGetValue(t1, out t2);
    }
}