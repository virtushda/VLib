/*
using Drawing;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using VLib;

namespace PrehistoricKingdom.AnimalsV2.Utility
{
    [ExecuteAlways]
    public class CapsulePointSignedDistanceTester : MonoBehaviour
    {
        [SerializeField] CapsuleNative capsule = new(new float4(0f, 0f, 0f, 0.5f), new float4(0f, 0f, 2f, 0.5f));
        [SerializeField] float3 testPoint = new(1f, 0f, 1f);
        [SerializeField] bool drawCapsule = true;
        [SerializeField] bool drawTestPoint = true;
        [SerializeField] Color capsuleColor = Color.cyan;
        [SerializeField] Color testPointInsideColor = Color.green;
        [SerializeField] Color testPointOutsideColor = Color.red;

        void Update()
        {
            var capsuleTransformed = capsule;
            capsuleTransformed.TransformLengthScaling(transform.localToWorldMatrix);

            if (drawCapsule)
            {
                Draw.ingame.PushColor(capsuleColor);
                capsuleTransformed.DrawAline(ref Draw.ingame);
                Draw.ingame.PopColor();
            }

            if (drawTestPoint)
            {
                var testPointTransformed = transform.TransformPoint(testPoint);
                var signedDistance = capsuleTransformed.PointSignedDistance(testPointTransformed);
                var pointColor = signedDistance < 0 ? testPointInsideColor : testPointOutsideColor;

                Draw.ingame.WireSphere(testPointTransformed, 0.1f, pointColor);
            }
        }

        [Button]
        void LogPointSignedDistance()
        {
            var capsuleTransformed = capsule;
            capsuleTransformed.TransformLengthScaling(transform.localToWorldMatrix);
            var testPointTransformed = transform.TransformPoint(testPoint);

            var signedDistance = capsuleTransformed.PointSignedDistance(testPointTransformed);

            Debug.Log($"Point: {testPointTransformed}, SignedDistance: {signedDistance:F4}, ContainsPoint: {capsuleTransformed.ContainsPoint(testPointTransformed)}");
        }

        [Button]
        void LogMultiplePoints()
        {
            var capsuleTransformed = capsule;
            capsuleTransformed.TransformLengthScaling(transform.localToWorldMatrix);

            Debug.Log("=== Testing Multiple Points ===");

            var testPoints = new float3[]
            {
                capsuleTransformed.pointA.xyz,
                capsuleTransformed.pointB.xyz,
                capsuleTransformed.Center,
                capsuleTransformed.pointA.xyz + new float3(capsuleTransformed.pointA.w, 0f, 0f),
                capsuleTransformed.pointA.xyz + new float3(capsuleTransformed.pointA.w * 2f, 0f, 0f),
            };

            foreach (var point in testPoints)
            {
                var signedDistance = capsuleTransformed.PointSignedDistance(point);
                Debug.Log($"Point: {point}, SignedDistance: {signedDistance:F4}, ContainsPoint: {capsuleTransformed.ContainsPoint(point)}");
            }
        }

        [Button]
        void LogContainsPoint()
        {
            var capsuleTransformed = capsule;
            capsuleTransformed.TransformLengthScaling(transform.localToWorldMatrix);
            var testPointTransformed = transform.TransformPoint(testPoint);

            var contains = capsuleTransformed.ContainsPoint(testPointTransformed);
            Debug.Log($"Point {testPointTransformed} is {(contains ? "INSIDE" : "OUTSIDE")} the capsule");
        }
    }
}
*/