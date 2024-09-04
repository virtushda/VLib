using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace VLib.SpatialAcceleration
{
    public struct SpatialHashGrid<T>
        where T : unmanaged, IEquatable<T>, ISpatialHashElement
    {
        VUnsafeRef<UnsafeSpatialHashGrid<T>> nativeMemory;
        public ref UnsafeSpatialHashGrid<T> NativeRef => ref nativeMemory.ValueRef;

        public int Count => NativeRef.Count;

        public SpatialHashGrid(float cellSize, int initCapacity = 1024)
        {
            nativeMemory = new VUnsafeRef<UnsafeSpatialHashGrid<T>>(Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            nativeMemory.Value = new UnsafeSpatialHashGrid<T>(cellSize, initCapacity);
        }

        public void Dispose()
        {
            nativeMemory.Value.Dispose();
            nativeMemory.Dispose();
            nativeMemory = default;
        }

        /// <summary> Returns true if value was added. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AddUpdate(in T value) => NativeRef.AddUpdate(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(in T value) => NativeRef.Contains(value);

        /// <summary> Returns true if value was removed. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(in T value) => NativeRef.Remove(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T ElementAt(int index) => ref NativeRef.ElementAt(index);
        
        public UnsafeSpatialHashGrid<T>.ElementRefIterator GetAllIndicesIntersecting(in RectNative worldRectXZ, ref UnsafeList<int> resultIndices)
        {
            NativeRef.GetAllIndicesIntersecting(worldRectXZ, ref resultIndices);
            return new UnsafeSpatialHashGrid<T>.ElementRefIterator(NativeRef, resultIndices);
        }
    }
}