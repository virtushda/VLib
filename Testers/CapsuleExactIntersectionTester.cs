/* using Drawing;
using Unity.Mathematics;
using UnityEngine;
using VLib;

namespace VLib.Testers
{
    [ExecuteInEditMode]
    public class CapsuleExactIntersectionTester : MonoBehaviour
    {
        [SerializeField] Transform capsuleAStart;
        [SerializeField] Transform capsuleAEnd;
        [SerializeField] Transform capsuleBStart;
        [SerializeField] Transform capsuleBEnd;

        [SerializeField, Min(0f)] float capsuleAStartRadius = 0.5f;
        [SerializeField, Min(0f)] float capsuleAEndRadius = 0.5f;
        [SerializeField, Min(0f)] float capsuleBStartRadius = 0.5f;
        [SerializeField, Min(0f)] float capsuleBEndRadius = 0.5f;

        bool exactHit;
        bool approximateHit;

        void OnEnable()
        {
            capsuleAStart ??= CreateHandle("Capsule A Start", new float3(-1.5f, 0f, 0f));
            capsuleAEnd ??= CreateHandle("Capsule A End", new float3(1.5f, 0f, 0f));
            capsuleBStart ??= CreateHandle("Capsule B Start", new float3(0f, -1f, 0.8f));
            capsuleBEnd ??= CreateHandle("Capsule B End", new float3(0f, 1f, -0.8f));
        }

        void Update()
        {
            if (!capsuleAStart || !capsuleAEnd || !capsuleBStart || !capsuleBEnd)
                return;

            var capsuleA = new CapsuleNative(
                new float4(capsuleAStart.position, capsuleAStartRadius),
                new float4(capsuleAEnd.position, capsuleAEndRadius));

            var capsuleB = new CapsuleNative(
                new float4(capsuleBStart.position, capsuleBStartRadius),
                new float4(capsuleBEnd.position, capsuleBEndRadius));

            exactHit = capsuleA.IntersectsCapsuleExact(capsuleB);
            approximateHit = capsuleA.ApproximatelyIntersects(capsuleB);

            var draw = DrawingManager.GetBuilder();

            draw.PushColor(Color.cyan);
            capsuleA.DrawAline(ref draw);
            draw.PopColor();

            draw.PushColor(Color.yellow);
            capsuleB.DrawAline(ref draw);
            draw.PopColor();

            var resultColor = exactHit ? Color.green : Color.red;
            var labelPos = (capsuleA.Center + capsuleB.Center) * 0.5f + math.up() * 0.5f;
            draw.Label2D(labelPos, exactHit ? "Exact: Hit" : "Exact: Miss", 14f, LabelAlignment.Center, resultColor);

            if (approximateHit != exactHit)
            {
                draw.Label2D(labelPos + math.up() * 0.25f, approximateHit ? "Approx: Hit" : "Approx: Miss", 12f, LabelAlignment.Center, Color.white);
            }

            draw.Dispose();
        }

        Transform CreateHandle(string name, float3 localPosition)
        {
            var handle = new GameObject(name).transform;
            handle.SetParent(transform, false);
            handle.localPosition = localPosition;
            handle.localRotation = Quaternion.identity;
            return handle;
        }
    }
} */