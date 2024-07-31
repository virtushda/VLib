#if UNITY_EDITOR
#define DRAWDEBUG
#endif

using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace VLib
{
    public static class CameraUtils
    {
        /// <summary> In build or play mode: Camera.main <br/>
        /// In edit mode: SceneView camera </summary>
        public static Camera GetCurrent()
        {
            Camera camera = null;
#if UNITY_EDITOR
            if (Application.isPlaying)
                camera = Camera.main;
            else
                camera = SceneView.lastActiveSceneView.camera;
#else
            camera = Camera.main;
#endif
            if (!camera)
                Debug.LogError("Could not find current camera.");
            return camera;
        }
        
        public static Matrix4x4 CalculateObliqueMatrixFromWorldPlane(Camera cam, Vector4 clipPlaneWorld)
        {
            Vector4 obliqueClipPlaneCamSpace = Matrix4x4.Transpose(Matrix4x4.Inverse(cam.worldToCameraMatrix)) * clipPlaneWorld;
            return cam.CalculateObliqueMatrix(obliqueClipPlaneCamSpace);
        }

        public static bool RecalcLayerCullDistances(ref float[] layerCullDists, float drawDistance)
        {
            bool dirtied = false;

            if (layerCullDists == null)
            {
                layerCullDists = new float[32];
                dirtied = true;
            }

            for (int i = 0; i < layerCullDists.Length; i++)
            {
                if (layerCullDists[i] != drawDistance)
                {
                    layerCullDists[i] = drawDistance;
                    dirtied = true;
                }
            }

            return dirtied;
        }

        public static void SetUpdateCameraLayerCulling(Camera cam, float[] layerCullDistances)
        {
            cam.layerCullSpherical = true;
            cam.layerCullDistances = layerCullDistances;
        }

        public static Matrix4x4 GetScissorCullingRect(Matrix4x4 inputProjectionMatrix, Rect r, Matrix4x4 worldToCamMatrix)
        {
            if (r.x < 0)
            {
                r.width += r.x;
                r.x = 0;
            }

            if (r.y < 0)
            {
                r.height += r.y;
                r.y = 0;
            }

            r.width = Mathf.Min(1 - r.x, r.width);
            r.height = Mathf.Min(1 - r.y, r.height);

            Matrix4x4 m = inputProjectionMatrix;
            //Matrix4x4 m1 = Matrix4x4.TRS(new Vector3(r.x, r.y, 0), Quaternion.identity, new Vector3(r.width, r.height, 1));
            Matrix4x4 m2 = Matrix4x4.TRS(new Vector3((1 / r.width - 1), (1 / r.height - 1), 0), Quaternion.identity, new Vector3(1 / r.width, 1 / r.height, 1));
            Matrix4x4 m3 = Matrix4x4.TRS(new Vector3(-r.x * 2 / r.width, -r.y * 2 / r.height, 0), Quaternion.identity, Vector3.one);

            //Technically this commented out line works... but not with render textures, and other fun problems, just cull instead!!!
            //cam.projectionMatrix = m3 * m2 * m; 
            return m3 * m2 * m * worldToCamMatrix;
        }

        [GenerateTestsForBurstCompatibility]
        public static float3 WorldToViewportPointNative(float3 pointWorld, float4x4 worldToCamMatrix, float4x4 projectionMatrix)
        {
            //https://forum.unity.com/threads/camera-worldtoviewportpoint-math.644383/
            float4 worldPoint4 = new float4(pointWorld, 1);
            float4 viewPoint4 = math.mul(worldToCamMatrix, worldPoint4);
            float4 projPoint4 = math.mul(projectionMatrix, viewPoint4);
            float3 normProjPoint3 = projPoint4.xyz / projPoint4.w;
            return new float3(normProjPoint3.xy * .5f + .5f, -viewPoint4.z);
        }
        
        [GenerateTestsForBurstCompatibility]
        public static float3 ViewportToWorldPointNative(float2 viewportPoint2, float4x4 camToWorldMatrix, float4x4 inverseProjectionMatrix)
        {
            Matrix4x4 projUnity = inverseProjectionMatrix;
            //float4x4 P = Camera.main.projection$$anonymous$$atrix;
            //float4x4 V = Camera.main.transform.worldToLocal$$anonymous$$atrix;
            //float4x4 VP = P * V;
 
            // get projection W by Z
            float4 projW = projUnity * new Vector4(0, 0, 1, 1);
            
            // restore point4
            float3 point3 = new float3(viewportPoint2.xy * 2 - 1, projW.z/projW.w);

            float3 pointInvProj = math.transform(inverseProjectionMatrix, point3);
            float3 worldPoint = math.transform(camToWorldMatrix, pointInvProj);

            return worldPoint;

            //float4 result4 = VP.inverse * point4;  // multiply 4 components

            //float4 resultInv = result4 / result4.w;  // store 3 components of the resulting 4 components
            //return resultInv.xyz;
        }

        public static (float3 bl, float3 tl, float3 tr, float3 br) GetFrustumCornersAtDistance(this Camera c, float dist)
        {
            var frustumHeight = 2.0f * dist * Mathf.Tan(c.fieldOfView * 0.5f * Mathf.Deg2Rad);
            var frustumWidth = frustumHeight * c.aspect;

            var cT = c.transform;
            float3 camPos = cT.position;
            
            float3 upOffset = cT.up;
            float3 upOffsetDouble = upOffset * frustumHeight;
            upOffset = upOffsetDouble * .5f;
            
            float3 rightOffset = cT.right;
            rightOffset *= frustumWidth * .5f;
            
            float3 forward = cT.forward;
            var centerPoint = camPos + forward * dist;

            float3 bottom = centerPoint - upOffset;
            float3 bottomLeft = bottom - rightOffset;
            float3 bottomRight = bottom + rightOffset;
            float3 topLeft = bottomLeft + upOffsetDouble;
            float3 topRight = bottomRight + upOffsetDouble;
            
            #if DRAWDEBUG
            Debug.DrawLine(bottomLeft, topLeft, Color.red);
            Debug.DrawLine(bottomRight, topRight, Color.green);
            Debug.DrawLine(topLeft, topRight, Color.yellow);
            Debug.DrawLine(bottomLeft, bottomRight, Color.blue);

            Debug.DrawLine(camPos, topLeft, Color.red);
            Debug.DrawLine(camPos, bottomLeft, Color.red);
            Debug.DrawLine(camPos, topRight, Color.green);
            Debug.DrawLine(camPos, bottomRight, Color.green);
            #endif

            return (bottomLeft, topLeft, topRight, bottomRight);
        }

        public static (PlaneNative b, PlaneNative l, PlaneNative t, PlaneNative r) CornersToPlanes
            (float3 camPos, (float3 bl, float3 tl, float3 tr, float3 br) corners)
        {
            var b = new PlaneNative(camPos, corners.br, corners.bl);
            var l = new PlaneNative(camPos, corners.bl, corners.tl);
            var t = new PlaneNative(camPos, corners.tl, corners.tr);
            var r = new PlaneNative(camPos, corners.tr, corners.br);
            return (b, l, t, r);
        }

        public static (PlaneNative b, PlaneNative l, PlaneNative t, PlaneNative r) GetSideFrustumPlanes(this Camera cam, float frustumCornerSampleDist = 100)
        {
            var frustumCorners = cam.GetFrustumCornersAtDistance(frustumCornerSampleDist);
            return CornersToPlanes(cam.transform.position, frustumCorners);
        }

        static FieldInfo canvasHackFieldCached;
        static FieldInfo CanvasHackFieldCachedAuto()
        {
            if (canvasHackFieldCached == null)
                    canvasHackFieldCached = typeof(Canvas).GetField("willRenderCanvases", BindingFlags.NonPublic | BindingFlags.Static);
            return canvasHackFieldCached;
        }
        
        public static void RenderWithoutUnitySlowCanvasShit(this Camera cam)
        {
            var canvasHackField = CanvasHackFieldCachedAuto();
            var canvasHackObject = canvasHackField.GetValue(null);
            canvasHackField.SetValue(null, null);
            cam.Render();
            canvasHackField.SetValue(null, canvasHackObject );

        }

        [GenerateTestsForBurstCompatibility]
        public static RectNative ProjectSphereToViewportRect(float4 sphere, float4x4 worldToCamMatrix, float4x4 projectionMatrix, float3 camRightVec)
        {
            var centerViewPoint = WorldToViewportPointNative(sphere.xyz, worldToCamMatrix, projectionMatrix);
            var sideViewPoint = WorldToViewportPointNative(sphere.xyz + camRightVec * sphere.w, worldToCamMatrix, projectionMatrix);
            return new RectNative(centerViewPoint.xy, (sideViewPoint.x - centerViewPoint.x) * 2);
        }
    }
}