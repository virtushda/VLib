using System;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace VLib
{
    public struct TranslationFacing : ITRS, IEquatable<TranslationFacing>
    {
        public Vector3 position;
        public float facing;
        
        public TranslationFacing(Vector3 position, float facing)
        {
            this.position = position;
            this.facing = facing;
        }
        
        public TranslationFacing(float3 position, float facing)
        {
            this.position = position;
            this.facing = facing;
        }
        
        public TranslationFacing(Vector3 position, Quaternion rotation)
        {
            this.position = position;
            facing = ToFacing(rotation);
        }

        public TranslationFacing(float3 position, quaternion rotation)
        {
            this.position = UnsafeUtility.As<float3, Vector3>(ref position);
            facing = ToFacing(rotation);
        }

        public TranslationFacing(float3 position)
        {
            this.position = position;
            facing = default;
        }

        public TranslationFacing(Transform t) => this = t.GetTRS<TranslationFacing>();

        public TranslationFacing(TransformAccess t) => this = t.GetTRS<TranslationFacing>();
        
        /// <summary> constructor that generates a trs from a position and a direction </summary> 
        public TranslationFacing(Vector3 position, Vector3 direction)
        {
            this.position = position;
            facing = ToFacing(direction);
        }
        /// <summary> constructor that generates a trs from a position and a direction </summary> 
        public TranslationFacing(float3 position, float3 direction)
        {
            this.position = position;
            facing = ToFacing(direction);
        }

        public TranslationFacing(Matrix4x4 matrix)
        {
            position = matrix.GetPosition();
            facing = ToFacing(matrix.rotation);
        }
        
        public TranslationFacing(TranslationRotation tr, float turn = 0f)
        {
            position = tr.position;
            facing = ToFacing(tr.rotation) + turn;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion ToRotation(float facing) => quaternion.RotateY(facing);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ToDirection(float facing) => math.rotate(quaternion.RotateY(facing), VMath.Forward3);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToFacing(quaternion rotation) => ToFacing(math.rotate(rotation, VMath.Forward3));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToFacing(float3 forward) => forward is { x: 0f, z: 0f } ? 0f : math.atan2(forward.x, forward.z);

        public Vector3 Position { get => position; set => position = value; }
        public Quaternion Rotation { get => RotationNative; set => RotationNative = value; }
        public Vector3 Scale { get => Vector3.one; set { } }
        public float3 PositionNative { get => UnsafeUtility.As<Vector3, float3>(ref position); set => position = UnsafeUtility.As<float3, Vector3>(ref value); }
        public quaternion RotationNative { get => ToRotation(facing); set => facing = ToFacing(value); }
        
        public float3 ForwardNative => new float3(math.sin(facing), 0f, math.cos(facing));
        public float3 RightNative => new float3(math.cos(facing), 0f, -math.sin(facing));

        public float3 ScaleNative { get => VMath.One3; set { } }

        public static TranslationFacing Lerp(TranslationFacing tr0, TranslationFacing tr1, float t)
        {
            return new TranslationFacing(math.lerp(tr0.PositionNative, tr1.PositionNative, t), math.lerp(tr0.Forward(), tr1.Forward(), t));
        }
        
        public bool Equals(TranslationFacing other) => position.Equals(other.position) && facing.Equals(other.facing);
        public override bool Equals(object obj) => obj is TranslationFacing other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(position, facing);
        public static bool operator ==(TranslationFacing left, TranslationFacing right) => left.Equals(right);
        public static bool operator !=(TranslationFacing left, TranslationFacing right) => !left.Equals(right);
        
        public static TranslationFacing operator +(TranslationFacing tr, float facing) => new(tr.position, tr.facing + facing);
        public static TranslationFacing operator -(TranslationFacing tr, float facing) => new(tr.position, tr.facing - facing);
        public static TranslationFacing operator +(TranslationFacing tr, float3 pos) => new(tr.PositionNative + pos, tr.facing);
        public static TranslationFacing operator -(TranslationFacing tr, float3 pos) => new(tr.PositionNative - pos, tr.facing);
        public static TranslationFacing operator +(TranslationFacing tr, Vector3 pos) => new(tr.position + pos, tr.facing);
        public static TranslationFacing operator -(TranslationFacing tr, Vector3 pos) => new(tr.position - pos, tr.facing);
        
        public static implicit operator TranslationFacing(TranslationRotation tr) => new(tr);
        public static implicit operator TranslationFacing((float3 pos, quaternion rot) tr) => new(tr.pos, tr.rot);
        public static implicit operator TranslationFacing((float3 pos, quaternion? rot) tr) => new(tr.pos, tr.rot ?? default);
        public static implicit operator TranslationFacing(float3 pos) => new(pos);
        public static implicit operator TranslationFacing(Vector3 pos) => new(pos);
    }
}