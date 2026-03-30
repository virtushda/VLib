using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace VLib.Tests
{
    public class CapsuleNativeTests
    {
        const float DistanceTolerance = 1e-4f;

        static CapsuleNative Capsule(float3 pointA, float3 pointB, float radius)
        {
            return new CapsuleNative(pointA, pointB, radius);
        }

        [Test]
        public void PointSignedDistance_UniformCapsule_MatchesExpectedValues()
        {
            var capsule = Capsule(new float3(-1f, 0f, 0f), new float3(1f, 0f, 0f), 0.5f);

            Assert.That(capsule.PointSignedDistance(new float3(0f, 0f, 0f)), Is.EqualTo(-0.5f).Within(DistanceTolerance));
            Assert.That(capsule.PointSignedDistance(new float3(0f, 0.5f, 0f)), Is.EqualTo(0f).Within(DistanceTolerance));
            Assert.That(capsule.PointSignedDistance(new float3(0f, 1.5f, 0f)), Is.EqualTo(1f).Within(DistanceTolerance));
            Assert.That(capsule.PointSignedDistance(new float3(1.5f, 0f, 0f)), Is.EqualTo(0f).Within(DistanceTolerance));
        }

        [Test]
        public void PointSignedDistance_DegenerateCapsule_BehavesLikeSphere()
        {
            var capsule = Capsule(new float3(2f, 3f, 4f), new float3(2f, 3f, 4f), 1.25f);

            Assert.That(capsule.PointSignedDistance(new float3(2f, 3f, 4f)), Is.EqualTo(-1.25f).Within(DistanceTolerance));
            Assert.That(capsule.PointSignedDistance(new float3(3f, 3f, 4f)), Is.EqualTo(-0.25f).Within(DistanceTolerance));
            Assert.That(capsule.PointSignedDistance(new float3(4.25f, 3f, 4f)), Is.EqualTo(1f).Within(DistanceTolerance));
        }

        [Test]
        public void PointSignedDistance_TranslationInvariant()
        {
            var capsule = Capsule(new float3(-1f, -2f, 0.25f), new float3(2f, 1.5f, 3f), 0.9f);
            var point = new float3(0.5f, -0.25f, 1.1f);
            var translation = new float3(7.5f, -3.25f, 2.75f);

            var translatedCapsule = capsule;
            translatedCapsule.pointA += translation;
            translatedCapsule.pointB += translation;

            Assert.That(
                translatedCapsule.PointSignedDistance(point + translation),
                Is.EqualTo(capsule.PointSignedDistance(point)).Within(DistanceTolerance));
        }

        [Test]
        public void ToTaperedCapsule_PreservesUniformRadius()
        {
            var capsule = Capsule(new float3(-1f, 0f, 0f), new float3(1f, 0f, 0f), 0.75f);

            var tapered = capsule.ToTaperedCapsule();

            Assert.That(tapered.pointA.xyz, Is.EqualTo(capsule.pointA));
            Assert.That(tapered.pointB.xyz, Is.EqualTo(capsule.pointB));
            Assert.That(tapered.pointA.w, Is.EqualTo(capsule.radius).Within(DistanceTolerance));
            Assert.That(tapered.pointB.w, Is.EqualTo(capsule.radius).Within(DistanceTolerance));
        }

        [Test]
        public void UniformCapsule_RoundTripsThroughTryConvertToCapsuleNative()
        {
            var capsule = Capsule(new float3(-1f, 0f, 0f), new float3(1f, 0f, 0f), 0.5f);

            var tapered = capsule.ToTaperedCapsule();

            Assert.That(tapered.TryConvertToCapsuleNative(out var roundTrippedCapsule), Is.True);

            Assert.That(roundTrippedCapsule.pointA, Is.EqualTo(capsule.pointA));
            Assert.That(roundTrippedCapsule.pointB, Is.EqualTo(capsule.pointB));
            Assert.That(roundTrippedCapsule.radius, Is.EqualTo(capsule.radius).Within(DistanceTolerance));
        }

        [Test]
        public void IntersectsRay_TangentRayCountsAsIntersection()
        {
            var capsule = Capsule(new float3(-1f, 0f, 0f), new float3(1f, 0f, 0f), 0.5f);
            var ray = new Ray(new Vector3(0f, 0.5f, -2f), Vector3.forward);

            Assert.That(capsule.IntersectsRay(ray), Is.True);
        }
    }
}