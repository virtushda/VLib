using System;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace VLib.Tests
{
    public class TaperedCapsuleNativeTests
    {
        const float DistanceTolerance = 1e-4f;

        static TaperedCapsuleNative Capsule(float3 pointA, float3 pointB, float radiusA, float radiusB)
        {
            return new TaperedCapsuleNative(new float4(pointA, radiusA), new float4(pointB, radiusB));
        }

        [Test]
        public void PointSignedDistance_TaperedCapsule_EndpointCentersMatchEndpointRadii()
        {
            var capsule = Capsule(new float3(-1f, 0f, 0f), new float3(1f, 0f, 0f), 0.25f, 0.75f);

            Assert.That(capsule.PointSignedDistance(capsule.pointA.xyz), Is.EqualTo(-0.25f).Within(DistanceTolerance));
            Assert.That(capsule.PointSignedDistance(capsule.pointB.xyz), Is.EqualTo(-0.75f).Within(DistanceTolerance));
        }

        [Test]
        public void PointSignedDistance_DegenerateCapsule_BehavesLikeSphere()
        {
            var capsule = Capsule(new float3(2f, 3f, 4f), new float3(2f, 3f, 4f), 1.25f, 1.25f);

            Assert.That(capsule.PointSignedDistance(new float3(2f, 3f, 4f)), Is.EqualTo(-1.25f).Within(DistanceTolerance));
            Assert.That(capsule.PointSignedDistance(new float3(3f, 3f, 4f)), Is.EqualTo(-0.25f).Within(DistanceTolerance));
            Assert.That(capsule.PointSignedDistance(new float3(4.25f, 3f, 4f)), Is.EqualTo(1f).Within(DistanceTolerance));
        }

        [Test]
        public void PointSignedDistance_ContainedCapsule_CollapsesToDominantSphere()
        {
            var capsule = Capsule(new float3(0f, 0f, 0f), new float3(0.5f, 0f, 0f), 3f, 1f);

            Assert.That(capsule.PointSignedDistance(new float3(0f, 0f, 0f)), Is.EqualTo(-3f).Within(DistanceTolerance));
            Assert.That(capsule.PointSignedDistance(new float3(3f, 0f, 0f)), Is.EqualTo(0f).Within(DistanceTolerance));
            Assert.That(capsule.PointSignedDistance(new float3(4f, 0f, 0f)), Is.EqualTo(1f).Within(DistanceTolerance));
        }

        [Test]
        public void PointSignedDistance_TranslationInvariant()
        {
            var capsule = Capsule(new float3(-1f, -2f, 0.25f), new float3(2f, 1.5f, 3f), 0.2f, 0.9f);
            var point = new float3(0.5f, -0.25f, 1.1f);
            var translation = new float3(7.5f, -3.25f, 2.75f);

            var translatedCapsule = capsule;
            translatedCapsule.Translate(translation);

            Assert.That(
                translatedCapsule.PointSignedDistance(point + translation),
                Is.EqualTo(capsule.PointSignedDistance(point)).Within(DistanceTolerance));
        }

        [Test]
        public void PointSignedDistance_MatchesNumericReferenceForRepresentativeCases()
        {
            var capsules = new[]
            {
                Capsule(new float3(-1.5f, 0.5f, -0.25f), new float3(0.75f, 2.5f, 1.5f), 0.2f, 0.9f),
                Capsule(new float3(0f, 0f, 0f), new float3(0.5f, 0f, 0f), 3f, 1f),
                Capsule(new float3(1f, 2f, 3f), new float3(1f, 2f, 3f), 1.75f, 1.75f),
            };

            var points = new[]
            {
                new float3(0f, 0f, 0f),
                new float3(0f, 0.25f, 0f),
                new float3(0f, 1.25f, 0f),
                new float3(2.5f, 0f, 0f),
                new float3(-3f, -1f, 2f),
                new float3(1f, 2f, 4.5f),
            };

            foreach (var capsule in capsules)
            {
                foreach (var point in points)
                {
                    var expected = ReferencePointSignedDistance(capsule, point);
                    var actual = capsule.PointSignedDistance(point);

                    Assert.That(actual, Is.EqualTo(expected).Within(2e-3f), $"Capsule {capsule.pointA} -> {capsule.pointB}, point {point}");
                    Assert.That(capsule.ContainsPoint(point), Is.EqualTo(actual <= 0f));
                }
            }
        }

        [Test]
        public void IntersectsCapsuleExact_RejectsViaOuterCapsule()
        {
            var a = Capsule(new float3(-1f, 0f, 0f), new float3(1f, 0f, 0f), 0.25f, 0.75f);
            var b = Capsule(new float3(10f, 0f, 0f), new float3(12f, 0f, 0f), 0.25f, 0.75f);

            Assert.That(a.IntersectsCapsuleExact(b), Is.False);
        }

        [Test]
        public void IntersectsCapsuleExact_AcceptsViaInnerCapsule()
        {
            var a = Capsule(new float3(-1f, 0f, 0f), new float3(1f, 0f, 0f), 0.75f, 1f);
            var b = Capsule(new float3(-0.5f, 0f, 0f), new float3(1.5f, 0f, 0f), 0.75f, 1f);

            Assert.That(a.IntersectsCapsuleExact(b), Is.True);
        }

        [Test]
        public void TryConvertToUniformCapsule_ReturnsFalseForTaperedShape()
        {
            var capsule = Capsule(new float3(-1f, 0f, 0f), new float3(1f, 0f, 0f), 0.25f, 0.75f);

            Assert.That(capsule.TryConvertToCapsuleNative(out var uniformCapsule), Is.False);
            Assert.That(uniformCapsule.IsZero, Is.True);
        }

        [Test]
        public void IntersectsRay_TangentRayCountsAsIntersection()
        {
            var capsule = Capsule(new float3(-1f, 0f, 0f), new float3(1f, 0f, 0f), 0.5f, 0.5f);
            var ray = new Ray(new Vector3(0f, 0.5f, -2f), Vector3.forward);

            Assert.That(capsule.IntersectsRay(ray), Is.True);
        }

        static float ReferencePointSignedDistance(in TaperedCapsuleNative capsule, float3 point)
        {
            const int sampleCount = 2048;

            float bestT = 0f;
            float bestValue = float.PositiveInfinity;

            for (int i = 0; i <= sampleCount; i++)
            {
                float t = i / (float)sampleCount;
                float value = SurfaceDistanceAt(capsule, point, t);
                if (value < bestValue)
                {
                    bestValue = value;
                    bestT = t;
                }
            }

            float interval = 1f / sampleCount;
            float lower = math.max(0f, bestT - interval);
            float upper = math.min(1f, bestT + interval);

            for (int i = 0; i < 32; i++)
            {
                float left = lower + (upper - lower) / 3f;
                float right = upper - (upper - lower) / 3f;

                float leftValue = SurfaceDistanceAt(capsule, point, left);
                float rightValue = SurfaceDistanceAt(capsule, point, right);

                if (leftValue < rightValue)
                    upper = right;
                else
                    lower = left;
            }

            return SurfaceDistanceAt(capsule, point, (lower + upper) * 0.5f);
        }

        static float SurfaceDistanceAt(in TaperedCapsuleNative capsule, float3 point, float t)
        {
            float3 center = math.lerp(capsule.pointA.xyz, capsule.pointB.xyz, t);
            float radius = math.lerp(capsule.pointA.w, capsule.pointB.w, t);
            return math.distance(point, center) - radius;
        }
    }
}