using UnityEngine;
using UnityEngine.Rendering;

namespace VLib.Collections
{
    public struct IndirectRenderCall
    {
        Mesh mesh;
        int submeshIndex;
        Material material;
        Bounds instancingBounds;
        ComputeBuffer argsBuffer;
        int offset;
        MaterialPropertyBlock mpb;
        ShadowCastingMode castShadows;
        bool receiveShadows;
        int layer;
        Camera camera;
        LightProbeUsage lightProbeUsage;

        public IndirectRenderCall(Mesh mesh, int submeshIndex, Material material, Bounds instancingBounds, ComputeBuffer argsBuffer,
            int offset, MaterialPropertyBlock mpb,
            ShadowCastingMode castShadows, bool receiveShadows, int layer, Camera camera,
            LightProbeUsage lightProbeUsage)
        {
            this.mesh = mesh;
            this.submeshIndex = submeshIndex;
            this.material = material;
            this.instancingBounds = instancingBounds;
            this.argsBuffer = argsBuffer;
            this.offset = offset;
            this.mpb = mpb;
            this.castShadows = castShadows;
            this.receiveShadows = receiveShadows;
            this.layer = layer;
            this.camera = camera;
            this.lightProbeUsage = lightProbeUsage;
        }

        public void Render(bool useCustomCam = false, Camera customCam = null)
        {
            Graphics.DrawMeshInstancedIndirect(mesh,
                submeshIndex,
                material,
                instancingBounds,
                argsBuffer,
                offset,
                mpb,
                castShadows,
                receiveShadows,
                layer,
                useCustomCam ? customCam : camera,
                lightProbeUsage);
        }
    }
}