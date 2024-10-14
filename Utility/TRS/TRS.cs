using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace VLib
{
    public struct TRS : ITRS, IEquatable<TRS>
    {
        public static readonly TRS identity = new TRS(float3.zero, quaternion.identity, VMath.One3);

        public static implicit operator float4x4(TRS trs) => float4x4.TRS(trs.PositionNative, trs.RotationNative, trs.ScaleNative);
        public static implicit operator TRS(float4x4 matrix) => matrix.ToTRS();
        //public static implicit operator Matrix4x4(TRS trs) => float4x4.TRS(trs.position, trs.rotation, trs.scale);
        public static implicit operator TranslationRotation(TRS trs) => new(trs.position, trs.rotation);
        public static explicit operator TRS(TranslationRotation tr) => new(tr.position, tr.rotation, Vector3.one); 

        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        public TRS(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }

        public TRS(float3 position, quaternion rotation, float3 scale)
        {
            this.position = UnsafeUtility.As<float3, Vector3>(ref position);
            this.rotation = UnsafeUtility.As<quaternion, Quaternion>(ref rotation);
            this.scale = UnsafeUtility.As<float3, Vector3>(ref scale);
        }

        public TRS(Transform t) => this = t.GetTRS<TRS>();

        public TRS(TransformAccess t) => this = t.GetTRS<TRS>();
        
        /// <summary> constructor that generates a trs from a position and a direction </summary> 
        public TRS(Vector3 position, Vector3 direction)
        {
            this.position = position;
            this.rotation = Quaternion.LookRotation(direction, Vector3.up);
            this.scale = Vector3.one;
        }

        public Vector3 Position
        {
            get => position;
            set => position = value;
        }

        public Quaternion Rotation
        {
            get => rotation;
            set => rotation = value;
        }

        public Vector3 Scale
        {
            get => scale;
            set => scale = value;
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
            get => UnsafeUtility.As<Vector3, float3>(ref scale);
            set => scale = UnsafeUtility.As<float3, Vector3>(ref value);
        }

        /// <summary> Is non-NAN and has a valid rotation. </summary>
        public bool IsValid()
        {
            bool isInvalidInAnyWay = math.any(math.isnan(PositionNative)) || 
                                     math.any(math.isnan(RotationNative.value)) || 
                                     math.any(math.isnan(ScaleNative)) ||
                                     !RotationNative.IsValidRot();
            return !isInvalidInAnyWay;
        }
        
        public AffineTransform ToAffine() => new(PositionNative, RotationNative, ScaleNative);

        public bool Equals(TRS other) => position.Equals(other.position) && rotation.Equals(other.rotation) && scale.Equals(other.scale);

        public override bool Equals(object obj)
        {
            return obj is TRS other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(position, rotation, scale);
        }

        public static bool operator ==(TRS left, TRS right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TRS left, TRS right)
        {
            return !left.Equals(right);
        }
    }
}