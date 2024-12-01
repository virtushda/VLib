using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace VLib
{
    [Serializable]
    public struct PointForward : ITRS
    {
        float3 position;
        float3 forward;

        public float3 PositionNative
        {
            get => position;
            set => position = value;
        }
        public quaternion RotationNative
        {
            get => quaternion.LookRotation(forward, Vector3.up);
            set => forward = math.mul(value, VMath.Forward3);
        }

        public float3 ScaleNative
        {
            get => 1f;
            set => value = value;
        }
        
        public float3 Position
        {
            get => position;
            set => position = value;
        }

        public float3 Forward
        {
            get => forward;
            set => forward = value;
        }
        
        Vector3 ITRS.Position
        {
            get => UnsafeUtility.As<float3, Vector3>(ref position);
            set => position = UnsafeUtility.As<Vector3, float3>(ref value);
        }

        Quaternion ITRS.Rotation
        {
            get => quaternion.LookRotation(forward, Vector3.up);
            set => forward = value * Vector3.forward;
        }
        Vector3 ITRS.Scale
        {
            get => Vector3.one;
            set => value = value;
        }
        
        public PointForward(float3 position, float3 forward)
        {
            this.position = position;
            this.forward = forward;
        }
        
        public static implicit operator float3(PointForward point) => point.position;
        public static implicit operator PointForward(float3 point) => new(point, default);
    }
}