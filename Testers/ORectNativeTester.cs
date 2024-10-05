/*using System;
using Unity.Mathematics;
using UnityEngine;

namespace VLib.Libraries.VLib.Tests
{
    public class ORectNativeTester : MonoBehaviour
    {
        public float size = 20;

        float3 pointBL;
        float3 pointBR;
        float3 pointTL;
        float3 pointTR;

        ORectNative orect;
        
        void Update()
        {
            float3 extentForward = transform.forward * size * .5f;
            float3 extentRight = transform.right * size * .5f;

            float3 transformPosition = transform.position;
            pointBL = transformPosition - extentForward - extentRight;
            pointBR = transformPosition - extentForward + extentRight;
            pointTL = transformPosition + extentForward - extentRight;
            pointTR = transformPosition + extentForward + extentRight;

            orect = new ORectNative(pointBL.xz, pointBR.xz, pointTL.xz, pointTR.xz);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(pointBL, 1);
            Gizmos.DrawSphere(pointBR, 1);
            Gizmos.DrawSphere(pointTL, 1);
            Gizmos.DrawSphere(pointTR, 1);

#if UNITY_EDITOR
            orect.InternalAlignedRect.EditorOnlyDebugDraw(Color.green, Axis.X, Axis.Z, Axis.Y, transform.position.y, .05f);
            
            orect.EditorOnlyDebugDraw(Color.blue, Axis.X, Axis.Z, Axis.Y, transform.position.y, .05f);
#endif
        }
    }
}*/