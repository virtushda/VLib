using System;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
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

    // DAMN, I realized AffineTransform exists already
    /*/// <summary> Stores a transformation matrix in 12 bytes instead of 16 </summary>
    [Serializable]
    public struct CompactMatrix : ITRS, IEquatable<CompactMatrix>
    {
        public float3 translation;
        public float3x3 rotScaleMatrix;

        public Vector3 Position
        {
            get => translation;
            set => translation = value;
        }
        /// <summary> NOT IMPLEMENTED! Use <see cref="rotScaleMatrix"/>. </summary>
        [Obsolete]
        public Quaternion Rotation
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
        /// <summary> NOT IMPLEMENTED! Use <see cref="rotScaleMatrix"/>. </summary>
        [Obsolete]
        public Vector3 Scale
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
        public float3 PositionNative
        {
            get => translation;
            set => translation = value;
        }
        /// <summary> NOT IMPLEMENTED! Use <see cref="rotScaleMatrix"/>. </summary>
        [Obsolete]
        public quaternion RotationNative
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
        /// <summary> NOT IMPLEMENTED! Use <see cref="rotScaleMatrix"/>. </summary>
        [Obsolete]
        public float3 ScaleNative
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public CompactMatrix(float3 position, float3x3 rotScaleMatrix)
        {
            this.translation = position;
            this.rotScaleMatrix = rotScaleMatrix;
        }
        
        public CompactMatrix(float3 position, quaternion rotation, float3 scale)
        {
            this.translation = position;
            this.rotScaleMatrix = new float3x3(rotation);
            rotScaleMatrix.c0 *= scale.x;
            rotScaleMatrix.c1 *= scale.y;
            rotScaleMatrix.c2 *= scale.z;
        }
        
        public CompactMatrix(in float4x4 matrix)
        {
            translation = matrix.c3.xyz;
            rotScaleMatrix = matrix.ToRotScale3X3();
        }
        
        public float4x4 ToFullMatrix() => new(rotScaleMatrix, translation);
        
        public static CompactMatrix Mul(CompactMatrix trs1, CompactMatrix trs2)
        {
            // Multiply the rotation-scale matrices
            float3x3 resultRotScale = math.mul(trs1.rotScaleMatrix, trs2.rotScaleMatrix);

            // Rotate and scale the translation of the second transformation
            float3 rotatedScaledTranslation2 = math.mul(trs1.rotScaleMatrix, trs2.translation);

            // Add the translations
            float3 resultTranslation = trs1.translation + rotatedScaledTranslation2;

            return new CompactMatrix(resultTranslation, resultRotScale);
        }
        
        public bool Equals(CompactMatrix other) => translation.Equals(other.translation) && rotScaleMatrix.Equals(other.rotScaleMatrix);
    }*/

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

        public Vector3 Position { get => position; set => position = value; }
        public Quaternion Rotation { get => rotation; set => rotation = value; }
        public Vector3 Scale { get => Vector3.one; set { } }
        public float3 PositionNative { get => UnsafeUtility.As<Vector3, float3>(ref position); set => position = UnsafeUtility.As<float3, Vector3>(ref value); }
        public quaternion RotationNative { get => UnsafeUtility.As<Quaternion, quaternion>(ref rotation); set => rotation = UnsafeUtility.As<quaternion, Quaternion>(ref value); }
        public float3 ScaleNative { get => VMath.One3; set { } }

        public bool Equals(TranslationRotation other) => position.Equals(other.position) && rotation.Equals(other.rotation);
        public override bool Equals(object obj) => obj is TranslationRotation other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(position, rotation);
        public static bool operator ==(TranslationRotation left, TranslationRotation right) => left.Equals(right);
        public static bool operator !=(TranslationRotation left, TranslationRotation right) => !left.Equals(right);

        public static implicit operator TranslationRotation((float3 pos, quaternion rot) tr) => new (tr.pos, tr.rot);
        public static implicit operator TranslationRotation((float3 pos, quaternion? rot) tr) => new (tr.pos, tr.rot ?? default);
        public static implicit operator TranslationRotation(float3 pos) => new (pos);
        public static implicit operator TranslationRotation(TranslationFacing tf) => new (tf.PositionNative, tf.RotationNative);
    }

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
        public static bool IsValidRot(this quaternion q) => !q.Equals(default);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        
        public static bool IsValidRot(this Quaternion q) => !q.Equals(default);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidRot<T>(this T trs)
            where T : struct, ITRS
        {
            return trs.RotationNative.IsValidRot();
        }
    }
}