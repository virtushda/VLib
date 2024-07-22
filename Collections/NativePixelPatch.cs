using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace VLib
{
    public struct NativePixelPatch<T> : INativeDisposable
        where T : unmanaged
    {
        NativeArray<T> array;
        RectNative pixelRect;
        bool isCreated;
        public bool IsCreated => isCreated;
        public NativeArray<T> ArrayCopy => array;
        public RectNative PixelRect { get => pixelRect; set => pixelRect = value; }

        public NativePixelPatch(ref NativeArray<T> array, ref RectNative pixelRect) : this()
        {
            SetArray(ref array);
            this.pixelRect = pixelRect;
        }

        public void SetArray(ref NativeArray<T> newArray)
        {
            if (newArray.IsCreated && newArray.Length > 0)
            {
                array = newArray;
                isCreated = true;
            }
            else
                Debug.LogError("You cannot assign an empty or uncreated nativearray to the Array property.");
        }

        public void InitEmptyArray(RectNative pixelRect, NativeArrayOptions initOption = NativeArrayOptions.ClearMemory)
        {
            Dispose();

            this.pixelRect = pixelRect;
            var newArray = new NativeArray<T>(pixelRect.CountInt, Allocator.Persistent, initOption);
            isCreated = true;
        }

        public void Dispose()
        {
            if (isCreated && array.IsCreated)
                array.Dispose();

            isCreated = false;
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            JobHandle handle = default;
            if (isCreated && array.IsCreated)
                handle = array.Dispose(inputDeps);

            isCreated = false;
            return handle;
        }
    }
}