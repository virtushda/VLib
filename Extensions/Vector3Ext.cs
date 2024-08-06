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
    }
}