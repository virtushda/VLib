using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

using float2 = Unity.Mathematics.float2;
using float3 = Unity.Mathematics.float3;
using quaternion = Unity.Mathematics.quaternion;

namespace VLib
{
    [GenerateTestsForBurstCompatibility]
    public static class VMath
    {
        #region Defaults

        public static readonly float3 One3 = 1;
        public static readonly float3 Right3 = Vector3.right;
        public static readonly float3 Up3 = Vector3.up;
        public static readonly float3 Forward3 = Vector3.forward;

        #endregion
        
        /// <summary>Gets the sign of a given value, with zero counted as positive.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SignBinary(int value) => value >= 0 ? 1 : -1;

        /// <summary>Gets the sign of a given value, with zero counted as positive.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SignBinary(float value) => value >= 0 ? 1 : -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PassThresholdDirection(float from, float to, float thres)
        {
            int fromSign = SignBinary(from - thres);
            int toSign = SignBinary(to - thres);

            if (fromSign == toSign)
                return 0;
            return toSign > fromSign ? 1 : -1;
        }

        /// <summary>Returns 'value' matching the sign of the 'reference'.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MatchSign(int value, int reference) => SignBinary(value) == SignBinary(reference) ? value : -value;

        /// <summary>Returns 'value' matching the sign of the 'reference'.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float MatchSign(float value, float reference) => SignBinary(value) == SignBinary(reference) ? value : -value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SetDistanceFromZero(float value, float desiredDistFromZero)
        {
            desiredDistFromZero = max(0.0001f, desiredDistFromZero);
            var absValue = abs(value);
            value *= desiredDistFromZero / absValue;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (int2x4 points_BL_BR_TL_TR, float2 weightsXY) ComputeBilinearSampleData(float2 coord)
        {
            int2 bl = (int2)coord;
            int2 tr = (int2)ceil(coord);
            int2 br = new int2(tr.x, bl.y);
            int2 tl = new int2(bl.x, tr.y);

            float2 weights = coord - bl;
            return (new int2x4(bl, br, tl, tr), weights);
        }

        /// <summary> Clamped version </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (int2x4 points_BL_BR_TL_TR, float2 weightsXY) ComputeBilinearSampleData(float2 coord, int2 minValue, int2 maxValue)
        {
            int2 bl = (int2)coord;
            int2 tr = (int2)ceil(coord);

            bl = clamp(bl, minValue, maxValue);
            tr = clamp(tr, minValue, maxValue);

            int2 br = new int2(tr.x, bl.y);
            int2 tl = new int2(bl.x, tr.y);

            float2 weights = coord - bl;
            return (new int2x4(bl, br, tl, tr), weights);
        }

        /// <summary> Bilinear Interpolation </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static U Blerp<T, U>(T c00, T c10, T c01, T c11, float tx, float ty)
            where T : ILerpable<U>
        {
            return c00.Lerp(c00.Lerp(c00.Value, c10.Value, tx),
                            c00.Lerp(c01.Value, c11.Value, tx), ty);
        }
        
        /// <summary> Bilinear Interpolation for floats </summary>
        public static float BlerpF(float c00, float c10, float c01, float c11, float tx, float ty)
        {
            return lerp(lerp(c00, c10, tx),
                        lerp(c01, c11, tx), ty);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 Rotate90CW(float2 input)
        {
            input.x *= -1;
            input.xy = input.yx;
            return input;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 Rotate90CCW(float2 input)
        {
            input.xy = input.yx;
            input.x *= -1;
            return input;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Rotate90CW(this float3 input)
        {
            input.x *= -1;
            input.xz = input.zx;
            return input;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Rotate90CCW(this float3 input)
        {
            input.xz = input.zx;
            input.x *= -1;
            return input;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion FromToRotation(float3 from, float3 to) =>
            quaternion.AxisAngle(
                                 angle: acos(
                                                  clamp(
                                                             dot(
                                                                      normalize(from),
                                                                      normalize(to)
                                                                     ),
                                                             -1f,
                                                             1f)
                                                 ),
                                 axis: normalize(cross(from, to))
                                );

        /// <summary> FromToRotation without normalizing input vectors </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion FromToRotationFast(float3 fromNormalized, float3 toNormalized) =>
            quaternion.AxisAngle(
                                 angle: acos(
                                                  clamp(
                                                             dot(
                                                                      fromNormalized,
                                                                      toNormalized
                                                                     ),
                                                             -1f,
                                                             1f)
                                                 ),
                                 axis: normalize(cross(fromNormalized, toNormalized))
                                );
        
        /// <summary>Copied from Vector2.Angle. Returns value in degrees.</summary>
        [MethodImpl((MethodImplOptions) 256)]
        public static float Angle(float2 from, float2 to)
        {
            float num = (float) (sqrt((double) lengthsq(from)) * lengthsq(to));
            return num < 1.00000000362749E-15 ? 0.0f : (float) acos((double) clamp(dot(from, to) / num, -1f, 1f)) * 57.29578f;
        }

        /// <summary>Copied from Vector2.SignedAngle. Returns value in degrees.</summary>
        [MethodImpl((MethodImplOptions) 256)]
        public static float SignedAngle(float2 from, float2 to)
        {
            double2 fromDouble = from;
            double2 toDouble = to;
            return Angle(from, to) * sign((float) (fromDouble.x * toDouble.y - fromDouble.y * toDouble.x));
        }
        
        /// <summary>Copied from Vector3.Angle. Returns value in degrees.</summary>
        [MethodImpl((MethodImplOptions) 256)]
        public static float Angle(float3 from, float3 to)
        {
            float num = (float) sqrt(lengthsq(from) * (double) lengthsq(to));
            return num < 1.0000000036274937E-15 ? 0.0f : (float) acos((double) clamp(dot(from, to) / num, -1f, 1f)) * 57.29578f;
        }

        /// <summary>Copied from Vector3.SignedAngle. Returns value in degrees.</summary>
        /// <param name="axis"> ONLY DETERMINES THE SIGN, NOT THE AXIS OF ROTATION</param>
        [MethodImpl((MethodImplOptions) 256)]
        public static float SignedAngle(float3 from, float3 to, float3 axis)
        {
            float num1 = Angle(from, to);
            float3 num234 = from.yzx * to.zxy - from.zxy * to.yzx;
            //float num2 = (float) ((double) from.y * (double) to.z - (double) from.z * (double) to.y);
            //float num3 = (float) ((double) from.z * (double) to.x - (double) from.x * (double) to.z);
            //float num4 = (float) ((double) from.x * (double) to.y - (double) from.y * (double) to.x);
            //float num5 = sign((float) (axis.x * num234.x + axis.y * num3 + axis.z * num4));
            float num5 = sign(csum(axis * num234));
            return num1 * num5;
        }

        /// <summary> Determine the rotation between two vectors where the second direction is projected onto the plane defined by the rotation axis.
        /// NOTE: EXPECTS NORMALIZED INPUTS.</summary>
        public static float SignedAngleDoneRight(float3 fromNorm, float3 toNorm, float3 rotationAxisNorm, float epsilon = .0001f, bool projectFrom = true, bool projectTo = true)
        {
            // Project vectors onto axis, unless otherwise specified
            fromNorm = projectFrom ? Vector3.ProjectOnPlane(fromNorm, rotationAxisNorm) : fromNorm;
            toNorm = projectTo ? Vector3.ProjectOnPlane(toNorm, rotationAxisNorm) : toNorm;
            
            // Check for values with essentially no rotation along a given axis, like the exact same vectors, or perfectly perpendicular vectors
            var dotOverlapOfProjected = dot(fromNorm, toNorm);
            if (1 - dotOverlapOfProjected < epsilon)
                return 0;
            
            float angle = SignedAngle(fromNorm, toNorm, rotationAxisNorm);
            return angle;
        }
        
        public static bool TryGetInfiniteLineIntersection2D(float2 pointA, float2 directionA, float2 pointB, float2 directionB, out float2 intersectionPoint, out float lengthAlongA)
        {
            intersectionPoint = float2.zero; 

            // Cross product to determine if the lines are parallel
            float directionProduct = directionA.x * directionB.y - directionA.y * directionB.x;

            // Due to floating point precision problems we have to check if the result is near zero instead of exactly zero
            if (abs(directionProduct) < Mathf.Epsilon)
            {
                lengthAlongA = 0;
                return false; // The lines are parallel, so an intersection is not possible
            }

            // Calculate differences
            var diff = pointA - pointB;
            
            lengthAlongA = (directionB.x * diff.y - directionB.y * diff.x) / directionProduct;

            // Calculate the exact intersection point. 
            var offsetAlongDirA = directionA * lengthAlongA;
            intersectionPoint = pointA + offsetAlongDirA;

            return true; // The lines intersect - return true and the intersectionPoint as output argument
        }
        
        public static bool LineSegmentIntersection(float2x2 a, float2x2 b, out float2 intersection, out float aNorm, out float bNorm)
        {
            var p1 = a.c0;
            var p2 = a.c1;
            var p3 = b.c0;
            var p4 = b.c1;
            intersection = float2.zero;
            aNorm = 0f;
            bNorm = 0f;

            var d = (p2.x - p1.x) * (p4.y - p3.y) - (p2.y - p1.y) * (p4.x - p3.x);
            if (d == 0f)
                return false;

            aNorm = ((p3.x - p1.x) * (p4.y - p3.y) - (p3.y - p1.y) * (p4.x - p3.x)) / d;
            bNorm = ((p3.x - p1.x) * (p2.y - p1.y) - (p3.y - p1.y) * (p2.x - p1.x)) / d;

            if (aNorm < 0f || aNorm > 1f || bNorm < 0f || bNorm > 1f)
                return false;

            intersection.x = p1.x + aNorm * (p2.x - p1.x);
            intersection.y = p1.y + aNorm * (p2.y - p1.y);

            return true;
        }

        public static bool LineSegmentIntersectionXZ(float3x2 a, float3x2 b, out float3 intersection, out float aNorm, out float bNorm, float heightLerp = 0.5f)
        {
            intersection = float3.zero;
            var aXZ = new float2x2(a.c0.xz, a.c1.xz);
            var bXZ = new float2x2(b.c0.xz, b.c1.xz);
            if (!LineSegmentIntersection(aXZ, bXZ, out var intersectionXZ, out aNorm, out bNorm))
                return false;

            var ya = lerp(a.c0.y, a.c1.y, aNorm);
            var yb = lerp(b.c0.y, b.c1.y, bNorm);
            intersection = new float3(intersectionXZ.x, lerp(ya, yb, heightLerp), intersectionXZ.y);
            return true;
        }

        public static class Grid
        {
            // Method to compute coord from index
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int2 IndexToCoord(int index, int width) => new(index % width, index / width);
        
            // Method to compute index from coord
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int CoordToIndex(int x, int y, int width) => y * width + x;
        
            // Method to compute index from coord
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int CoordToIndex(int2 xy, int width) => xy.y * width + xy.x;
        }
    }
}