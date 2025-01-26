using System;
using Drawing;
using Unity.Mathematics;
using VLib.Licensed;
using static Unity.Mathematics.math;

namespace VLib
{
    [Serializable]
    public struct VTriangle3
    {
        public float3 a, b, c; //Clockwise order is expected

        public readonly float3 Center => (a + b + c) / 3f; 

        public readonly float3 AToB => b - a;
        public readonly float3 BToC => c - b;
        public readonly float3 CToA => a - c;

        public readonly float3x2 EdgeAB => new(a, b);
        public readonly float3x2 EdgeBC => new(b, c);
        public readonly float3x2 EdgeCA => new(c, a);

        public readonly float2 ABNormalXZ => VMath.Rotate90CCW(AToB.xz);
        public readonly float2 BCNormalXZ => VMath.Rotate90CCW(BToC.xz);
        public readonly float2 CANormalXZ => VMath.Rotate90CCW(CToA.xz);

        public readonly VTriangle2 To2D_XZ => new(this);
        public readonly VTriangle2 To2D_XY => new(this, 0, 1);
        public readonly VTriangle2 To2D_YZ => new(this, 1, 2);

        public readonly float3x2 this[byte edgeIndex] => edgeIndex switch
        {
            0 => EdgeAB,
            1 => EdgeBC,
            2 => EdgeCA,
            _ => throw new ArgumentOutOfRangeException(nameof(edgeIndex), edgeIndex, "Edge Index must be 0, 1, or 2")
        };

        public readonly float Area => GetArea(new float3 (math.length(AToB), math.length(BToC), math.length(CToA)));

        public readonly float3 EdgeLengths => new float3(math.length(AToB), math.length(BToC), math.length(CToA));
        public readonly half3 EdgeLengthsHalf => new half3((half)math.length(AToB), (half)math.length(BToC), (half)math.length(CToA));

        public static float GetArea(in float3 edgeLengths)
        {
            float a = edgeLengths.x;
            float b = edgeLengths.y;
            float c = edgeLengths.z;
            float s = (a + b + c) / 2f;
            var area = math.sqrt(math.abs(s * (s - a) * (s - b) * (s - c)));
            BurstAssert.True(area >= 0);
            return area;
        }

        public VTriangle3(float3 a, float3 b, float3 c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }

        public readonly float3x2 GetSide(int index)
        {
            switch (index)
            {
                case 0: return EdgeAB;
                case 1: return EdgeBC;
                case 2: return EdgeCA;
                default: return 0;
            }
        }

        public static VTriangle3 operator +(VTriangle3 triangle, float3 offset) => new (triangle.a + offset, triangle.b + offset, triangle.c + offset);
        public static VTriangle3 operator -(VTriangle3 triangle, float3 offset) => new (triangle.a - offset, triangle.b - offset, triangle.c - offset);

        public BoundsNative Bounds
        {
            get
            {
                var bounds = new BoundsNative(a, 0f);
                bounds.Encapsulate(b);
                bounds.Encapsulate(c);
                return bounds;
            }
        }

        public void Rescale(float scale)
        {
            var center = Center;
            a = math.lerp(center, a, scale);
            b = math.lerp(center, b, scale);
            c = math.lerp(center, c, scale);
        }
        
        public VTriangle3 GetRescaled(float scale)
        {
            var center = Center;
            return new VTriangle3(math.lerp(center, a, scale), math.lerp(center, b, scale), math.lerp(center, c, scale));
        }

        public readonly bool ContainsXZInHeight(float3 pos, float heightThres)
        {
            // Check XZ Containment
            var tri2D = To2D_XZ;
            if (!tri2D.Contains(pos.xz))
                return false;
            
            // Check Height Containment
            var center = Center;
            float centerDif = center.y - pos.y;
            if (centerDif > heightThres)
                return false;

            return true;
        }
        
        public readonly bool ContainsXZInHeight(float3 pos, float heightBelowThres, float heightAboveThres)
        {
            // Check XZ Containment
            var tri2D = To2D_XZ;
            if (!tri2D.Contains(pos.xz))
                return false;
            
            // Check Height Containment
            return PointInHeightRangeXZ(pos, heightBelowThres, heightAboveThres, false);
            
            /*var center = Center;
            if (center.y > pos.y + heightAboveThres)
                return false;
            if (center.y < pos.y - heightBelowThres)
                return false;

            return true;*/
        }
        
        public readonly bool ContainsXZInHeightOffset(float3 pos, float heightBelowThres, float heightAboveThres, out float3 pointOnTri, float barycentricOffset = 0)
        {
            pointOnTri = ProjectPointToSurfaceXZ(pos.xz, out var barycentricCoords);
            
            // Check Barycentric XZ Containment
            if (math.cmin(barycentricCoords) < barycentricOffset)
                return false;
            
            // Check Height Containment
            return PointInHeightRangeOfSurfacePoint(pos.y, heightBelowThres, heightAboveThres, pointOnTri);
        }

        public readonly bool PointInHeightRangeXZ(float3 point, float heightBelowThres, float heightAboveThres, bool clampBarycentric)
        {
            var surfacePointXZ = clampBarycentric ?
                ProjectPointToSurfaceXZClamped(point.xz, out _, out _) :
                ProjectPointToSurfaceXZ(point.xz, out _);
            return PointInHeightRangeOfSurfacePoint(point.y, heightBelowThres, heightAboveThres, surfacePointXZ);
        }

        static bool PointInHeightRangeOfSurfacePoint(float pointY, float heightBelowThres, float heightAboveThres, float3 surfacePointXZ)
        {
            return !(surfacePointXZ.y > pointY + heightAboveThres || surfacePointXZ.y < pointY - heightBelowThres);
        }
        
        public static float3 ClampBarycentric(float3 coords)
        {
            var positiveCoords = math.max(coords, 0);
            var negativeSum = math.csum(math.min(coords, 0)); 
            return positiveCoords / (1f - negativeSum);
        }

        /// <summary>
        /// Projects a world point on the triangle in XZ space
        /// </summary>
        public readonly float3 ProjectPointToSurfaceXZ(float2 point, out float3 barycentricCoords)
        {
            var tri2D = new VTriangle2(this);
            barycentricCoords = tri2D.BarycentricCoordsOfPoint(point);
            return PointAtBarycentric(barycentricCoords);
        }
        
        /// <summary>
        /// Projects a world point on the triangle in XZ space
        /// </summary>
        public readonly float3 ProjectPointToSurfaceXZClamped(float2 point, out float3 barycentricCoords, out float3 clampedBarycentricCoords)
        {
            var tri2D = new VTriangle2(this);
            barycentricCoords = tri2D.BarycentricCoordsOfPoint(point); 
            clampedBarycentricCoords = ClampBarycentric(barycentricCoords);
            return PointAtBarycentric(clampedBarycentricCoords);
        }

        public readonly float3 PointAtBarycentric(float3 bary) => a * bary.x + b * bary.y + c * bary.z;

        public readonly float3 BarycentricCoordsOfPoint(float3 point) => Barycentric(point, a, b, c);
        
        public readonly float3 RandomPointOnSurface(float random01A, float random01B)
        {
            //Source: I made it the fuck up - Sal
            if (random01A + random01B > 1f)
            {
                random01A = 1f - random01A;
                random01B = 1f - random01B;
            }
            return a - random01A * (c - b) + (random01B - 1f) * (a - c);
        }

        /*/// <summary> Returns true if the input line overlaps any edge of the triangle in XZ space, where the intersection point returned is the one closest to the end of the input line.
        /// Think of it as getting the point where a line EXITS a triangle. </summary>
        public readonly bool TryGetExitPointFromXZView(float3x2 line, out float3 exitPoint, out float3x2 exitEdge)
        {
            var lineVec = line.c1 - line.c0;
            var lineVec2D = lineVec.xz;
            if (lineVec2D.Equals(float2.zero))
            {
                exitPoint = default;
                exitEdge = default;
                return false;
            }
            var lineDir2D = math.normalize(lineVec2D);
            
            // Store intersection closest to the end of the input line, which should get us our EXIT point
            var closestEdgeIntersectIndexToEndOfLine = -1;
            float3x2 closestEdge = default;
            var closestIntersectDistSq = float.MaxValue;
            var closestIntersectPoint = float2.zero;
            var closestIntersectPercentAlongEdge = 0f;

            // Try intersect all three edges
            IntersectTest(EdgeAB, 0, line, lineDir2D);
            IntersectTest(EdgeBC, 1, line, lineDir2D);
            IntersectTest(EdgeCA, 2, line, lineDir2D);

            // Return the correct intersection
            switch (closestEdgeIntersectIndexToEndOfLine)
            {
                case 0:
                    exitPoint = math.lerp(EdgeAB.c0, EdgeAB.c1, closestIntersectPercentAlongEdge);
                    exitEdge = EdgeAB;
                    return true;
                case 1:
                    exitPoint = math.lerp(EdgeBC.c0, EdgeBC.c1, closestIntersectPercentAlongEdge);
                    exitEdge = EdgeBC;
                    return true;
                case 2:
                    exitPoint = math.lerp(EdgeCA.c0, EdgeCA.c1, closestIntersectPercentAlongEdge);
                    exitEdge = EdgeCA;
                    return true;
                default:
                    exitPoint = default;
                    exitEdge = default;
                    return false;
            }

            // Local functions

            void IntersectTest(float3x2 triangleEdge, int edgeIndex, float3x2 incomingLine, float2 lineDirXZ)
            {
                // First check if the triangle edge faces the same way as the incoming line, we are only looking for an exit point!
                var triangleEdgeNormal = VMath.Rotate90CCW(triangleEdge.c1.xz - triangleEdge.c0.xz);
                if (math.dot(triangleEdgeNormal, lineDirXZ) <= 0)
                    return;
                
                // Check actual intersection
                if (VMath.LineSegmentsIntersectXZ(triangleEdge, incomingLine, out var intersectPoint, out var intersectPercentAlongEdge))
                {
                    var dist = math.distancesq(intersectPoint, line.c1.xz);
                    if (dist < closestIntersectDistSq)
                    {
                        closestEdgeIntersectIndexToEndOfLine = edgeIndex;
                        closestIntersectDistSq = dist;
                        closestIntersectPoint = intersectPoint;
                        closestIntersectPercentAlongEdge = intersectPercentAlongEdge;
                        closestEdge = triangleEdge;
                    }
                }
            }
        }*/

        public readonly bool SegmentOverlapXZ(float3x2 segment, out float3x2 overlap, float heightLerp = 0f)
        {
            overlap = default;
            var p0 = ProjectPointToSurfaceXZ(segment.c0.xz, out var bary0);
            var p1 = ProjectPointToSurfaceXZ(segment.c1.xz, out var bary1);
            var contains0 = math.all(bary0 >= 0f);
            var contains1 = math.all(bary1 >= 0f);

            if (contains0)
                p0.y = math.lerp(segment.c0.y, p0.y, heightLerp);
            if (contains1)
                p1.y = math.lerp(segment.c1.y, p1.y, heightLerp);

            if (contains0 && contains1)
            {
                overlap = new float3x2(p0, p1);
                return true;
            }
            
            var hitsAB = VMath.LineSegmentIntersectionXZ(segment, EdgeAB, out var intersectionAB, out var normAB, out _, heightLerp);
            var hitsBC = VMath.LineSegmentIntersectionXZ(segment, EdgeBC, out var intersectionBC, out var normBC, out _, heightLerp);
            var hitsCA = VMath.LineSegmentIntersectionXZ(segment, EdgeCA, out var intersectionCA, out var normCA, out _, heightLerp);

            if (hitsAB)
            {
                if(contains0)
                    overlap = new float3x2(p0, intersectionAB);
                else if (contains1)
                    overlap = new float3x2(intersectionAB, p1);
                else if (hitsBC)
                    overlap = normAB < normBC ? new float3x2(intersectionAB, intersectionBC) : new float3x2(intersectionBC, intersectionAB);
                else if (hitsCA)
                    overlap = normAB < normCA ? new float3x2(intersectionAB, intersectionCA) : new float3x2(intersectionCA, intersectionAB);
                else
                    return false;
                return true;
            }
            if (hitsBC)
            {
                if(contains0)
                    overlap = new float3x2(p0, intersectionBC);
                else if (contains1)
                    overlap = new float3x2(intersectionBC, p1);
                else if (hitsCA)
                    overlap = normBC < normCA ? new float3x2(intersectionBC, intersectionCA) : new float3x2(intersectionCA, intersectionBC);
                else
                    return false;
                return true;
            }
            if (hitsCA)
            {
                if(contains0)
                    overlap = new float3x2(p0, intersectionCA);
                else if (contains1)
                    overlap = new float3x2(intersectionCA, p1);
                else
                    return false;
                return true;
            }
            return false;
        }
        
        // Compute barycentric coordinates (u, v, w) for point p with respect to triangle (a, b, c)
        public static float3 Barycentric(float3 p, float3 a, float3 b, float3 c)
        {
            float3 v0 = b - a, v1 = c - a, v2 = p - a;
            float d00 = math.dot(v0, v0);
            float d01 = math.dot(v0, v1);
            float d11 = math.dot(v1, v1);
            float d20 = math.dot(v2, v0);
            float d21 = math.dot(v2, v1);
            float denom = d00 * d11 - d01 * d01;
            
            var v = (d11 * d20 - d01 * d21) / denom;
            var w = (d00 * d21 - d01 * d20) / denom;
            var u = 1.0f - v - w;
            return new(u, v, w);
        }

        public readonly float3 ClosestPoint(in float3 point) => FromEmbree.ClosestPointTriangle(point, a, b, c);

        public readonly void DrawAline(CommandBuilder drawer, UnityEngine.Color color)
        {
            drawer.PushColor(color);
            drawer.WireTriangle(a, b, c);
            drawer.WireSphere(Center, .2f);
            drawer.PopColor();
        }
    }
}