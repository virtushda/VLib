using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace VLib
{
    /// <summary> A cheaper TRS struct with scale enforced at (1,1,1) </summary>
    [Serializable]
    public struct TranslationRotation : ITRS, IEquatable<TranslationRotation>
    {
        public Vector3 position;
        public Quaternion rotation;
        
        public TranslationRotation(Vector3 position, Quaternion rotation)
        {
            this.position = position;
            this.rotation = rotation;
            Scale = Vector3.one;
        }

        public TranslationRotation(float3 position, quaternion rotation)
        {
            this.position = UnsafeUtility.As<float3, Vector3>(ref position);
            this.rotation = UnsafeUtility.As<quaternion, Quaternion>(ref rotation);
            Scale = VMath.One3;
        }

        public TranslationRotation(float3 position)
        {
            this.position = position;
            this.rotation = default;
        }

        public TranslationRotation(Transform t) => this = t.GetTRS<TranslationRotation>();

        public TranslationRotation(TransformAccess t) => this = t.GetTRS<TranslationRotation>();
        
        /// <summary> constructor that generates a trs from a position and a direction </summary> 
        public TranslationRotation(Vector3 position, Vector3 direction)
        {
            this.position = position;
            this.rotation = Quaternion.LookRotation(direction, Vector3.up);
            Scale = Vector3.one;
        }
        /// <summary> constructor that generates a trs from a position and a direction </summary> 
        public TranslationRotation(float3 position, float3 direction)
        {
            this.position = position;
            rotation = quaternion.LookRotation(direction, Vector3.up);
            Scale = Vector3.one;
        }

        public TranslationRotation(Matrix4x4 matrix)
        {
            position = matrix.GetPosition();
            rotation = matrix.rotation;
        }

        public Vector3 Position
        {
            readonly get => position;
            set => position = value;
        }

        public Quaternion Rotation
        {
            readonly get => rotation;
            set => rotation = value;
        }

        public Vector3 Scale
        {
            readonly get => Vector3.one;
            set { }
        }

        public float3 PositionNative
        {
            get => UnsafeUtility.As<Vector3, float3>(ref position);
            set => position = UnsafeUtility.As<float3, Vector3>(ref value);
        }

        public quaternion RotationNative
        {
            get => UnsafeUtility.As<Quaternion, quaternion>(ref rotation);
            set => rotation = UnsafeUtility.As<quaternion, Quaternion>(ref value);
        }

        public float3 ScaleNative
        {
            get => VMath.One3;
            set { }
        }

        public void GetTransformed(in AffineTransform transform, out TranslationRotation posRotTransformed)
        {
            posRotTransformed = this;
            posRotTransformed.PositionNative = math.transform(transform, posRotTransformed.PositionNative);
            var transformRot = new quaternion(transform.rs);
            posRotTransformed.RotationNative = math.mul(transformRot, posRotTransformed.RotationNative);
        }

        public static TranslationRotation Lerp(in TranslationRotation a, in TranslationRotation b, float t)
        {
            return new TranslationRotation
            {
                PositionNative = math.lerp(a.PositionNative, b.PositionNative, t),
                RotationNative = math.slerp(a.RotationNative, b.RotationNative, t)
            };
        }
        
        public bool Equals(TranslationRotation other) => position.Equals(other.position) && rotation.Equals(other.rotation);
        public override bool Equals(object obj) => obj is TranslationRotation other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(position, rotation);
        public static bool operator ==(TranslationRotation left, TranslationRotation right) => left.Equals(right);
        public static bool operator !=(TranslationRotation left, TranslationRotation right) => !left.Equals(right);

        public static implicit operator TranslationRotation((float3 pos, quaternion rot) tr) => new (tr.pos, tr.rot);
        public static implicit operator TranslationRotation((float3 pos, quaternion? rot) tr) => new (tr.pos, tr.rot ?? default);
        public static explicit operator TranslationRotation(float3 pos) => new (pos);
        public static explicit operator TranslationRotation(Vector3 pos) => new (pos);
        public static implicit operator TranslationRotation(TranslationFacing tf) => new (tf.PositionNative, tf.RotationNative);
        public static implicit operator float3 (TranslationRotation tr) => tr.PositionNative;
        public static implicit operator quaternion (TranslationRotation tr) => tr.RotationNative;
    }
}