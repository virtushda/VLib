using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using Drawing;
using Unity.Mathematics;
using UnityEngine;

namespace VLib
{
    [Serializable]
    public struct BoundsNative
    {
        [SerializeField] float3 m_Center;
        [SerializeField] float3 m_Extents;

        public static BoundsNative Identity = new BoundsNative(new float3(-10,-10,-10), float3.zero);
        // Creates new Bounds with a given /center/ and total /size/. Bound ::ref::extents will be half the given size.
        public BoundsNative(float3 center, float3 size)
        {
            m_Center = center;
            m_Extents = size * 0.5F;
        }

        /// <summary>
        /// RectNative -> Bounds
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="thirdDimensionAxis">0:X, 1:Y, 2:Z</param>
        /// <param name="thirdDimensionCenter"></param>
        /// <param name="thirdDimensionHeight"></param>
        public BoundsNative(in RectNative rect, byte thirdDimensionAxis, float thirdDimensionCenter, float thirdDimensionHeight)
        {
            switch (thirdDimensionAxis)
            {
                case 0:
                {
                    m_Center = new float3(thirdDimensionCenter, rect.Center);
                    m_Extents = new float3(thirdDimensionHeight * 0.5f, rect.Extents);
                    break;
                }
                case 1:
                {
                    var rectCenter = rect.Center;
                    m_Center = new float3(rectCenter.x, thirdDimensionCenter, rectCenter.y);
                    m_Extents = new float3(rect.Width * 0.5f, thirdDimensionHeight * 0.5f, rect.Height * 0.5f);
                    break;
                }
                default:
                {
                    m_Center = new float3(rect.Center, thirdDimensionCenter);
                    m_Extents = new float3(rect.Extents, thirdDimensionHeight * 0.5f);
                    break;
                }
            }
        }

        // used to allow Bounds to be used as keys in hash tables
        public readonly override int GetHashCode() => Center.GetHashCode() ^ (Extents.GetHashCode() << 2);

        // also required for being able to use Vector4s as keys in hash tables
        public readonly override bool Equals(object other)
        {
            if (!(other is BoundsNative)) return false;

            return Equals((BoundsNative)other);
        }

        public readonly bool Equals(BoundsNative other) => Center.Equals(other.Center) && Extents.Equals(other.Extents);

        // The center of the bounding box.
        public float3 Center { readonly get { return m_Center; } set { m_Center = value; } }

        // The total size of the box. This is always twice as large as the ::ref::extents.
        public float3 Size { readonly get { return m_Extents * 2.0F; } set { m_Extents = value * 0.5F; } }

        // The extents of the box. This is always half of the ::ref::size.
        public float3 Extents { readonly get { return m_Extents; } set { m_Extents = value; } }

        public readonly float ExtentsLength => math.length(Extents);

        // The minimal point of the box. This is always equal to ''center-extents''.
        public float3 Min { readonly get  { return Center - Extents; } set { SetMinMax(value, Max); } }

        // The maximal point of the box. This is always equal to ''center+extents''.
        public float3 Max { readonly get { return Center + Extents; } set { SetMinMax(Min, value); } }

        public readonly float Volume
        {
            get
            {
                var size = Size;
                return size.x * size.y * size.z;
            }
        }

        public readonly float AreaXZ
        {
            get
            {
                var size = Size;
                return size.x * size.z;
            }
        }

        /// <summary> The average of size X and size Z. </summary>
        public readonly float AverageSizeXZ
        {
            get
            {
                var size = Size;
                return (size.x + size.z) * 0.5f;
            }
        }

        //*undoc*
        public static bool operator ==(in BoundsNative lhs, in BoundsNative rhs)
        {
            // Returns false in the presence of NaN values.
            return ((lhs.Center.Equals(rhs.Center)) && lhs.Extents.Equals(rhs.Extents));
        }

        //*undoc*
        public static bool operator !=(in BoundsNative lhs, in BoundsNative rhs)
        {
            // Returns true in the presence of NaN values.
            return !(lhs == rhs);
        }

        //Bottom 4
        /// <summary> X right, Y up, Z for </summary>
        public readonly float3 XMinYMinZMin => Min;
        /// <summary> X right, Y up, Z for </summary>
        public readonly float3 XMinYMinZMax => new float3(Min.xy, Max.z);
        /// <summary> X right, Y up, Z for </summary>
        public readonly float3 XMaxYMinZMin => new float3(Max.x, Min.yz);
        /// <summary> X right, Y up, Z for </summary>
        public readonly float3 XMaxYMinZMax => new float3(Max.x, Min.y, Max.z);
        //Top 4
        /// <summary> X right, Y up, Z for </summary>
        public readonly float3 XMinYMaxZMin => new float3(Min.x, Max.y, Min.z);
        /// <summary> X right, Y up, Z for </summary>
        public readonly float3 XMinYMaxZMax => new float3(Min.x, Max.yz);
        /// <summary> X right, Y up, Z for </summary>
        public readonly float3 XMaxYMaxZMin => new float3(Max.xy, Min.z);
        /// <summary> X right, Y up, Z for </summary>
        public readonly float3 XMaxYMaxZMax => Max;

        // Sets the bounds to the /min/ and /max/ value of the box.
        public void SetMinMax(float3 min, float3 max)
        {
            Extents = (max - min) * 0.5F;
            Center = min + Extents;
        }

        // Grows the Bounds to include the /point/.
        public void Encapsulate(float3 point)
        {
            SetMinMax(math.min(Min, point), math.max(Max, point));
        }

        // Grows the Bounds to include the /Bounds/.
        public void Encapsulate(in BoundsNative bounds)
        {
            Encapsulate(bounds.Center - bounds.Extents);
            Encapsulate(bounds.Center + bounds.Extents);
        }

        // Expand the bounds by increasing its /size/ by /amount/ along each side.
        public void Expand(float amount)
        {
            amount *= .5f;
            Extents += new float3(amount, amount, amount);
        }

        // Expand the bounds by increasing its /size/ by /amount/ along each side.
        public void Expand(float3 amount)
        {
            m_Extents += amount * .5f;
        }

        // Does another bounding box intersect with this bounding box?
        [Pure]
        public readonly bool Intersects(in BoundsNative bounds)
        {
            return (Min.x <= bounds.Max.x) && (Max.x >= bounds.Min.x) &&
                (Min.y <= bounds.Max.y) && (Max.y >= bounds.Min.y) &&
                (Min.z <= bounds.Max.z) && (Max.z >= bounds.Min.z);
        }
        
        [Pure]
        public readonly bool IntersectsXZ(in BoundsNative bounds)
        {
            return (Min.x <= bounds.Max.x) && (Max.x >= bounds.Min.x) &&
                (Min.z <= bounds.Max.z) && (Max.z >= bounds.Min.z);
        }

        [Pure]
        public readonly bool Contains(float3 point) => math.all(math.abs(point - m_Center) < m_Extents);

        [Pure] public readonly bool ContainsXZ(float3 point) => ContainsXZ(point.xz);
        
        [Pure]
        public readonly bool ContainsXZ(float2 point) => math.all(math.abs(point - m_Center.xz) < m_Extents.xz);
        
        [Pure]
        public readonly bool ContainsY(float y) => y >= Min.y && y <= Max.y;

        public readonly float3 ClosestPointTo(float3 point) => math.clamp(point, Min, Max);
        
        /*
         * These calls are internal Unity calls and I don't feel like implementing them
         * 
        public bool IntersectRay(Ray ray) { float dist; return IntersectRayAABB(ray, this, out dist); }
        public bool IntersectRay(Ray ray, out float distance) { return IntersectRayAABB(ray, this, out distance); }
        */

        /// *listonly*
        public readonly override string ToString() => ToString(null, null);

        // Returns a nicely formatted string for the bounds.
        public readonly string ToString(string format) => ToString(format, null);

        public readonly string ToString(string format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
                format = "F2";
            if (formatProvider == null)
                formatProvider = CultureInfo.InvariantCulture.NumberFormat;
            return string.Format("Center: {0}, Extents: {1}", m_Center.ToString(format, formatProvider), m_Extents.ToString(format, formatProvider));
        }
        
        public static implicit operator Bounds(in BoundsNative d) => new Bounds(d.Center, d.Size);
        public static implicit operator BoundsNative(in Bounds d) => new BoundsNative(d.center, d.size);
        
        public void AlineDraw(CommandBuilder draw, Color col, float time)
        {
            draw.PushColor(col);
            if (time > 0.01f)
                draw.PushDuration(time);
            
            draw.WireBox(Center, Size);
            
            draw.PopColor();
            if (time > 0.01f)
                draw.PopDuration();
        }
    }
}