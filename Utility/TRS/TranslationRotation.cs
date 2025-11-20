using System;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace VLib
{
    /// <summary> A cheaper TRS struct with scale enforced at (1,1,1) </summary>
    [Serializable, StructLayout(LayoutKind.Explicit, Size = 28)]
    public struct TranslationRotation : ITRS, IEquatable<TranslationRotation>
    {
        [FieldOffset(0)]
        public Vector3 position;
        [FieldOffset(0)]
        public float3 positionNative;
        
        [FieldOffset(12)]
        public Quaternion rotation;
        [FieldOffset(12)]
        public quaternion rotationNative;
        
        public TranslationRotation(Vector3 position, Quaternion rotation)
        {
            // Use object initializer to avoid duplicate field writes
            this = new TranslationRotation
            {
                position = position,
                rotation = rotation,
                Scale = Vector3.one
            };
        }

        public TranslationRotation(float3 position, quaternion rotation)
        {
            // Use object initializer to avoid duplicate field writes
            this = new TranslationRotation
            {
                positionNative = position,
                rotationNative = rotation,
                Scale = VMath.One3
            };
        }

        public TranslationRotation(float3 position)
        {
            // Use object initializer to avoid duplicate field writes
            this = new TranslationRotation
            {
                positionNative = position,
                rotationNative = default,
                Scale = VMath.One3
            };
        }

        public TranslationRotation(Transform t) => this = t.GetTRS<TranslationRotation>();

        public TranslationRotation(TransformAccess t) => this = t.GetTRS<TranslationRotation>();
        
        /// <summary> constructor that generates a trs from a position and a direction </summary> 
        public TranslationRotation(Vector3 position, Vector3 direction)
        {
            // Use object initializer to avoid duplicate field writes
            this = new TranslationRotation
            {
                position = position,
                rotation = VMath.LookRotationAdaptive(direction), //Quaternion.LookRotation(direction, Vector3.up),
                Scale = Vector3.one
            };
        }
        /// <summary> constructor that generates a trs from a position and a direction </summary> 
        public TranslationRotation(float3 position, float3 direction)
        {
            // Use object initializer to avoid duplicate field writes
            this = new TranslationRotation
            {
                positionNative = position,
                rotationNative = VMath.LookRotationAdaptive(direction), //quaternion.LookRotation(direction, math.up()),
                Scale = VMath.One3
            };
        }

        public TranslationRotation(Matrix4x4 matrix)
        {
            // Use object initializer to avoid duplicate field writes
            this = new TranslationRotation
            {
                position = matrix.GetPosition(),
                rotation = matrix.rotation,
                Scale = Vector3.one
            };
        }
        
        public TranslationRotation(in float4x4 matrix)
        {
            // Use object initializer to avoid duplicate field writes
            this = new TranslationRotation
            {
                positionNative = matrix.GetPositionDelta(),
                rotationNative = matrix.RotationDelta(),
                Scale = VMath.One3
            };
        }
        
        public TranslationRotation(in TRS trs)
        {
            // Use object initializer to avoid duplicate field writes
            this = new TranslationRotation
            {
                position = trs.position,
                rotation = trs.rotation,
                Scale = Vector3.one
            };
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
            readonly get => positionNative;
            set => positionNative = value;
        }

        public quaternion RotationNative
        {
            readonly get => rotationNative;
            set => rotationNative = value;
        }

        public float3 ScaleNative
        {
            get => VMath.One3;
            set { }
        }

        public readonly void GetTransformed(in AffineTransform transform, out TranslationRotation posRotTransformed)
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

        public readonly float3 TransformPoint(float3 point)
        {
            // Rotate
            point = math.rotate(rotationNative, point);
            // Translate
            return point + positionNative;
        }
        
        public float3 InverseTransformPoint(float3 point)
        {
            // Untranslate
            point -= positionNative;
            // Unrotate
            return math.rotate(math.inverse(rotationNative), point);
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
        
        [BurstDiscard]
        public override string ToString()
        {
            var p = position;
            var r = rotation.eulerAngles;
            return $"Pos[{p.x:0.##}, {p.y:0.##}, {p.z:0.##}], Rot[{r.x:0.##}, {r.y:0.##}, {r.z:0.##}]";
        }
    }
}