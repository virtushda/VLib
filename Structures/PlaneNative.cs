using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using static System.Math;

namespace VLib
{
    [Serializable, GenerateTestsForBurstCompatibility]
    public struct PlaneNative : IEquatable<PlaneNative>
    {
        public float3 planePoint;
        public float3 normal;

        #region "Constructors"
        /// <summary>Default constructor, initializes plane in global cordinate system.</summary>
        public PlaneNative(float3 planePoint, float3 normal)
        {
            this.planePoint = planePoint;
            this.normal = normalizesafe(normal);
        }

        /// <summary>
        /// Initializes plane using general equation in 3D space: A*x+B*y+C*z+D=0.
        /// </summary>
        /// <param name="a">Parameter "A" in general plane equation.</param>
        /// <param name="b">Parameter "B" in general plane equation.</param>
        /// <param name="c">Parameter "C" in general plane equation.</param>
        /// <param name="d">Parameter "D" in general plane equation.</param>
        /// <param name="coord">Coordinate system in which plane equation is defined (default: Coord3d.GlobalCS).</param>
        /*public PlaneNative(double a, double b, double c, double d, Coord3d coord = null)
        {
            if (coord == null)
            {
                coord = Coord3d.GlobalCS;
            }
            if (Abs(a) > Abs(b) && Abs(a) > Abs(c))
            {
                point = new Point3d(-d / a, 0, 0, coord);
            }
            else if (Abs(b) > Abs(a) && Abs(b) > Abs(c))
            {
                point = new Point3d(0, -d / b, 0, coord);
            }
            else
            {
                point = new Point3d(0, 0, -d / c, coord);
            }
            normal = new Vector3d(a, b, c, coord);
        }*/

        /// <summary>Initializes plane using three points.</summary>
        public PlaneNative(float3 p1, float3 p2, float3 p3)
        {
            float3 v1 = p1 - p2;
            float3 v2 = p1 - p3;
            normal = normalizesafe(cross(v1, v2));
            planePoint = p1;
        }
        #endregion

        public bool PointOutside(float3 point)
        {
            float planeToPointDir = dot(point - planePoint, normal);
            return planeToPointDir > 0;
        }

        public bool SphereOutside(float4 sphere)
        {
            // Find point on sphere that is deepest in plane
            float3 deepestPoint = sphere.xyz - normal * sphere.w; // w is radius
            return PointOutside(deepestPoint);
        }
 
        // TODO: Make this more efficient...
        public bool BoundsOutsideNaive(BoundsNative bounds)
        {
            return PointOutside(bounds.XMinYMinZMin) &&
                   PointOutside(bounds.XMaxYMinZMax) &&
                   PointOutside(bounds.XMinYMinZMax) &&
                   PointOutside(bounds.XMaxYMinZMin) &&
                   PointOutside(bounds.XMinYMaxZMin) &&
                   PointOutside(bounds.XMaxYMaxZMax) &&
                   PointOutside(bounds.XMinYMaxZMax) &&
                   PointOutside(bounds.XMaxYMaxZMin);
        }

        /*public bool BoundsOutsideNative(OBoundsNative oBounds, bool planeIsWorldSpace = true)
        {
            var 
        }*/

        /// <summary>Test all 8 points, simple</summary>
        /// <returns>0: Inside plane, 1: Intersecting plane, 2: Outside plane</returns>
        public byte BoundsIntersectionNaive(BoundsNative bounds)
        {
            bool4 bottom4 = new bool4(
                PointOutside(bounds.XMinYMinZMin),
                PointOutside(bounds.XMaxYMinZMax),
                PointOutside(bounds.XMinYMinZMax),
                PointOutside(bounds.XMaxYMinZMin));
            
            bool4 top4 = new bool4(PointOutside(bounds.XMinYMaxZMin),
                                    PointOutside(bounds.XMaxYMaxZMax),
                                    PointOutside(bounds.XMinYMaxZMax),
                                    PointOutside(bounds.XMaxYMaxZMin));
            
            if (all(bottom4) && all(top4))
                return 2;
            if (any(bottom4) && any(top4))
                return 1;
            return 0;

        }

        public bool Equals(PlaneNative otherPlane) => planePoint.Equals(otherPlane.planePoint) && normal.Equals(otherPlane.normal);

        public void DebugDrawNormal(Color c)
        {
            Debug.DrawLine(planePoint, planePoint + normal, c);
        }

        // Operators overloads
        //-----------------------------------------------------------------

        public static bool operator ==(PlaneNative s1, PlaneNative s2) => s1.Equals(s2);

        public static bool operator !=(PlaneNative s1, PlaneNative s2) => !s1.Equals(s2);
    }
}