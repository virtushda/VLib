using System;
using System.Runtime.CompilerServices;
using Drawing;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;
using float4x4 = Unity.Mathematics.float4x4;

namespace VLib
{
    public static class MatrixExt
    {
        /// <summary> Direction </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Right(in this float4x4 matrix) => normalizesafe(matrix.RightRaw());
        
        /// <summary> Direction </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Right(in this AffineTransform matrix) => normalizesafe(matrix.RightRaw());

        /// <summary> Direction </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Up(in this float4x4 matrix) => normalizesafe(matrix.UpRaw());
        
        /// <summary> Direction </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Up(in this AffineTransform matrix) => normalizesafe(matrix.UpRaw());

        /// <summary> Direction </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Forward(in this float4x4 matrix) => normalizesafe(matrix.ForwardRaw());
        
        /// <summary> Direction </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Forward(in this AffineTransform matrix) => normalizesafe(matrix.ForwardRaw());

        /// <summary> Not a 'direction', is UNNORMALIZED. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 RightRaw(in this float4x4 matrix) => matrix.c0.xyz;
        
        /// <summary> Not a 'direction', is UNNORMALIZED. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 RightRaw(in this AffineTransform matrix) => matrix.rs.c0;

        /// <summary> Not a 'direction', is UNNORMALIZED. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 UpRaw(in this float4x4 matrix) => matrix.c1.xyz;
        
        /// <summary> Not a 'direction', is UNNORMALIZED. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 UpRaw(in this AffineTransform matrix) => matrix.rs.c1;

        /// <summary> Not a 'direction', is UNNORMALIZED. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ForwardRaw(in this float4x4 matrix) => matrix.c2.xyz;
        
        /// <summary> Not a 'direction', is UNNORMALIZED. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ForwardRaw(in this AffineTransform matrix) => matrix.rs.c2;
        
        public static bool ApproxEquals(in this float4x4 matrix, in float4x4 other, float epsilon = 0.0001f)
        {
            for (int i = 0; i < 4; i++)
            {
                float4 dif = abs(matrix[i] - other[i]);
                if (cmax(dif) > epsilon)
                    return false;
            }

            return true;
        }

        /// <summary> This is not the correct way to 'lerp' a transformation matrix. Only valid for float4x4 storing arbitrary values. </summary>
        public static float4x4 LerpDirectNonTransform(in this float4x4 matrixA, in float4x4 matrixB, float lerpValue)
        {
            return new float4x4(lerp(matrixA.c0, matrixB.c0, lerpValue),
                                lerp(matrixA.c1, matrixB.c1, lerpValue),
                                lerp(matrixA.c2, matrixB.c2, lerpValue),
                                lerp(matrixA.c3, matrixB.c3, lerpValue));
        }
        
        public static float4x4 LerpTransform(in this float4x4 matrixA, in float4x4 matrixB, float lerpValue)
        {
            matrixA.Decompose(out var posA, out var rotA, out var scaleA);
            matrixB.Decompose(out var posB, out var rotB, out var scaleB);
            return float4x4.TRS(lerp(posA, posB, lerpValue), slerp(rotA, rotB, lerpValue), lerp(scaleA, scaleB, lerpValue));
        }

        public static void SetTRS(ref this float4x4 matrix, in float3 translation, in quaternion rotation, in float3 scale) => matrix = float4x4.TRS(translation, rotation, scale);

        public static TRS ToTRS(in this float4x4 matrix)
        {
            matrix.Decompose(out var p, out var r, out var s);
            return new TRS(p,r,s);
        }

        public static T ToTRS<T>(in this float4x4 matrix)
            where T : unmanaged, ITRS
        {
            matrix.Decompose(out var p, out var r, out var s);
            return new T
                   {
                       PositionNative = p,
                       RotationNative = r,
                       ScaleNative = s
                   };
        }

        /// <summary>
        /// Decomposes a 4x4 TRS matrix into separate transforms (translation * rotation * scale)
        /// Matrix may not contain skew
        /// </summary>
        /// <param name="m">Matrix</param>
        /// <param name="translation">Translation</param>
        /// <param name="rotation">Rotation</param>
        /// <param name="scale">Scale</param>
        public static void Decompose(
            in this Matrix4x4 m,
            out Vector3 translation,
            out Quaternion rotation,
            out Vector3 scale
        )
        {
            translation = new Vector3(m.m03, m.m13, m.m23);
            var mRotScale = m.ToRotScale3X3();
            mRotScale.DecomposeRotScaleRaw(out float4 mRotation, out float3 mScale);
            rotation = new Quaternion(mRotation.x, mRotation.y, mRotation.z, mRotation.w);
            scale = new Vector3(mScale.x, mScale.y, mScale.z);
        }

        /// <summary>
        /// Decomposes a 4x4 TRS matrix into separate transforms (translation * rotation * scale)
        /// Matrix may not contain skew
        /// </summary>
        /// <param name="translation">Translation</param>
        /// <param name="rotation">Rotation</param>
        /// <param name="scale">Scale</param>
        public static void Decompose(
            in this float4x4 m,
            out float3 translation,
            out quaternion rotation,
            out float3 scale
        )
        {
            // Leverage affine transform
            var affine = new AffineTransform(m);
            decompose(affine, out translation, out rotation, out scale);
            
            /*var mRotScale = m.ToRotScale3X3();
            mRotScale.DecomposeRotScale(out rotation, out scale);
            translation = m.c3.xyz;*/
        }
        
        /// <summary>
        /// Decomposes a 4x4 TRS matrix into separate transforms (translation * rotation * scale)
        /// Matrix may not contain skew
        /// </summary>
        /// <param name="translation">Translation</param>
        /// <param name="rotation">Rotation</param>
        /// <param name="scale">Scale</param>
        public static void DecomposeToLegacy(
            in this float4x4 m,
            out Vector3 translation,
            out Quaternion rotation,
            out Vector3 scale
        )
        {
            // Leverage affine transform
            var affine = new AffineTransform(m);
            decompose(affine, out var translationMathLib, out var rotationMathLib, out var scaleMathLib);

            // Switch types
            translation = UnsafeUtility.As<float3, Vector3>(ref translationMathLib);
            rotation = UnsafeUtility.As<quaternion, Quaternion>(ref rotationMathLib);
            scale = UnsafeUtility.As<float3, Vector3>(ref scaleMathLib);
        }

        /*/// <summary>
        /// Decomposes a 4x4 TRS matrix into separate transforms (translation * rotation * scale)
        /// Matrix may not contain skew
        /// </summary>
        /// <param name="translation">Translation</param>
        /// <param name="rotation">Rotation</param>
        /// <param name="scale">Scale</param>
        public static void DecomposeRaw(
            in this float4x4 m,
            out float3 translation,
            out float4 rotation,
            out float3 scale
        )
        {
            var mRotScale = ToRotScale3X3(m);
            mRotScale.DecomposeRotScaleRaw(out rotation, out scale);
            translation = m.c3.xyz;
        }*/

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3x3 ToRotScale3X3(in this float4x4 m) => float3x3(m);
        
        public static float3x3 ToRotScale3X3(in this Matrix4x4 m) =>
            new float3x3(m.m00, m.m01, m.m02,
                         m.m10, m.m11, m.m12,
                         m.m20, m.m21, m.m22);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 GetPositionDelta(in this float4x4 m) => m.c3.xyz;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetPositionDelta(ref this float4x4 m, float3 position) => m.c3.xyz = position;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion RotationDelta(in this float4x4 m)
        {
            m.Decompose(out _, out var rotation, out _);
            return rotation;
        }

        /*/// <summary>
        /// Decomposes a 3x3 matrix into rotation and scale
        /// </summary>
        /// <param name="rotation">Rotation quaternion values</param>
        /// <param name="scale">Scale</param>
        public static void DecomposeRotScale(in this float3x3 m, out quaternion rotation, out float3 scale)
        {
            float3x3 rotationMatrix;
            scale.x = normalize(m.c0, out rotationMatrix.c0);
            scale.y = normalize(m.c1, out rotationMatrix.c1);
            scale.z = normalize(m.c2, out rotationMatrix.c2);

            if (rotationMatrix.IsNegative())
            {
                rotationMatrix *= -1f;
                scale *= -1f;
            }

            normalize(rotationMatrix);
            rotation = new quaternion(rotationMatrix);
        }*/

        /// <summary>
        /// Decomposes a 3x3 matrix into rotation and scale
        /// </summary>
        /// <param name="rotation">Rotation quaternion values</param>
        /// <param name="scale">Scale</param>
        public static void DecomposeRotScaleRaw(in this float3x3 m, out float4 rotation, out float3 scale)
        {
            float3x3 rotationMatrix;
            scale.x = normalize(m.c0, out rotationMatrix.c0);
            scale.y = normalize(m.c1, out rotationMatrix.c1);
            scale.z = normalize(m.c2, out rotationMatrix.c2);

            if (rotationMatrix.IsNegative())
            {
                rotationMatrix *= -1f;
                scale *= -1f;
            }

            normalize(rotationMatrix);
            rotation = new quaternion(rotationMatrix).value;
        }

        public static float3 ScaleDelta(in this float4x4 m)
        {
            m.ScaleDelta(out var scale);
            return scale;
        }

        /// <summary>Extract scale from a 4x4 matrix</summary>
        /// <param name="scale">Scale</param>
        public static void ScaleDelta(in this float4x4 m, out float3 scale)
        {
            float3x3 rotationMatrix;
            scale.x = normalize(m.c0.xyz, out rotationMatrix.c0);
            scale.y = normalize(m.c1.xyz, out rotationMatrix.c1);
            scale.z = normalize(m.c2.xyz, out rotationMatrix.c2);

            if (rotationMatrix.IsNegative())
                scale *= -1f;
        }

        /// <summary>Extract scale from a 4x4 matrix</summary>
        /// <param name="scale">Scale</param>
        public static void ScaleDeltaSqr(in this float4x4 m, out float3 scaleSqr)
        {
            float3x3 rotationMatrix = m.ToRotScale3X3();
            scaleSqr.x = lengthsq(rotationMatrix.c0);
            scaleSqr.y = lengthsq(rotationMatrix.c1);
            scaleSqr.z = lengthsq(rotationMatrix.c2);

            if (rotationMatrix.IsNegative())
                scaleSqr *= -1f;
        }
        
        //

        static float normalize(in float3 input, out float3 output)
        {
            float len = length(input);
            output = input / len;
            return len;
        }

        static void normalize(in float3x3 m)
        {
            math.normalize(m.c0);
            math.normalize(m.c1);
            math.normalize(m.c2);
        }

        public static bool IsNegative(in this float3x3 m)
        {
            var cross = math.cross(m.c0, m.c1);
            return dot(cross, m.c2) < 0f;
        }

        public static void PopulateWithArray(ref this float4x4 m, float[] array)
        {
            for (int i = 0; i < 4; i++)
            {
                var vec4 = m[i];

                var offset = i * 4;
                for (int j = 0; j < 4; j++)
                {
                    var arrayIdx = j + offset;
                    if (arrayIdx < array.Length)
                        vec4[j] = array[arrayIdx];
                    else
                    {
                        m[i] = vec4;
                        return;
                    }
                }

                m[i] = vec4;
            }
        }

        public static void PopulateWithSpan(ref this float4x4 m, ref Span<float> span)
        {
            for (int i = 0; i < 4; i++)
            {
                var vec4 = m[i];

                var offset = i * 4;
                for (int j = 0; j < 4; j++)
                {
                    var arrayIdx = j + offset;
                    if (arrayIdx < span.Length)
                        vec4[j] = span[arrayIdx];
                    else
                    {
                        m[i] = vec4;
                        return;
                    }
                }

                m[i] = vec4;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNan(in this float4x4 matrix) => 
            any(new bool4(
            any(isnan(matrix.c0)), 
            any(isnan(matrix.c1)), 
            any(isnan(matrix.c2)), 
            any(isnan(matrix.c3))));

        /// <summary> Returns true if was able to draw. False if matrix contains NANs. </summary>
        public static bool DrawAline(in this float4x4 matrix, CommandBuilder aline, bool normalized = true, float scale = 1)
        {
            if (matrix.IsNan())
                return false;
           
            var vectorX = normalized ? math.normalize(matrix.c0.xyz) : matrix.c0.xyz;
            var vectorY = normalized ? math.normalize(matrix.c1.xyz) : matrix.c1.xyz;
            var vectorZ = normalized ? math.normalize(matrix.c2.xyz) : matrix.c2.xyz;
            
            aline.Line(matrix.c3.xyz, matrix.c3.xyz + vectorX * scale, Color.red);
            aline.Line(matrix.c3.xyz, matrix.c3.xyz + vectorY * scale, Color.green);
            aline.Line(matrix.c3.xyz, matrix.c3.xyz + vectorZ * scale, Color.blue);
            return true;
        }
    }
}