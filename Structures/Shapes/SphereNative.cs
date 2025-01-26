using Unity.Mathematics;
using UnityEngine;

namespace VLib
{
    [System.Serializable]
    public struct SphereNative
    {
        public float4 pointRadius;

        public float3 Position
        {
            readonly get => pointRadius.xyz;
            set => pointRadius.xyz = value;
        }
        public float Radius
        {
            readonly get => pointRadius.w;
            set => pointRadius.w = value;
        }
        
        public SphereNative(float3 position, float radius) => pointRadius = new float4(position, radius);

        public SphereNative(float4 pointRadius) => this.pointRadius = pointRadius;
        
        public readonly bool Contains(float3 point) => math.distancesq(point, Position) <= Radius * Radius;
        
        public readonly bool IntersectsRay(in Ray ray) => Contains(VMath.ClosestPointOnRay(ray.origin, ray.direction, Position));
        
        public static implicit operator float4(SphereNative sphere) => sphere.pointRadius;
        public static explicit operator SphereNative(float4 pointRadius) => new SphereNative(pointRadius);
    }
}