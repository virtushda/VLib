using System;
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
    [Serializable]
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
            pointA = new float4(math.transform(transform, localPointA), radiusA);
            pointB = new float4(math.transform(transform, localPointB), radiusB);
        }

        public CapsuleNative(CapsuleCollider capsuleCollider)
        {
            Vector3 direction = capsuleCollider.direction == 0 ? Vector3.right : capsuleCollider.direction == 1 ? Vector3.up : Vector3.forward;
            pointA = new float4(capsuleCollider.transform.TransformPoint(capsuleCollider.center - direction * capsuleCollider.height * 0.5f), capsuleCollider.radius);
            pointB = new float4(capsuleCollider.transform.TransformPoint(capsuleCollider.center + direction * capsuleCollider.height * 0.5f), capsuleCollider.radius);
        }

        public float3 Center => (pointA.xyz + pointB.xyz) * .5f;
        public float3 AToB => pointB.xyz - pointA.xyz;

        /// <returns> Largest distance from the line defining the capsule. </returns>
        public readonly float MaxRadius() => max(pointA.w, pointB.w);
        
        /// <returns> Furthest distance from the center of the capsule. </returns>
        public readonly float FurthestDistanceFromCenter() => max(pointA.w, pointB.w) + distance(pointB.xyz, pointA.xyz) * 0.5f;

        public readonly SphereNative ToEncapsulatingSphere() => new((pointA.xyz + pointB.xyz) * 0.5f, FurthestDistanceFromCenter());

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
            var aToBNew = rotate(rotation, aToB);
            
            pointB.xyz = pointA.xyz + aToBNew;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool ContainsPoint(float3 point) => PointSignedDistance(point) < 0;

        //Adapted from: https://iquilezles.org/articles/distfunctions/
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float PointSignedDistance(float3 point)
        {
            CollisionComputeBase(point, out var pointToA, out var bToA, out var bToASqrDist, out var abLerpUnclamped);
            float abLerp = saturate(abLerpUnclamped);
            var radius = ComputeRadius(abLerp);
            
            return length( pointToA - bToA * abLerp ) - radius;
        }
        
        public readonly float3 GetRandomPointInside(ref Random random)
        {
            CollisionComputeBase(random.NextFloat3(pointA.xyz, pointB.xyz), out _, out var bToA, out _, out var abLerpUnclamped);
            var radius = ComputeRadius(abLerpUnclamped);
            var randomInRadius = random.NextFloat3Direction() * radius;
            return pointA.xyz + bToA * abLerpUnclamped + randomInRadius;
        }

        /// <summary> Computes whether the ray intersects the capsule. </summary>
        /// <param name="ray"> The ray to check for intersection. </param>
        /// <param name="raySegmentLength"> The length of the ray segment to check for intersection. Technically a ray is infinite, but a super long segment should work fine. </param>
        /// <returns> True if the ray intersects the capsule. </returns>
        public readonly bool IntersectsRay(in Ray ray, float raySegmentLength = 100000)
        {
            VMath.ClosestPointBetweenTwoSegments(new float3x2(pointA.xyz, pointB.xyz), new float3x2(ray.origin, ray.origin + ray.direction * raySegmentLength),
                out var closestPointOnCapsule, out var closestPointOnRay, out var capsuleABLerp);
            
            var radius = ComputeRadius(capsuleABLerp);
            return distancesq(closestPointOnCapsule, closestPointOnRay) < radius * radius;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CollisionComputeBase(float3 point, out float3 pointToA, out float3 bToA, out float bToASqrDist, out float abLerpUnclamped)
        {
            pointToA = point - pointA.xyz;
            bToA = pointB.xyz - pointA.xyz;
            bToASqrDist = dot(bToA, bToA);
            abLerpUnclamped = dot(pointToA, bToA) / bToASqrDist;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float ComputeRadius(float abLerp) => lerp(pointA.w, pointB.w, abLerp);

        /// <summary> Transforms this capsule by a local to world matrix. <br/>
        /// Radius is scaled by the change in the length of the capsule. </summary>
        public void TransformLengthScaling(in float4x4 boneTransformLocalToWorldMatrix)
        {
            var oldLength = distance(pointA.xyz, pointB.xyz);
            
            // Points
            pointA.xyz = transform(boneTransformLocalToWorldMatrix, pointA.xyz);
            pointB.xyz = transform(boneTransformLocalToWorldMatrix, pointB.xyz);
            
            // Radii - Uniform, scale by the change in length
            var newLength = distance(pointA.xyz, pointB.xyz);
            var scaleMult = newLength / max(.000001f, oldLength);
            pointA.w *= scaleMult;
            pointB.w *= scaleMult;
        }

        public readonly void DrawAline(ref CommandBuilder draw, byte connectingLines = 4)
        {
            draw.WireSphere(pointA.xyz, pointA.w);
            draw.WireSphere(pointB.xyz, pointB.w);
            
            // Draw connecting lines
            var pointAtoB = pointB.xyz - pointA.xyz;
            var pointAToBNorm = normalizesafe(pointAtoB);
            var offsetDir = cross(pointAToBNorm, up());
            var rotatorForNextLine = quaternion.AxisAngle(pointAToBNorm, PI2 / connectingLines);
            
            for (int i = 0; i < connectingLines; i++)
            {
                DrawConnectingLine(ref draw, offsetDir);
                offsetDir = mul(rotatorForNextLine, offsetDir);
            }
        }

        readonly void DrawConnectingLine(ref CommandBuilder draw, float3 offsetDir)
        {
            var pA = pointA.xyz + offsetDir * pointA.w;
            var pB = pointB.xyz + offsetDir * pointB.w;
            draw.Line(pA, pB);
        }
    }
}