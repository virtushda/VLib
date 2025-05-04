using System;
using System.Diagnostics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace VLib
{
    /// <summary> Represents a 3D position and a normalized direction in space. </summary>
    [Serializable]
    public struct TranslationDirection : ITRS, IEquatable<TranslationDirection>
    {
        public Vector3 position;
        public Vector3 direction;
        
        public TranslationDirection(Vector3 position, Vector3 direction)
        {
            this.position = position;
            this.direction = direction;
            CheckNormalized();
        }

        public TranslationDirection(float3 position, float3 direction)
        {
            this.position = UnsafeUtility.As<float3, Vector3>(ref position);
            this.direction = UnsafeUtility.As<float3, Vector3>(ref direction);
            CheckNormalized();
        }

        public TranslationDirection(float3 position)
        {
            this.position = position;
            this.direction = Vector3.forward;
        }

        public TranslationDirection(Transform t)
        {
            this = t.GetTRS<TranslationDirection>();
            CheckNormalized();
        }

        public TranslationDirection(TransformAccess t)
        {
            this = t.GetTRS<TranslationDirection>();
            CheckNormalized();
        }

        public TranslationDirection(ref Matrix4x4 matrix)
        {
            position = matrix.GetPosition();
            direction = matrix.GetColumn(2);
            CheckNormalized();
        }

        [Conditional("UNITY_ASSERTIONS"), Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void CheckNormalized() => BurstAssert.True(math.abs(direction.magnitude - 1f) < 0.001f); // Direction must be normalized

        public Vector3 Position
        {
            readonly get => position;
            set => position = value;
        }
        
        public Vector3 Direction
        {
            readonly get => direction;
            set
            {
                direction = value;
                CheckNormalized();
            }
        }

        public Quaternion Rotation
        {
            readonly get => Quaternion.LookRotation(direction, Vector3.up);
            set => Direction = value * Vector3.forward;
        }

        /// <summary> Scale is always 1 on this type. </summary>
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
            get => quaternion.LookRotation(DirectionNative, math.up());
            set => direction = math.rotate(value, math.forward());
        }

        public float3 DirectionNative
        {
            get => UnsafeUtility.As<Vector3, float3>(ref direction);
            set
            {
                direction = UnsafeUtility.As<float3, Vector3>(ref value);
                CheckNormalized();
            }
        }

        public float3 ScaleNative
        {
            get => VMath.One3;
            set { }
        }

        /// <summary> Transforms a direction from local space to world space using the rotation. </summary>
        /// <param name="localDirection">The direction vector in local space.</param>
        /// <returns>The direction vector transformed to world space.</returns>
        public Vector3 TransformDirection(Vector3 localDirection) => Rotation * localDirection;

        public void GetTransformed(in AffineTransform transform, out TranslationDirection posDirectionTransformed)
        {
            posDirectionTransformed = this;
            posDirectionTransformed.PositionNative = math.transform(transform, posDirectionTransformed.PositionNative);
            var transformRot = new quaternion(transform.rs);
            posDirectionTransformed.DirectionNative = math.rotate(transformRot, posDirectionTransformed.DirectionNative);
        }

        public static TranslationDirection Lerp(in TranslationDirection a, in TranslationDirection b, float t)
        {
            return new TranslationDirection
            {
                PositionNative = math.lerp(a.PositionNative, b.PositionNative, t),
                direction = math.normalizesafe(math.lerp(a.DirectionNative, b.DirectionNative, t))
            };
        }
        
        public bool Equals(TranslationDirection other) => position.Equals(other.position) && direction.Equals(other.direction);
        public override bool Equals(object obj) => obj is TranslationDirection other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(position, direction);
        public static bool operator ==(TranslationDirection left, TranslationDirection right) => left.Equals(right);
        public static bool operator !=(TranslationDirection left, TranslationDirection right) => !left.Equals(right);

        public static explicit operator TranslationDirection((float3 pos, float3 dir) td) => new (td.pos, td.dir);
        public static explicit operator TranslationDirection(float3 pos) => new (pos);
        public static explicit operator TranslationDirection(Vector3 pos) => new (pos);
        public static explicit operator TranslationDirection(TranslationRotation tr) => new (tr.PositionNative, math.rotate(tr.RotationNative, math.forward()));
        public static explicit operator TranslationDirection(TranslationFacing tf) => new (tf.PositionNative, tf.ForwardNative);
    }
}