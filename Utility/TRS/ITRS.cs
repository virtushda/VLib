using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace VLib
{
    public interface ITRS
    {
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 Scale { get; set; }
        public float3 PositionNative { get; set; }
        public quaternion RotationNative { get; set; }
        public float3 ScaleNative { get; set; }
    }

    public static class ITRSExt
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ApplyToTransform<T>(this T trs, Transform t)
            where T : ITRS
        {
            t.SetPositionAndRotation(trs.Position, trs.Rotation);
            t.localScale = trs.Scale;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ApplyPosRotToTransformAccess(in this TRS trs, ref TransformAccess t)
        {
            t.position = trs.position;
            t.rotation = trs.rotation;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetTRS<T>(this Transform t, bool localScale = false)
            where T : struct, ITRS
        {
            T trs = new T();
            trs.Position = t.position;
            trs.Rotation = t.rotation;
            if (localScale)
                trs.Scale = t.localScale;
            else
                trs.Scale = t.lossyScale;
            return trs;
        }

        /// <summary> Get TRS from TransformAccess, scale is local! </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetTRS<T>(this ref TransformAccess t)
            where T : struct, ITRS
        {
            T trs = new T();
            trs.Position = t.position;
            trs.Rotation = t.rotation;
            trs.Scale = t.localScale;
            return trs;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4x4 ToMatrix<T>(this T trs)
            where T : ITRS => float4x4.TRS(trs.PositionNative, trs.RotationNative, trs.ScaleNative);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 LocalDirToWorld<T>(this T trs, Vector3 direction)
            where T : unmanaged, ITRS => trs.Rotation * direction;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T MultWith<T>(this T trs, float4x4 matrix)
            where T : unmanaged, ITRS
        {
            var m = trs.ToMatrix();
            m = math.mul(matrix, m);
            return m.ToTRS<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Forward<T>(this T trs)
            where T : struct, ITRS
        {
            return math.rotate(trs.RotationNative, VMath.Forward3);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetForward<T>(this ref T trs, float3 forward)
            where T : struct, ITRS
        {
            trs.RotationNative = quaternion.LookRotation(forward, trs.Up());
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetForwardXZ<T>(this ref T trs, float3 forward)
            where T : struct, ITRS
        {
            var forwardXZ = forward;
            forwardXZ.y = 0f;
            trs.RotationNative = quaternion.LookRotation(forwardXZ, VMath.Up3);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ForwardXZ<T>(this T trs)
            where T : struct, ITRS
        {
            var forward = math.rotate(trs.RotationNative, VMath.Forward3);
            forward.y = 0f;
            return math.normalize(forward);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Right<T>(this T trs)
            where T : struct, ITRS
        {
            return math.rotate(trs.RotationNative, VMath.Right3);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 RightXZ<T>(this T trs)
            where T : struct, ITRS
        {
            var right = trs.Right();
            right.y = 0f;
            return math.normalize(right);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Up<T>(this T trs)
            where T : struct, ITRS
        {
            return math.rotate(trs.RotationNative, VMath.Up3);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TranslationRotation ToTR(this float3 pos) => new(pos);

        /// <summary>Sets the rotation of the TRS to an invalid value to denote no rotation</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NullRot<T>(this ref T trs)
            where T : struct, ITRS
        {
            trs.RotationNative = default;
        }

        /// <summary>Returns a copy of the TRS with rotation set to an invalid value to denote no rotation</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ToNullRot<T>(this T trs)
            where T : struct, ITRS
        {
            trs.RotationNative = default;
            return trs;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidRot(this quaternion q) => !q.Equals(default) || math.any(math.isnan(q.value));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        
        public static bool IsValidRot(this Quaternion q) => ((quaternion) q).IsValidRot();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidRot<T>(this T trs)
            where T : struct, ITRS
        {
            return trs.RotationNative.IsValidRot();
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void ConditionalCheckRotationValid<T>(this T trs)
            where T : struct, ITRS
        {
            if (!trs.RotationNative.IsValidRot())
                throw new ArgumentException("TRS rotation is invalid");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Delta<T>(this T from, in T to)
            where T : struct, ITRS
        {
            from.ConditionalCheckRotationValid();
            to.ConditionalCheckRotationValid();
            var posDelta = to.PositionNative - from.PositionNative;
            var rotDelta = math.mul(math.inverse(from.RotationNative), to.RotationNative);
            var scaleDelta = to.ScaleNative - from.ScaleNative;
            return new T
            {
                PositionNative = posDelta,
                RotationNative = rotDelta,
                ScaleNative = scaleDelta
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Add<T>(this T a, in T b)
            where T : struct, ITRS
        {
            return new T
            {
                PositionNative = a.PositionNative + b.PositionNative,
                RotationNative = math.mul(a.RotationNative, b.RotationNative),
                ScaleNative = a.ScaleNative + b.ScaleNative
            };
        }
    }
}