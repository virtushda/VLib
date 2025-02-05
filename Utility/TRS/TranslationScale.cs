using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace VLib
{
    /// <summary> A cheaper TRS struct without any rotation information. </summary>
    [Serializable]
    public struct TranslationScale : ITRS, IEquatable<TranslationScale>
    {
        public Vector3 position;
        public Vector3 scale;
        
        public static readonly TranslationScale zero = new TranslationScale(Vector3.zero, Vector3.zero);
        public static readonly TranslationScale identity = new TranslationScale(Vector3.zero, Vector3.one);

        public TranslationScale(Vector3 position, Vector3 scale)
        {
            this.position = position;
            this.scale = scale;
        }

        public TranslationScale(float3 position, float3 scale)
        {
            this.position = UnsafeUtility.As<float3, Vector3>(ref position);
            this.scale = UnsafeUtility.As<float3, Vector3>(ref scale);
        }

        public TranslationScale(float3 position)
        {
            this.position = position;
            this.scale = VMath.One3;
        }

        public TranslationScale(Transform t) => this = t.GetTRS<TranslationScale>();

        public TranslationScale(TransformAccess t) => this = t.GetTRS<TranslationScale>();

        public TranslationScale(Matrix4x4 matrix)
        {
            position = matrix.GetPosition();
            scale = matrix.lossyScale;
        }

        public Vector3 Position
        {
            readonly get => position;
            set => position = value;
        }

        public Quaternion Rotation
        {
            readonly get => Quaternion.identity;
            set { }
        }

        public Vector3 Scale
        {
            readonly get => scale;
            set => scale = value;
        }

        public float3 PositionNative
        {
            get => UnsafeUtility.As<Vector3, float3>(ref position);
            set => position = UnsafeUtility.As<float3, Vector3>(ref value);
        }

        public quaternion RotationNative
        {
            get => quaternion.identity;
            set { }
        }

        public float3 ScaleNative
        {
            get => UnsafeUtility.As<Vector3, float3>(ref scale);
            set => scale = UnsafeUtility.As<float3, Vector3>(ref value);
        }

        /*public void GetTransformed(in AffineTransform transform, out TranslationScale posRotTransformed)
        {
            posRotTransformed = this;
            posRotTransformed.PositionNative = math.transform(transform, posRotTransformed.PositionNative);
            var transformRot = new quaternion(transform.rs);
            posRotTransformed.RotationNative = math.mul(transformRot, posRotTransformed.RotationNative);
        }*/

        public static TranslationScale Lerp(in TranslationScale a, in TranslationScale b, float t)
        {
            return new TranslationScale
            {
                PositionNative = math.lerp(a.PositionNative, b.PositionNative, t),
                ScaleNative = math.lerp(a.ScaleNative, b.ScaleNative, t)
            };
        }
        
        public bool Equals(TranslationScale other) => position.Equals(other.position) && scale.Equals(other.scale);
        public override bool Equals(object obj) => obj is TranslationScale other && Equals(other);
        public override int GetHashCode() => position.GetHashCode() ^ scale.GetHashCode();
        public static bool operator ==(TranslationScale left, TranslationScale right) => left.Equals(right);
        public static bool operator !=(TranslationScale left, TranslationScale right) => !left.Equals(right);

        public static implicit operator TranslationScale((float3 pos, float3 scale) tr) => new (tr.pos, tr.scale);
        public static implicit operator TranslationScale((float3 pos, float3? scale) tr) => new (tr.pos, tr.scale ?? default);
        public static explicit operator TranslationScale(float3 pos) => new (pos);
        public static explicit operator TranslationScale(Vector3 pos) => new (pos);
        public static explicit operator float3 (TranslationScale tr) => tr.PositionNative;
    }
}