using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace VLib.Collections
{
    public class IndirectRenderCallCache
    {
        public IndirectRenderCall[] renderCalls;
        public int callCount;
        public int capacity;

        public IndirectRenderCallCache(int initCapacity)
        {
            this.capacity = initCapacity;
            renderCalls = new IndirectRenderCall[initCapacity];
        }

        public void AddRenderCall(Mesh mesh, int submeshIndex, Material material, Bounds instancingBounds, ComputeBuffer argsBuffer, int offset, 
            MaterialPropertyBlock mpb, ShadowCastingMode castShadows, bool receiveShadows, int layer, Camera camera, LightProbeUsage lightProbeUsage)
        {
            if (callCount >= capacity)
            {
                capacity *= 2;
                Array.Resize(ref renderCalls, capacity);

#if UNITY_EDITOR
            if (capacity > 32)
                Debug.LogError($"Large RenderCallCache Resize {capacity / 2} -> {capacity}!");
#endif
            }

            renderCalls[callCount] = new IndirectRenderCall(mesh,
                submeshIndex,
                material,
                instancingBounds,
                argsBuffer,
                offset,
                mpb,
                castShadows,
                receiveShadows,
                layer,
                camera,
                lightProbeUsage);
            callCount++;
        }

        public void ClearCalls() => callCount = 0;

        public void Render()
        {
            for (int i = 0; i < callCount; i++)
                renderCalls[i].Render();
        }
    }
}