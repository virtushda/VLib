using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace VLib
{
    public static class Vector3Ext
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 XZ(this Vector3 v) => new(v.x, v.z);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 XZVec2(this Vector3 v) => new Vector2(v.x, v.z);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 XZ_Y0(this Vector3 v) => new Vector3(v.x, 0, v.z);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Ray TransformRay(this Transform tr, Ray ray)
        {
            var origin = tr.TransformPoint(ray.origin);
            var direction = tr.TransformDirection(ray.direction);
            return new Ray(origin, direction);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Ray InverseTransformRay(this Transform tr, Ray ray)
        {
            var origin = tr.InverseTransformPoint(ray.origin);
            var direction = tr.InverseTransformDirection(ray.direction);
            return new Ray(origin, direction);
        }
    }
}