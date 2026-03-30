#if UNITY_EDITOR || DEVELOPMENT_BUILD
//#define DEBUGDRAW
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Drawing;
using Unity.Mathematics;
using UnityEngine;
using VLib.Aline;
using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;
using Random = Unity.Mathematics.Random;
using float3 = Unity.Mathematics.float3;

namespace VLib
{
    [Serializable]
    public struct CapsuleNative
    {
        public float3 pointA;
        public float3 pointB;
        public float radius;

        public CapsuleNative(float3 pointA, float3 pointB, float radius)
        {
            this.pointA = pointA;
            this.pointB = pointB;
            this.radius = radius;
        }

        public CapsuleNative(in AffineTransform transform, float3 localPointA, float3 localPointB, float radius)
        {
            pointA = math.transform(transform, localPointA);
            pointB = math.transform(transform, localPointB);
            this.radius = radius;
        }

        public readonly bool IsZero => pointA.Equals(pointB) && pointA.Equals(float3.zero) && radius == 0f;

        public readonly float3 Center => (pointA + pointB) * 0.5f;
        public readonly float3 AToB => pointB - pointA;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float FurthestDistanceFromCenter() => radius + distance(pointA, pointB) * 0.5f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly SphereNative ToEncapsulatingSphere() => new(Center, FurthestDistanceFromCenter());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly TaperedCapsuleNative ToTaperedCapsule() => new(pointA, pointB, radius);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool ContainsPoint(float3 point) => PointSignedDistance(point) <= 0f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float PointSignedDistance(float3 point)
        {
            float3 segmentVector = pointB - pointA;
            float segmentLengthSquared = dot(segmentVector, segmentVector);

            if (segmentLengthSquared <= 1e-8f)
                return distance(point, pointA) - radius;

            float lerpValue = saturate(dot(point - pointA, segmentVector) / segmentLengthSquared);
            float3 closestPoint = pointA + segmentVector * lerpValue;
            return distance(point, closestPoint) - radius;
        }

        public readonly float3 GetRandomPointInside(ref Random random)
        {
            float3 pointOnAxis = lerp(pointA, pointB, random.NextFloat());
            return pointOnAxis + random.NextFloat3Direction() * radius;
        }

        public readonly bool IntersectsRay(in Ray ray, float raySegmentLength = 100000f)
        {
            VMath.ClosestPointBetweenTwoSegments(
                new float3x2(pointA, pointB),
                new float3x2(ray.origin, ray.origin + ray.direction * raySegmentLength),
                out var closestPointOnCapsule,
                out var closestPointOnRay,
                out _);

            return distancesq(closestPointOnCapsule, closestPointOnRay) <= radius * radius;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IntersectsCapsule(in CapsuleNative other)
        {
            var thisBroadSphere = ToEncapsulatingSphere();
            var otherBroadSphere = other.ToEncapsulatingSphere();
            float maxBroadPhaseDistance = thisBroadSphere.Radius + otherBroadSphere.Radius;

            if (distancesq(thisBroadSphere.Position, otherBroadSphere.Position) > maxBroadPhaseDistance * maxBroadPhaseDistance)
                return false;

            VMath.ClosestPointBetweenTwoSegments(
                new float3x2(pointA, pointB),
                new float3x2(other.pointA, other.pointB),
                out var closestA,
                out var closestB,
                out _);

            float radiusSum = radius + other.radius;
            return distancesq(closestA, closestB) <= radiusSum * radiusSum;
        }

        public void TransformLengthScaling(in float4x4 boneTransformLocalToWorldMatrix)
        {
            float oldLength = distance(pointA, pointB);

            pointA = transform(boneTransformLocalToWorldMatrix, pointA);
            pointB = transform(boneTransformLocalToWorldMatrix, pointB);

            float newLength = distance(pointA, pointB);
            float scaleMultiplier = newLength / max(0.000001f, oldLength);
            radius *= scaleMultiplier;
        }

        public readonly void DrawAline(ref CommandBuilder draw, byte connectingLines = 4)
        {
            draw.WireSphere(pointA, radius);
            draw.WireSphere(pointB, radius);

            float3 axis = pointB - pointA;
            float3 axisNormal = normalizesafe(axis, forward());
            float3 basis = abs(axisNormal.y) > 0.99f ? right() : up();
            float3 offsetDirection = normalizesafe(cross(axisNormal, basis), right());
            quaternion lineRotator = quaternion.AxisAngle(axisNormal, PI2 / connectingLines);

            for (int i = 0; i < connectingLines; i++)
            {
                float3 pA = pointA + offsetDirection * radius;
                float3 pB = pointB + offsetDirection * radius;
                draw.Line(pA, pB);
                offsetDirection = mul(lineRotator, offsetDirection);
            }
        }
    }
}