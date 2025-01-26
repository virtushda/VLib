using System;
using Unity.Mathematics;

namespace VLib
{
    public struct VTriangle2 : IConvex<float2>
    {
        public float2 a, b, c; //Clockwise order is expected

        public readonly float2 AB => b - a;
        public readonly float2 BC => c - b;
        public readonly float2 CA => a - c;

        public readonly float2 ABNormalXZ => math.normalizesafe(VMath.Rotate90CCW(AB));
        public readonly float2 BCNormalXZ => math.normalizesafe(VMath.Rotate90CCW(BC));
        public readonly float2 CANormalXZ => math.normalizesafe(VMath.Rotate90CCW(CA));

        public readonly int Sides => 3;

        public VTriangle2(float2 a, float2 b, float2 c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }

        public VTriangle2(VTriangle3 triangle3, int x2D_MapsTo = 0, int y2D_MapsTo = 2)
        {
            a = new float2(triangle3.a[x2D_MapsTo], triangle3.a[y2D_MapsTo]);
            b = new float2(triangle3.b[x2D_MapsTo], triangle3.b[y2D_MapsTo]);
            c = new float2(triangle3.c[x2D_MapsTo], triangle3.c[y2D_MapsTo]);
        }

        public readonly RectNative GetEncapsulatingRect()
        {
            RectNative rect = new RectNative(a);
            rect.Encapsulate(b);
            rect.Encapsulate(c);
            return rect;
        }

        public readonly bool Intersects(in VTriangle2 other)
        {
            //Check Triangle Outside ANY EDGE
            return !(AllOutsideLine2D(other.ABNormalXZ, other.a) ||
                   AllOutsideLine2D(other.BCNormalXZ, other.b) ||
                   AllOutsideLine2D(other.CANormalXZ, other.c) ||
                   other.AllOutsideLine2D(ABNormalXZ, a) ||
                   other.AllOutsideLine2D(BCNormalXZ, b) ||
                   other.AllOutsideLine2D(CANormalXZ, c));
        }

        /// <returns>Inside/Intersecting: False -- Outside: True</returns>
        public readonly bool AllOutsideLine2D(float2 linePnt, float2 lineNorm)
        {
            //Lines to Points
            var lineToA = linePnt - a;
            var lineToB = linePnt - b;
            var lineToC = linePnt - c;
            //Dot Products to check line side
            float3 dots = math.ceil(new float3(
                math.dot(lineNorm, lineToA),
                math.dot(lineNorm, lineToB),
                math.dot(lineNorm, lineToC)));
            return math.all(dots > 0);
        }
        
        public readonly bool CircleOutsideLine2D(in float2 linePnt, in float2 lineNorm, float2 point, in float radius)
        {
            // Offset Point by radius negatively along normal (generate worst-case point for circle intersect)
            point -= lineNorm * radius;
            return PointOutsideLine2D(linePnt, lineNorm, point);
        }
        
        public readonly bool PointOutsideLine2D(in float2 linePnt, in float2 lineNorm, float2 point)
        {
            //Lines to Points
            var lineToA = point - linePnt;
            //Dot Products to check line side
            return math.dot(lineNorm, lineToA) > 0;
        }

        public readonly bool Contains(float2 point)
        {
            var s = (a.x - c.x) * (point.y - c.y) - (a.y - c.y) * (point.x - c.x);
            var t = (b.x - a.x) * (point.y - a.y) - (b.y - a.y) * (point.x - a.x);

            if (math.all(new bool3((s < 0) != (t < 0), s != 0, t != 0)))
                return false;

            var d = (c.x - b.x) * (point.y - b.y) - (c.y - b.y) * (point.x - b.x);
            return d == 0 || (d < 0) == (s + t <= 0);
        }

        /// <summary> Not perfectly accurate around the triangle corners, can detect a little extra overlap. Otherwise should be a very very fast algorithm. </summary>
        public readonly bool OverlapsCircleFast(float2 circleCenter, float circleRadius)
        {
            if (CircleOutsideLine2D(a, ABNormalXZ, circleCenter, circleRadius))
                return false;
            if (CircleOutsideLine2D(b, BCNormalXZ, circleCenter, circleRadius))
                return false;
            if (CircleOutsideLine2D(c, CANormalXZ, circleCenter, circleRadius))
                return false;
            // If not 'beyond' any triangle line, then we must be overlapping
            return true;
        }

        public bool Contains(float2 point, out float3 barycentricCoords)
        {
            barycentricCoords = BarycentricCoordsOfPoint(point);
            return !math.any(barycentricCoords < 0f);
        }
        
        public float3 BarycentricCoordsOfPoint(float2 point) => Barycentric(point, a, b, c);
        
        // Compute barycentric coordinates (u, v, w) for point p with respect to triangle (a, b, c)
        public static float3 Barycentric(float2 p, float2 a, float2 b, float2 c)
        {
            float2 v0 = b - a, v1 = c - a, v2 = p - a;
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
        
        /*bool cross(VTriangle points, b, c, normal) {
            let bx = b.x;
            let by = b.y;
            let cyby = c.y - by;
            let cxbx = c.x - bx;
            let pa = points.a;
            let pb = points.b;
            let pc = points.c;
            return !(
                (((pa.x - bx) * cyby - (pa.y - by) * cxbx) * normal < 0) ||
                (((pb.x - bx) * cyby - (pb.y - by) * cxbx) * normal < 0) ||
                (((pc.x - bx) * cyby - (pc.y - by) * cxbx) * normal < 0));
        }

// test if one of the triangles has a side with all of the other triangles points on the outside.  Assume that we know all the normals of ech triangle, which tells us the direction that the points are specified.
        bool Intersects(VTriangle other) 
        {
            var normal0 = (b.x-a.x)*(c.y-a.y)- (b.y-a.y)*(c.x-a.x);
            var normal1 = (other.b.x-other.a.x)*(other.c.y-other.a.y) - (other.b.y-other.a.y)*(other.c.x-other.a.x);
              
            return !(cross(other, t0.a, t0.b, normal0) ||
                     cross(other, t0.b, t0.c, normal0) ||
                     cross(other, t0.c, t0.a, normal0) ||
                     cross(t0, other.a, other.b, normal1) ||
                     cross(t0, other.b, other.c, normal1) ||
                     cross(t0, other.c, other.a, normal1));
        }*/
        
        public float2 GetVertex(int index)
        {
            switch (index)
            {
                case 0: return a;
                case 1: return b;
                case 2: return c;
                default: return 0;
            }
        }
        
        public float2 GetSide(int index)
        {
            switch (index)
            {
                case 0: return AB;
                case 1: return BC;
                case 2: return CA;
                default: return 0;
            }
        }

        public float2 GetNormal(int index)
        {
            switch (index)
            {
                case 0: return ABNormalXZ;
                case 1: return BCNormalXZ;
                case 2: return CANormalXZ;
                default: throw new InvalidOperationException("Triangles only have 3 sides silly!");
            }
        }
    }
}