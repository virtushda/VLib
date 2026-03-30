#if UNITY_EDITOR || DEVELOPMENT_BUILD
//#define DEBUGDRAW
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Drawing;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using VLib.Aline;
using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;
using Random = Unity.Mathematics.Random;
using float4 = Unity.Mathematics.float4;

namespace VLib
{
    /// <summary> A capsule with variable radii at both endpoints. </summary>
    [Serializable]
    public struct TaperedCapsuleNative
    {
        public float4 pointA;
        public float4 pointB;

        /// <summary> float4 is Point-Radius </summary>
        public TaperedCapsuleNative(float4 pointA, float4 pointB)
        {
            this.pointA = pointA;
            this.pointB = pointB;
        }
        
        public TaperedCapsuleNative(float3 pointA, float3 pointB, float radius)
        {
            this.pointA = new float4(pointA, radius);
            this.pointB = new float4(pointB, radius);
        }

        public static explicit operator TaperedCapsuleNative(CapsuleNative capsule) => capsule.ToTaperedCapsule();

        public readonly bool TryConvertToCapsuleNative(out CapsuleNative capsule)
        {
            if (abs(pointA.w - pointB.w) > 1e-6f)
            {
                capsule = default;
                return false;
            }

            capsule = new CapsuleNative(pointA.xyz, pointB.xyz, pointA.w);
            return true;
        }

        /// <summary> Generate a capsule from a world transform and some local points. </summary>
        public TaperedCapsuleNative(in AffineTransform transform, float3 localPointA, float3 localPointB, float radiusA, float radiusB)
        {
            pointA = new float4(math.transform(transform, localPointA), radiusA);
            pointB = new float4(math.transform(transform, localPointB), radiusB);
        }

        public TaperedCapsuleNative(CapsuleCollider capsuleCollider)
        {
            Vector3 direction = capsuleCollider.direction == 0 ? Vector3.right : capsuleCollider.direction == 1 ? Vector3.up : Vector3.forward;
            pointA = new float4(capsuleCollider.transform.TransformPoint(capsuleCollider.center - direction * capsuleCollider.height * 0.5f), capsuleCollider.radius);
            pointB = new float4(capsuleCollider.transform.TransformPoint(capsuleCollider.center + direction * capsuleCollider.height * 0.5f), capsuleCollider.radius);
        }

        public readonly bool IsZero => pointA.Equals(pointB) && pointA.Equals(float4.zero); // A==B check most likely to reject early, but still technically valid, so zero check after.
        
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
        public readonly bool ContainsPoint(float3 point) => PointSignedDistance(point) <= 0;

        //Adapted from: https://iquilezles.org/articles/distfunctions/
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float PointSignedDistance(float3 point)
        {
            float3 startPosition = pointA.xyz;
            float3 endPosition = pointB.xyz;
            float startRadius = pointA.w;
            float endRadius = pointB.w;

            CollapseContainedCapsuleToSphere(ref startPosition, ref endPosition, ref startRadius, ref endRadius);

            float3 segmentVector = endPosition - startPosition;
            float segmentLengthSquared = dot(segmentVector, segmentVector);

            if (segmentLengthSquared <= 1e-8f)
                return distance(point, startPosition) - max(startRadius, endRadius);

            float radiusDelta = startRadius - endRadius;
            float radialLengthSquared = segmentLengthSquared - radiusDelta * radiusDelta;
            float inverseSegmentLengthSquared = 1f / segmentLengthSquared;

            float3 pointFromStart = point - startPosition;
            float segmentProjection = dot(pointFromStart, segmentVector);
            float projectionFromEnd = segmentProjection - segmentLengthSquared;

            float3 radialVector = pointFromStart * segmentLengthSquared - segmentVector * segmentProjection;
            float radialDistanceSquared = dot(radialVector, radialVector);
            float startProjectionSquared = segmentProjection * segmentProjection * segmentLengthSquared;
            float endProjectionSquared = projectionFromEnd * projectionFromEnd * segmentLengthSquared;

            float capSwitch = sign(radiusDelta) * radiusDelta * radiusDelta * radialDistanceSquared;

            if (sign(projectionFromEnd) * radialLengthSquared * endProjectionSquared > capSwitch)
                return sqrt(radialDistanceSquared + endProjectionSquared) * inverseSegmentLengthSquared - endRadius;

            if (sign(segmentProjection) * radialLengthSquared * startProjectionSquared < capSwitch)
                return sqrt(radialDistanceSquared + startProjectionSquared) * inverseSegmentLengthSquared - startRadius;

            return (sqrt(radialDistanceSquared * radialLengthSquared * inverseSegmentLengthSquared) + segmentProjection * radiusDelta) * inverseSegmentLengthSquared - startRadius;
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
            
            // Clamp capsule lerp to stay on the capsule
            capsuleABLerp = saturate(capsuleABLerp);
            var radius = ComputeRadius(capsuleABLerp);
            
            IntersectsRayDebug(ray, closestPointOnCapsule, closestPointOnRay, capsuleABLerp, radius);
            
            return distancesq(closestPointOnCapsule, closestPointOnRay) <= radius * radius;
        }

        [Conditional("DEBUGDRAW")]
        readonly void IntersectsRayDebug(in Ray ray, float3 closestPointOnSegAb, float3 closestPointOnSegCd, float capsuleABLerpT, float radius)
        {
            AlineBurst.EnqueueRay(ray, 10000, Color.white);
            AlineBurst.EnqueueSphere(closestPointOnSegAb, .05f, Color.white);
            AlineBurst.EnqueueSphere(closestPointOnSegCd, .05f, ColorExt.blueBright);
            AlineBurst.EnqueueCapsule(this, Color.green);
            AlineBurst.EnqueueSphere(math.lerp(pointA.xyz, pointB.xyz, capsuleABLerpT), radius, ColorExt.orange);
        }

        /// <summary> Fast intersection check that's less accurate for capsules with large radius changes. </summary>
        public readonly bool ApproximatelyIntersects(in TaperedCapsuleNative other)
        {
            // Cheap broad phase
            var aSphere = ToEncapsulatingSphere();
            var bSphere = other.ToEncapsulatingSphere();
            var maxDist = aSphere.Radius + bSphere.Radius;
            if (math.distancesq(aSphere.Position, bSphere.Position) > maxDist * maxDist)
                return false;

            // Narrow phase
            VMath.ClosestPointBetweenTwoSegments(
                new float3x2(pointA.xyz, pointB.xyz),
                new float3x2(other.pointA.xyz, other.pointB.xyz),
                out var closestA,
                out var closestB,
                out var tA);

            var tB = VMath.InverseSampleLineSegment(other.pointA.xyz, other.pointB.xyz, closestB);

            var r = ComputeRadius(math.saturate(tA)) + other.ComputeRadius(math.saturate(tB));
            return math.distancesq(closestA, closestB) <= r * r;
        }

        public readonly CapsuleNative ToUniformOuter() => new(pointA.xyz, pointB.xyz, max(pointA.w, pointB.w));

        public readonly CapsuleNative ToUniformInner() => new(pointA.xyz, pointB.xyz, min(pointA.w, pointB.w));

        /// <summary>
        /// Returns true if this tapered capsule intersects another tapered capsule.
        /// Uses a sphere reject, capsule bounds checks, an approximate accept, then an exact quadratic solve.
        /// </summary>
        /// <remarks> The <c>coefficient*</c> values form the quadratic used to test center separation against combined radius. </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IntersectsCapsuleExact(in TaperedCapsuleNative other)
        {
            // Quick sphere reject.
            var thisBroadSphere = ToEncapsulatingSphere();
            var otherBroadSphere = other.ToEncapsulatingSphere();

            float maxBroadPhaseDistance = thisBroadSphere.Radius + otherBroadSphere.Radius;
            if (distancesq(thisBroadSphere.Position, otherBroadSphere.Position) > maxBroadPhaseDistance * maxBroadPhaseDistance)
                return false;

            if (!ToUniformOuter().IntersectsCapsule(other.ToUniformOuter()))
                return false;

            if (ToUniformInner().IntersectsCapsule(other.ToUniformInner()))
                return true;

            // Cheap accept for obvious hits.
            if (ApproximatelyIntersects(other))
                return true;

            float3 thisStartPosition = pointA.xyz;
            float3 thisEndPosition = pointB.xyz;
            float thisStartRadius = pointA.w;
            float thisEndRadius = pointB.w;

            float3 otherStartPosition = other.pointA.xyz;
            float3 otherEndPosition = other.pointB.xyz;
            float otherStartRadius = other.pointA.w;
            float otherEndRadius = other.pointB.w;

            // Collapse degenerate capsules that are really just one dominant sphere.
            CollapseContainedCapsuleToSphere(ref thisStartPosition, ref thisEndPosition, ref thisStartRadius, ref thisEndRadius);
            CollapseContainedCapsuleToSphere(ref otherStartPosition, ref otherEndPosition, ref otherStartRadius, ref otherEndRadius);

            float3 thisSegmentVector = thisEndPosition - thisStartPosition;
            float3 otherSegmentVector = otherEndPosition - otherStartPosition;
            float3 startToStartVector = thisStartPosition - otherStartPosition;

            float thisRadiusDelta = thisEndRadius - thisStartRadius;
            float otherRadiusDelta = otherEndRadius - otherStartRadius;
            float combinedStartRadius = thisStartRadius + otherStartRadius;

            // Quadratic form for center distance minus squared combined radius.
            float coefficientSS = dot(thisSegmentVector, thisSegmentVector) - thisRadiusDelta * thisRadiusDelta;
            float coefficientST = -(dot(thisSegmentVector, otherSegmentVector) + thisRadiusDelta * otherRadiusDelta);
            float coefficientTT = dot(otherSegmentVector, otherSegmentVector) - otherRadiusDelta * otherRadiusDelta;
            float coefficientS = dot(startToStartVector, thisSegmentVector) - combinedStartRadius * thisRadiusDelta;
            float coefficientT = -(dot(startToStartVector, otherSegmentVector) + combinedStartRadius * otherRadiusDelta);

            float scaleEstimate =
                1f +
                dot(startToStartVector, startToStartVector) +
                dot(thisSegmentVector, thisSegmentVector) +
                dot(otherSegmentVector, otherSegmentVector) +
                combinedStartRadius * combinedStartRadius +
                thisRadiusDelta * thisRadiusDelta +
                otherRadiusDelta * otherRadiusDelta;

            float separationTolerance = 1e-6f * scaleEstimate;

            // Check the interior stationary point first.
            float determinant = coefficientSS * coefficientTT - coefficientST * coefficientST;
            float determinantTolerance = 1e-6f * (1f + abs(coefficientSS * coefficientTT) + coefficientST * coefficientST);

            if (determinant > determinantTolerance &&
                coefficientSS > separationTolerance &&
                coefficientTT > separationTolerance)
            {
                float thisParameter = (coefficientST * coefficientT - coefficientTT * coefficientS) / determinant;
                float otherParameter = (coefficientST * coefficientS - coefficientSS * coefficientT) / determinant;

                if (thisParameter >= 0f && thisParameter <= 1f &&
                    otherParameter >= 0f && otherParameter <= 1f)
                {
                    float interiorSeparation = EvaluateSquaredSeparationMinusRadiusSumSquared(
                        startToStartVector,
                        thisSegmentVector,
                        otherSegmentVector,
                        combinedStartRadius,
                        thisRadiusDelta,
                        otherRadiusDelta,
                        thisParameter,
                        otherParameter);

                    if (interiorSeparation <= separationTolerance)
                        return true;
                }
            }

                    // If the minimum is not inside the square, it must be on an edge.
            if (FindMinimumOnEdgeWithFixedThisParameter(
                    0f,
                    coefficientSS, coefficientST, coefficientTT, coefficientS, coefficientT,
                    startToStartVector, thisSegmentVector, otherSegmentVector,
                    combinedStartRadius, thisRadiusDelta, otherRadiusDelta) <= separationTolerance)
                return true;

            if (FindMinimumOnEdgeWithFixedThisParameter(
                    1f,
                    coefficientSS, coefficientST, coefficientTT, coefficientS, coefficientT,
                    startToStartVector, thisSegmentVector, otherSegmentVector,
                    combinedStartRadius, thisRadiusDelta, otherRadiusDelta) <= separationTolerance)
                return true;

            if (FindMinimumOnEdgeWithFixedOtherParameter(
                    0f,
                    coefficientSS, coefficientST, coefficientTT, coefficientS, coefficientT,
                    startToStartVector, thisSegmentVector, otherSegmentVector,
                    combinedStartRadius, thisRadiusDelta, otherRadiusDelta) <= separationTolerance)
                return true;

            if (FindMinimumOnEdgeWithFixedOtherParameter(
                    1f,
                    coefficientSS, coefficientST, coefficientTT, coefficientS, coefficientT,
                    startToStartVector, thisSegmentVector, otherSegmentVector,
                    combinedStartRadius, thisRadiusDelta, otherRadiusDelta) <= separationTolerance)
                return true;

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float EvaluateSquaredSeparationMinusRadiusSumSquared(
            float3 startToStartVector,
            float3 thisSegmentVector,
            float3 otherSegmentVector,
            float combinedStartRadius,
            float thisRadiusDelta,
            float otherRadiusDelta,
            float thisParameter,
            float otherParameter)
        {
            // Evaluate the separation at one pair of capsule parameters.
            float3 centerDelta =
                startToStartVector +
                thisSegmentVector * thisParameter -
                otherSegmentVector * otherParameter;

            float combinedRadius =
                combinedStartRadius +
                thisRadiusDelta * thisParameter +
                otherRadiusDelta * otherParameter;

            return dot(centerDelta, centerDelta) - combinedRadius * combinedRadius;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float FindMinimumOnEdgeWithFixedThisParameter(
            float fixedThisParameter,
            float coefficientSS,
            float coefficientST,
            float coefficientTT,
            float coefficientS,
            float coefficientT,
            float3 startToStartVector,
            float3 thisSegmentVector,
            float3 otherSegmentVector,
            float combinedStartRadius,
            float thisRadiusDelta,
            float otherRadiusDelta)
        {
            const float epsilon = 1e-8f;

            // For a valid edge parabola, use its stationary point.
            if (coefficientTT > epsilon)
            {
                float otherParameter = saturate(-(coefficientT + coefficientST * fixedThisParameter) / coefficientTT);

                return EvaluateSquaredSeparationMinusRadiusSumSquared(
                    startToStartVector,
                    thisSegmentVector,
                    otherSegmentVector,
                    combinedStartRadius,
                    thisRadiusDelta,
                    otherRadiusDelta,
                    fixedThisParameter,
                    otherParameter);
            }

            float atStart = EvaluateSquaredSeparationMinusRadiusSumSquared(
                startToStartVector,
                thisSegmentVector,
                otherSegmentVector,
                combinedStartRadius,
                thisRadiusDelta,
                otherRadiusDelta,
                fixedThisParameter,
                0f);

            float atEnd = EvaluateSquaredSeparationMinusRadiusSumSquared(
                startToStartVector,
                thisSegmentVector,
                otherSegmentVector,
                combinedStartRadius,
                thisRadiusDelta,
                otherRadiusDelta,
                fixedThisParameter,
                1f);

            return min(atStart, atEnd);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float FindMinimumOnEdgeWithFixedOtherParameter(
            float fixedOtherParameter,
            float coefficientSS,
            float coefficientST,
            float coefficientTT,
            float coefficientS,
            float coefficientT,
            float3 startToStartVector,
            float3 thisSegmentVector,
            float3 otherSegmentVector,
            float combinedStartRadius,
            float thisRadiusDelta,
            float otherRadiusDelta)
        {
            const float epsilon = 1e-8f;

            // For a valid edge parabola, use its stationary point.
            if (coefficientSS > epsilon)
            {
                float thisParameter = saturate(-(coefficientS + coefficientST * fixedOtherParameter) / coefficientSS);

                return EvaluateSquaredSeparationMinusRadiusSumSquared(
                    startToStartVector,
                    thisSegmentVector,
                    otherSegmentVector,
                    combinedStartRadius,
                    thisRadiusDelta,
                    otherRadiusDelta,
                    thisParameter,
                    fixedOtherParameter);
            }

            float atStart = EvaluateSquaredSeparationMinusRadiusSumSquared(
                startToStartVector,
                thisSegmentVector,
                otherSegmentVector,
                combinedStartRadius,
                thisRadiusDelta,
                otherRadiusDelta,
                0f,
                fixedOtherParameter);

            float atEnd = EvaluateSquaredSeparationMinusRadiusSumSquared(
                startToStartVector,
                thisSegmentVector,
                otherSegmentVector,
                combinedStartRadius,
                thisRadiusDelta,
                otherRadiusDelta,
                1f,
                fixedOtherParameter);

            return min(atStart, atEnd);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CollapseContainedCapsuleToSphere(
            ref float3 endpointA,
            ref float3 endpointB,
            ref float radiusA,
            ref float radiusB)
        {
            // If one endpoint sphere contains the other, keep the dominant sphere.
            float3 endpointDelta = endpointB - endpointA;
            float radiusDelta = radiusB - radiusA;

            if (lengthsq(endpointDelta) <= radiusDelta * radiusDelta)
            {
                if (radiusA >= radiusB)
                {
                    endpointB = endpointA;
                    radiusB = radiusA;
                }
                else
                {
                    endpointA = endpointB;
                    radiusA = radiusB;
                }
            }
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
            var pointAToBNorm = normalizesafe(pointAtoB, forward());
            var basis = abs(pointAToBNorm.y) > 0.99f ? right() : up();
            var offsetDir = normalizesafe(cross(pointAToBNorm, basis), right());
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