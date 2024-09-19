using System.Runtime.CompilerServices;
using Drawing;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;
using Random = Unity.Mathematics.Random;

namespace VLib
{
    /// <summary> A capsule with variable radii at both endpoints. </summary>
    public struct CapsuleNative
    {
        public float4 pointA;
        public float4 pointB;

        /// <summary> float4 is Point-Radius </summary>
        public CapsuleNative(float4 pointA, float4 pointB)
        {
            this.pointA = pointA;
            this.pointB = pointB;
        }

        /// <summary> Generate a capsule from a world transform and some local points. </summary>
        public CapsuleNative(in AffineTransform transform, float3 localPointA, float3 localPointB, float radiusA, float radiusB)
        {
            this.pointA = new float4(math.transform(transform, localPointA), radiusA);
            this.pointB = new float4(math.transform(transform, localPointB), radiusB);
        }

        public float3 Center => (pointA.xyz + pointB.xyz) * .5f;
        public float3 AToB => pointB.xyz - pointA.xyz;

        public readonly float MaxRadius() => max(pointA.w, pointB.w) + distance(pointB.xyz, pointA.xyz) * 0.5f;

        public void Translate(float3 translation)
        {
            pointA.xyz += translation;
            pointB.xyz += translation;
        }

        public void SetWorldPositionRotation(in TranslationRotation capsuleTransform)
        {
            var aToB = AToB;
            pointA.xyz = capsuleTransform.position;
            
            // Compute direction change
            //var aToBNormalized = normalizesafe(aToB);
            var rotation = (quaternion)Quaternion.FromToRotation(aToB, capsuleTransform.Forward());
            var aToBNew = math.rotate(rotation, aToB);
            
            pointB.xyz = pointA.xyz + aToBNew;
        }

        //Adapted from: https://iquilezles.org/articles/distfunctions/

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float PointSignedDistance(float3 point)
        {
            CollisionComputeBase(point, out var pointToA, out var bToA, out var bToASqrDist, out var abLerpUnclamped);
            float abLerp = saturate(abLerpUnclamped);
            var radius = ComputeRadius(abLerp);
            return length( pointToA - bToA * abLerp ) - radius;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool ContainsPoint(float3 point) => PointSignedDistance(point) < 0;

        public readonly float3 GetRandomPointInside(ref Random random)
        {
            CollisionComputeBase(random.NextFloat3(pointA.xyz, pointB.xyz), out _, out var bToA, out _, out var abLerpUnclamped);
            var radius = ComputeRadius(abLerpUnclamped);
            var randomInRadius = random.NextFloat3Direction() * radius;
            return pointA.xyz + bToA * abLerpUnclamped + randomInRadius;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly void CollisionComputeBase(float3 point, out float3 pointToA, out float3 bToA, out float bToASqrDist, out float abLerpUnclamped)
        {
            pointToA = point - pointA.xyz;
            bToA = pointB.xyz - pointA.xyz;
            bToASqrDist = dot(bToA, bToA);
            abLerpUnclamped = dot(pointToA, bToA) / bToASqrDist;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly float ComputeRadius(float abLerp) => lerp(pointA.w, pointB.w, abLerp);

        public readonly void DrawAline(ref CommandBuilder draw, byte connectingLines = 4)
        {
            draw.WireSphere(pointA.xyz, pointA.w);
            draw.WireSphere(pointB.xyz, pointB.w);
            
            // Draw connecting lines
            var pointAtoB = pointB.xyz - pointA.xyz;
            var pointAToBNorm = normalizesafe(pointAtoB);
            var offsetDir = cross(pointAToBNorm, up());
            var rotatorForNextLine = quaternion.AxisAngle(pointAToBNorm, math.PI2 / connectingLines);
            
            for (int i = 0; i < connectingLines; i++)
            {
                DrawAline_SpecificallyAConnectingLinePlease(ref draw, offsetDir);
                offsetDir = mul(rotatorForNextLine, offsetDir);
            }
        }

        readonly void DrawAline_SpecificallyAConnectingLinePlease(ref CommandBuilder draw, float3 offsetDir)
        {
            var pA = pointA.xyz + offsetDir * pointA.w;
            var pB = pointB.xyz + offsetDir * pointB.w;
            draw.Line(pA, pB);
        }
    }
}