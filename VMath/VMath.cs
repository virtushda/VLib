﻿using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;
using Debug = UnityEngine.Debug;
using float2 = Unity.Mathematics.float2;
using float3 = Unity.Mathematics.float3;
using float4 = Unity.Mathematics.float4;
using int2 = Unity.Mathematics.int2;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEvenB(this sbyte value) => (value & 1) == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEvenB(this byte value) => (value & 1) == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEvenS(this short value) => (value & 1) == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEvenS(this ushort value) => (value & 1) == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEven(this int value) => (value & 1) == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEven(this uint value) => (value & 1) == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEvenL(this long value) => (value & 1) == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEvenL(this ulong value) => (value & 1) == 0;
        
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

        /// <summary> Get a blend between the minimum and the average of the given values </summary>
        public static float MinAvgBlend2(float a, float b, float blendAverage01)
        {
            var avg = (a + b) / 2f;
            var min = math.min(a, b);
            return lerp(min, avg, blendAverage01);
        }

        /// <summary> Get a blend between the minimum and the average of the given values </summary>
        public static float MinAvgBlend3(float a, float b, float c, float blendAverage01)
        {
            var avg = (a + b + c) / 3f;
            var min = math.min(math.min(a, b), c);
            return lerp(min, avg, blendAverage01);
        }

        /// <summary> Get a blend between the minimum and the average of the given values </summary>
        public static float MinAvgBlend4(float a, float b, float c, float d, float blendAverage01)
        {
            var avg = (a + b + c + d) / 4f;
            var min = math.min(math.min(a, b), math.min(c, d));
            return lerp(min, avg, blendAverage01);
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

        public static float2 ClampToRectAroundZero(this float2 vector, in RectNative rect, bool logOnBadRect = true)
        {
            var vectorAbs = abs(vector);
            // Pick extents based on which sides of the rect the vector is on
            var extentsFromZero = select(abs(rect.Min), rect.Max, vector >= float2.zero);
            
            //BurstAssert.True(all(extentsFromZero >= .0000001f)); //Rect must have extents to avoid divide by zero!
            if (any(extentsFromZero < .0000001f))
            {
                if (logOnBadRect)
                    Debug.LogError("Rect must contain (0,0) to avoid divide by zero!");
                return float2.zero;
            }

            var maxVectorScalePastRect = cmax(vectorAbs / extentsFromZero);
            
            // If the vector is already within the rect, return it
            // This should also protect against division by zero doing it this way
            return maxVectorScalePastRect < 1 ? vector : vector / maxVectorScalePastRect; // Flip the scale to shrink the vector to fit into the rect
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
            return num < 1.0000000036274937E-15 ? 0.0f : (float) acos((double) clamp(dot(from, to) / num, -1f, 1f)) * TODEGREES;
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

        public static float3 ClosestPointOnRay(in float3 point, in float3 rayOrigin, in float3 rayDirection)
        {
            BurstAssert.ApproxEquals(lengthsq(rayDirection), 1f); // Must be normalized
            var rayOriginToPoint = point - rayOrigin;
            var distanceAlongRay = dot(rayOriginToPoint, rayDirection);
            return rayOrigin + distanceAlongRay * rayDirection;
        }
        
        public static bool LineSegmentIntersection(in float2x2 a, in float2x2 b, out float2 intersection, out float aNorm, out float bNorm)
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

        public static bool LineSegmentIntersectionXZ(in float3x2 a, in float3x2 b, out float3 intersection, out float aNorm, out float bNorm, float heightLerp = 0.5f)
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
        
        public static bool LineSegmentSphereIntersection(in float3 p1, in float3 p2, in float3 center, float radius, out float3 intersection)
        {
            intersection = float3.zero;

            var d = p2 - p1; // Direction vector of the line
            var f = p1 - center; // Vector from the center of the sphere to the first point

            var a = dot(d, d);
            var b = 2f * dot(f, d);
            var c = dot(f, f) - radius * radius;

            var discriminant = b * b - 4f * a * c;
            
            // No intersection
            if (discriminant < 0)
                return false;

            // One or two solutions
            discriminant = sqrt(discriminant);
            var t1 = (-b - discriminant) / (2f * a);
            var t2 = (-b + discriminant) / (2f * a);
            
            // If t1 or t2 is between 0 and 1, it's on the line segment
            if (t1 is >= 0f and <= 1f)
            {
                intersection = p1 + t1 * d;
                return true;
            }
            if (t2 is >= 0f and <= 1f)
            {
                intersection = p1 + t2 * d;
                return true;
            }
            return false;
        }

        /// <summary> Tells you how far along a given line segment a point would be. The point is inherently projected to the line.
        /// Values below 0 indicate the point is before the start of the line segment, and values above 1 indicate the point is after the end of the line segment. </summary>
        public static float InverseSampleLineSegment(in float3 p0, in float3 p1, in float3 position)
        {
            var v = p1 - p0;
            var w = position - p0;
            var dotVV = dot(v, v);
            if (dotVV < EPSILON)
                return 0; // p0 and p1 are the same point, so we can't sample along the line segment
            return dot(w, v) / dotVV;
        }

        /// <summary> Constrains a given position to lie on the line segment defined by points `a` and `b`. </summary>
        /// <param name="position">The position to constrain.</param>
        /// <param name="a">The start point of the line segment.</param>
        /// <param name="b">The end point of the line segment.</param>
        /// <param name="lineLerpT">The interpolation factor indicating how far along the segment the constrained position lies.</param>
        /// <returns>The constrained position on the line segment.</returns>
        public static float3 ConstrainToSegment(in float3 position, in float3 a, in float3 b, out float lineLerpT)
        {
            lineLerpT = InverseSampleLineSegment(a, b, position);
            return lerp(a, b, saturate(lineLerpT));
        }

        // Implementation of https://zalo.github.io/blog/closest-point-between-segments/
        /// <summary> Calculates the closest points between two line segments `ab` and `cd` in 3D space. </summary>
        /// <param name="ab">The first line segment defined by two points.</param>
        /// <param name="cd">The second line segment defined by two points.</param>
        /// <param name="closestPointOnSegAB">The closest point on segment `ab`.</param>
        /// <param name="closestPointOnSegCD">The closest point on segment `cd`.</param>
        /// <param name="lineABLerpT">The interpolation factor along segment `ab`. Unclamped.</param>
        public static void ClosestPointBetweenTwoSegments(in float3x2 ab, in float3x2 cd, out float3 closestPointOnSegAB, out float3 closestPointOnSegCD, out float lineABLerpT)
        {
            var segDC = cd.c1 - cd.c0; // segD-segC; 
            var lineDirSqrMag = dot(segDC, segDC);

            var inPlaneA = ab.c0 - ((dot(ab.c0 - cd.c0, segDC) / lineDirSqrMag) * segDC);
            var inPlaneB = ab.c1 - ((dot(ab.c1 - cd.c0, segDC) / lineDirSqrMag) * segDC);
            var inPlaneBA = inPlaneB - inPlaneA;
            
            var t = 0f;
            if (Hint.Likely(any(inPlaneA != inPlaneB)))
                t = dot(cd.c0 - inPlaneA, inPlaneBA) / dot(inPlaneBA, inPlaneBA);
            var segABtoLineCD = lerp(ab.c0, ab.c1, saturate(t));

            closestPointOnSegCD = ConstrainToSegment(segABtoLineCD, cd.c0, cd.c1, out _);
            closestPointOnSegAB = ConstrainToSegment(closestPointOnSegCD, ab.c0, ab.c1, out lineABLerpT); // Is this second constrain necessary??
        }

        #region Checks / Defensive
        
        /// <summary> Performs a division, but checks for divide by zero first. The check is stripped out in release code. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DivideChecked(this float numerator, float denominator, float epsilon = EPSILON, bool logError = true)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG || DEVELOPMENT_BUILD
            if (abs(denominator) < epsilon)
            {
                if (logError)
                    Debug.LogError("Division by zero!");
                return 0;
            }
#endif
            return numerator / denominator;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 DivideChecked(this float2 numerator, float2 denominator, float epsilon = EPSILON, bool logError = true)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG || DEVELOPMENT_BUILD
            if (any(abs(denominator) < epsilon))
            {
                if (logError)
                    Debug.LogError("Division by zero!");
                return float2.zero;
            }
#endif
            return numerator / denominator;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 DivideChecked(this float3 numerator, float3 denominator, float epsilon = EPSILON, bool logError = true)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG || DEVELOPMENT_BUILD
            if (any(abs(denominator) < epsilon))
            {
                if (logError)
                    Debug.LogError("Division by zero!");
                return float3.zero;
            }
#endif
            return numerator / denominator;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 DivideChecked(this float4 numerator, float4 denominator, float epsilon = EPSILON, bool logError = true)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG || DEVELOPMENT_BUILD
            if (any(abs(denominator) < epsilon))
            {
                if (logError)
                    Debug.LogError("Division by zero!");
                return float4.zero;
            }
#endif
            return numerator / denominator;
        }

        /// <summary> Divide, but if the denominator is zero, return 0. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DivideSafe(this float numerator, float denominator, float epsilon = EPSILON)
        {
            return select(numerator / denominator, 0, abs(denominator) < epsilon);
            
            /*if (abs(denominator) < epsilon)
                return 0;
            return numerator / denominator;*/
        }
        
        /// <summary> Performs a division, but checks for divide by zero first. The check is stripped out in release code. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int DivideChecked(this int numerator, int denominator, bool logError = true)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG || DEVELOPMENT_BUILD
            if (denominator == 0)
            {
                if (logError)
                    Debug.LogError("Division by zero!");
                return 0;
            }
#endif
            return numerator / denominator;
        }

        /// <summary> Divide, but if the denominator is zero, return 0. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int DivideSafe(this int numerator, int denominator)
        {
            if (denominator == 0)
                return 0;
            return numerator / denominator;
        }

        /// <summary> <inheritdoc cref="ModuloChecked(int,int)"/> </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ModuloChecked(this float numerator, float denominator, float epsilon = EPSILON)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG || DEVELOPMENT_BUILD
            if (abs(denominator) < epsilon)
            {
                Debug.LogError("Division by zero!");
                return 0;
            }
#endif
            return numerator % denominator;
        }

        /// <summary> Perform a modulo operator, but in the editor, check that we're not dividing by zero! </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ModuloChecked(this int numerator, int denominator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG || DEVELOPMENT_BUILD
            if (abs(denominator) < 1)
            {
                Debug.LogError("Division by zero!");
                return 0;
            }
#endif
            return numerator % denominator;
        }
        
        /// <summary> Modulo, but if the denominator is zero, return 0. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ModuloSafe(this float numerator, float denominator, float epsilon = EPSILON)
        {
            if (abs(denominator) < epsilon)
                return 0;
            return numerator % denominator;
        }
        
        /// <summary> <inheritdoc cref="ModuloSafe"/> </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ModuloSafe(this int numerator, int denominator)
        {
            if (denominator == 0)
                return 0;
            return numerator % denominator;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void CheckNANOrInf(this float value)
        {
            if (isnan(value))
                Debug.LogError("VALUE NAN");
            else if (isinf(value))
                Debug.LogError("VALUE INFINITE");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void CheckNANOrInf(this float2 value)
        {
            if (any(isnan(value)))
                Debug.LogError("VALUE NAN");
            else if (any(isinf(value)))
                Debug.LogError("VALUE INFINITE");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void CheckNANOrInf(this float3 value)
        {
            if (any(isnan(value)))
                Debug.LogError("VALUE NAN");
            else if (any(isinf(value)))
                Debug.LogError("VALUE INFINITE");
        }

        public static bool IsNanOrInf(in this float4 value) => IsNanOrInf(value, out _);
        
        public static bool IsNanOrInf(in this float4 value, out bool falseNan_trueInf)
        {
            if (any(isnan(value)))
            {
                falseNan_trueInf = true;
                return true;
            }
            if (any(isinf(value)))
            {
                falseNan_trueInf = false;
                return true;
            }
            falseNan_trueInf = false;
            return false;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void CheckNANOrInf(this float4 value)
        {
            if (any(isnan(value)))
                Debug.LogError("VALUE NAN");
            else if (any(isinf(value)))
                Debug.LogError("VALUE INFINITE");
        }
        
        #endregion

        public static class Curve
        {
            /// <summary> Produces a simple bendable curve that travels from 0 to 1. The multiplier and offset can be used to transform the curve to do multiple things. <br/>
            /// Equivalent to: 'y=\left(x^{r}\right)m+b' on Desmos (copy and paste) </summary>
            public static float SimpleAdjustExponent_01(float x, float xExponent, float multiplier = 1f, float offset = 0) => (pow(x, xExponent) * multiplier) + offset;

            /// <summary> Faster version of <see cref="SimpleAdjustExponent_01"/> without the 'pow', but exponent is fixed to 2. </summary>
            public static float SimpleExponent2_01(float x, float multiplier = 1f, float offset = 0) => (x * x) * multiplier + offset;
            
            /// <summary> Faster version of <see cref="SimpleAdjustExponent_01"/> without the 'pow', but exponent is fixed to 3. </summary>
            public static float SimpleExponent3_01(float x, float multiplier = 1f, float offset = 0) => (x * x * x) * multiplier + offset;
            
            /// <summary> Faster version of <see cref="SimpleAdjustExponent_01"/> without the 'pow', but exponent is fixed to 4. </summary>
            public static float SimpleExponent4_01(float x, float multiplier = 1f, float offset = 0) => (x * x * x * x) * multiplier + offset;
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

            /*/// <summary> Loop-free method to find the coordinate if you spiral around (0,0) 'index' times in a counter-clockwise direction. </summary>
            public static int2 SpiralIndexToCoord(int index)
            {
                if (index == 0)
                    return int2.zero;
                index -= 1;
                int layer = (int) floor((sqrt(index) + 1) / 2);
                int layerSize = 2 * layer + 1;
                int maxIndexInLayer = layerSize * layerSize;
                layerSize -= 1;

                if (index >= maxIndexInLayer - layerSize)
                    return new int2(-layer + (maxIndexInLayer - index), layer);
                maxIndexInLayer -= layerSize;

                if (index >= maxIndexInLayer - layerSize)
                    return new int2(-layer, -layer + (maxIndexInLayer - index));
                maxIndexInLayer -= layerSize;

                if (index >= maxIndexInLayer - layerSize)
                    return new int2(layer - (maxIndexInLayer - index), -layer);

                return new int2(layer, layer - (maxIndexInLayer - index - layerSize));
            }
            
            /// <summary> Loop-free method to find the coord if you're spiraling around (0,0) 'index' times in a counter-clockwise direction. </summary>
            public static int SpiralCoordToIndex(int2 coord)
            {
                if (coord.Equals(int2.zero))
                    return 0;

                int layer = max(abs(coord.x), abs(coord.y));
                int layerSize = 2 * layer + 1;
                int maxIndexInLayer = layerSize * layerSize;
                layerSize -= 1;

                if (coord.y == layer)
                    return maxIndexInLayer - (layer - coord.x);
                maxIndexInLayer -= layerSize;

                if (coord.x == -layer)
                    return maxIndexInLayer - (layer - coord.y);
                maxIndexInLayer -= layerSize;

                if (coord.y == -layer)
                    return maxIndexInLayer - (layer + coord.x);

                return maxIndexInLayer - (layer + coord.y);
            }*/
        }

        public static class DistField
        {
            /// <summary> Adapted from: https://www.shadertoy.com/view/wdBXRW </summary>
            public static float SignedDistPoly2D(in UnsafeList<float2> vertices, float2 point)
            {
                var vertCount = vertices.Length;
                float d = dot(point - vertices[0], point - vertices[0]);
                float s = 1.0f;
                for (int i = 0, j = vertCount - 1; i < vertCount; j = i, i++)
                {
                    // distance
                    float2 e = vertices[j] - vertices[i];
                    float2 w = point - vertices[i];
                    float2 b = w - e * clamp(dot(w, e) / dot(e, e), 0.0f, 1.0f);
                    d = min(d, dot(b, b));

                    // winding number from http://geomalgorithms.com/a03-_inclusion.html
                    var cond = new bool3(
                        point.y >= vertices[i].y,
                        point.y < vertices[j].y,
                        e.x * w.y > e.y * w.x);
                    if (all(cond) || !any(cond))
                        s = -s;
                }

                return s * sqrt(d);
            }
        }
    }
}