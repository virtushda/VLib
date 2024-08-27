using System.Collections;
using System.Collections.Generic;
using Drawing;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace VLib
{
    /// <summary>
    /// Rect struct optimized for mathematics/burst.
    /// Can be used in place of Rect or RectInt.
    /// Can cast to and from: Rect, RectInt, float4
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public struct RectNative : IConvex<float2>
    {
        public static implicit operator Rect(RectNative r) => new Rect(r.corners.xy, r.corners.zw - r.corners.xy);
        public static implicit operator RectNative(Rect r) => new RectNative(new float4(r.min, r.max));

        public static explicit operator RectInt(RectNative r) => new RectInt(new Vector2Int((int)(r.corners.x + .5f), (int)(r.corners.y + .5f)), new Vector2Int((int)(r.corners.z - r.corners.x + .5f), (int)(r.corners.w - r.corners.y + .5f)));
        public static implicit operator RectNative(RectInt r) => new RectNative(new float4((Vector2)r.min, (Vector2)r.max));

        public static explicit operator RectNative(float4 r) => new RectNative(r);
        public static implicit operator float4(RectNative r) => r.corners;

        public readonly bool Equals(in RectNative other) => corners.Equals(other.corners);
        public readonly override bool Equals(object obj) => obj is RectNative other && Equals(other);
        public readonly override int GetHashCode() => corners.GetHashCode();
        
        public static bool operator ==(RectNative r1, RectNative r2) => math.all(r1.corners == r2.corners);
        public static bool operator !=(RectNative r1, RectNative r2) => !math.all(r1.corners == r2.corners);

        public float4 corners;

        #region Constructors

        /// <summary>
        /// </summary>
        /// <param name="corners">XY: BottomLeft, ZW: TopRight</param>
        public RectNative(float4 corners)
        {
            this.corners = corners;
        }

        public RectNative(float2 center, float2 size)
        {
            float2 halfSize = size * .5f;
            float2 bottomLeft = center - halfSize;
            float2 topRight = center + halfSize;
            
            corners = new float4(bottomLeft, topRight);
        }

        public RectNative(float2 point)
        {
            corners = float4.zero;
            SetToPoint(point);
        }

        public RectNative(float minX, float minY, float sizeX, float sizeY)
        {
            float2 minXY = new float2(minX, minY);
            float2 maxXY = minXY + new float2(sizeX, sizeY);
            corners = new float4(minXY, maxXY);
        }

        public RectNative(float3 boundsMin, float3 boundsMax, Axis axisA, Axis axisB)
        {
            corners = new float4
            (boundsMin[(int)axisA], boundsMin[(int)axisB],
                boundsMax[(int)axisA], boundsMax[(int)axisB]);
        }
        
        public static RectNative FromMinMax(float2 min, float2 max) => new(new float4(min, max));

        #endregion

        #region Properties

        public readonly float Width => corners.z - corners.x;
        public readonly int WidthInt => (int)Width;
        public readonly int WidthRoundInt => (int)(Width + .5f);

        public readonly float Height => corners.w - corners.y;
        public readonly int HeightInt => (int)Height;
        public readonly int HeightRoundInt => (int)(Height + .5f);

        public float2 Size
        {
            readonly get => corners.zw - corners.xy;
            set
            {
                //Remove offset, scale, add offset
                float2 center = Center;
                float2 size = Size;
                corners.xy -= center;
                corners.zw -= center;

                float2 sizeMult = value / math.max(math.abs(size), 0.00001f);
                corners.xy *= sizeMult;
                corners.zw *= sizeMult;

                corners.xy += center;
                corners.zw += center;
            }
        }
        public readonly int2 SizeInt => (int2)Size;
        public readonly int2 SizeRoundInt => (int2)(Size + .5f);

        public float2 Extents
        {
            readonly get => Size * .5f;
            set => Size = value * 2;
        }

        public readonly float Area
        {
            get
            {
                var size = corners.zw - corners.xy;
                return size.x * size.y;
            }
        }

        public readonly int CountInt
        {
            get
            {
                int2 widthHeightInt = (int2)Size;
                return widthHeightInt.x * widthHeightInt.y;
            }
        }

        public float2 Min { readonly get => corners.xy; set => corners.xy = value; }
        public readonly int2 MinInt => (int2)Min;
        public readonly int2 MinRoundInt => (int2)(Min + .5f);

        public float2 Max { readonly get => corners.zw; set => corners.zw = value; }
        public readonly int2 MaxInt => (int2)Max;
        public readonly int2 MaxRoundInt => (int2)(Max + .5f);
        public readonly int2 MaxCeilInt => (int2) math.ceil(Max);

        public float2 Center
        {
            readonly get => (corners.xy + corners.zw) * .5f;
            set
            {
                float2 offset = value - Center;
                corners.xy += offset;
                corners.zw += offset;
            }
        }
        public readonly int2 CenterInt => (int2)Center;
        public readonly int2 CenterRoundInt => (int2)(Center + .5f);

        public readonly float2 CornerBL => corners.xy;
        public readonly float2 CornerBR => corners.zy;
        public readonly float2 CornerTL => corners.xw;
        public readonly float2 CornerTR => corners.zw;

        public readonly float2 BL_To_TL => CornerTL - CornerBL;
        public readonly float2 TL_To_TR => CornerTR - CornerTL;
        public readonly float2 TR_To_BR => CornerBR - CornerTR;
        public readonly float2 BR_To_BL => CornerBL - CornerBR;

        public readonly float2x2 SideBottom => new (CornerBL, CornerBR);
        public readonly float2x2 SideTop => new (CornerTL, CornerTR);
        public readonly float2x2 SideLeft => new (CornerBL, CornerTL);
        public readonly float2x2 SideRight => new (CornerBR, CornerBR);

        #endregion
        
        #region IConvex Impl

        public readonly int Sides => 4;
        
        public readonly float2 GetVertex(int index)
        {
            switch (index)
            {
                case 0: return CornerBL;
                case 1: return CornerTL;
                case 2: return CornerTR;
                case 3: return CornerBR;
                default: return 0;
            }
        }

        public readonly float2 GetSide(int index)
        {
            switch (index)
            {
                case 0: return BL_To_TL;
                case 1: return TL_To_TR;
                case 2: return TR_To_BR;
                case 3: return BR_To_BL;
                default: return 0;
            }
        }

        public readonly float2 GetNormal(int index)
        {
            switch (index)
            {
                case 0: return new float2(-1, 0);
                case 1: return new float2(0, 1);
                case 2: return new float2(1, 0);
                case 3: return new float2(0, -1);
                default: return 0;
            }
        }
        
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RectNative SetToPoint(float2 point)
        {
            corners = point.xyxy;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int2 LocalToWorldCoord(int2 localCoord) => localCoord + MinInt;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float2 LocalToWorldCoord(float2 localCoord) => localCoord + Min;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int2 WorldToLocalCoord(int2 worldCoord) => worldCoord - MinInt;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float2 WorldToLocalCoord(float2 worldCoord) => worldCoord - Min;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RectNative Encapsulate(float2 point)
        {
            corners.xy = math.min(corners.xy, point);
            corners.zw = math.max(corners.zw, point);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RectNative Encapsulate(RectNative rectNative)
        {
            corners.xy = math.min(corners.xy, rectNative.corners.xy);
            corners.zw = math.max(corners.zw, rectNative.corners.zw);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Contains(float2 point)
        {
            bool2 validXY = new bool2
                (point.x >= corners.x && point.x <= corners.z,
                point.y >= corners.y && point.y <= corners.w);

            return math.all(validXY);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool2 ContainsXY(float2 point)
        {
            return math.abs(point - Center) <= Extents;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Contains(float2 point, float margin)
        {
            bool4 validXY = new bool4(point >= corners.xy + margin, point <= corners.zw - margin);

            return math.all(validXY);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Contains(RectNative otherRect) => math.all(new bool4(otherRect.Min >= corners.xy, otherRect.Max <= corners.zw));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RectNative ClampTo(in float4 clampCorners)
        {
            corners.xy = ClampPointInternal(corners.xy, clampCorners);
            corners.zw = ClampPointInternal(corners.zw, clampCorners);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float2 ClampPoint(float2 pointToClamp2) => ClampPointInternal(pointToClamp2, corners);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float2 ClampPointInternal(float2 point, float4 sourceCorners)
        {
            point = math.max(point, sourceCorners.xy);
            point = math.min(point, sourceCorners.zw);
            return point;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float2 ClampPointMargin(float2 pointToClamp2, float extraMargin)
        {
            pointToClamp2 = math.max(pointToClamp2, corners.xy + extraMargin);
            pointToClamp2 = math.min(pointToClamp2, corners.zw - extraMargin);
            return pointToClamp2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float3 ClampPointXZ(float3 pointToClamp3, float extraMargin)
        {
            float2 clampedXZ = ClampPointMargin(pointToClamp3.xz, extraMargin);
            return new float3(clampedXZ.x, pointToClamp3.y, clampedXZ.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Overlaps(RectNative otherRect)
        {
            return math.all(corners.xy <= otherRect.corners.zw) && math.all(corners.zw >= otherRect.corners.xy);
        }

        public readonly (float2 overlapXY, float overlapArea) OverlapArea(in RectNative otherRect)
        {
            float2 minClamped = otherRect.ClampPoint(corners.xy);
            float2 maxClamped = otherRect.ClampPoint(corners.zw);
            float2 overlapXY = maxClamped - minClamped;
            return (overlapXY, overlapXY.x * overlapXY.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly RectNative GetExpanded(float expansion) => new(new float4(corners.xy - expansion, corners.zw + expansion));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Expand(float expansion)
        {
            corners.xy -= expansion;
            corners.zw += expansion;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExpandInt(float expansion)
        {
            Expand(expansion);

            corners.xy = (int2)corners.xy;
            corners.zw = (int2)(corners.zw + .99999f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RectNative EnsureValid(float minSize = 0.005f)
        {
            float2 avgPoint = (corners.xy + corners.zw) * .5f;
            float2 minSizeOffset = new float2(minSize);

            float2 avgPointXY = avgPoint - minSizeOffset;
            float2 avgPointZW = avgPoint + minSizeOffset;

            corners.xy = math.min(corners.xy, avgPointXY);
            corners.zw = math.max(corners.zw, avgPointZW);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly RectNative GetLocalizedMinToZero()
        {
            float2 min = Min;
            return (RectNative)(corners - new float4(min, min));
        }

        /// <summary>
        /// Change the 'cell size' of a rect. Useful when taking a bounds on one grid, and getting the same bounds on a different resolution grid.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly RectNative GetWithShiftedResolution(float2 currentCellSize, float2 newCellSize)
        {
            float2 cellSizeMult = currentCellSize / newCellSize;
            return new RectNative(corners * cellSizeMult.xyxy);
        }

        /// <summary> Extends the rect in all directions until both corners are whole numbers </summary> 
        public readonly RectNative GetExpandedCeilToInt() => new RectNative(new float4(math.floor(corners.xy), math.ceil(corners.zw)));

        public readonly (float2 centerPointF, int2 centerPoint, float radius) GetFitCircle()
        {
            return (Center,
                    CenterInt,
                    math.cmin(Size) * .5f);
        }

        /// <param name="circleXYR">Circle.xy = 2D Position, Circle.z = Radius</param>
        public readonly ShapeOverlap TestCircleOverlap(float3 circleXYR)
        {
            float2 extents = Extents;
            float2 exteriorMargins = extents + circleXYR.z;
            float2 distRectCenterToCircCenter = circleXYR.xy - Center;
            float2 distRectCenterToCircCenterAbs = math.abs(distRectCenterToCircCenter);
            
            //Fast Reject Outside
            if (math.any(distRectCenterToCircCenterAbs > exteriorMargins))
                return ShapeOverlap.Outside;
            
            //Check if completely inside rect
            //Get Area that Circle Center MUST be inside for full containment
            float2 interiorMargins = math.max(float2.zero, extents - circleXYR.z);
            if (math.all(new bool4(distRectCenterToCircCenter <= interiorMargins, distRectCenterToCircCenter >= -interiorMargins)))
                return ShapeOverlap.Inside;
            
            //Corner Check for Accuracy, but last because it's rare case
            float2 cornerToCircleCenter = distRectCenterToCircCenterAbs - extents;
            //Point must be closest to a corner and NOT a side, sides are already checked in above cases
            if (math.all(math.sign(cornerToCircleCenter) >= 0))
            {
                //Check that point is outside corner
                if (math.lengthsq(cornerToCircleCenter) > circleXYR.z * circleXYR.z)
                    return ShapeOverlap.Outside;
            }

            return ShapeOverlap.Intersecting;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly (int2x4 points_BL_BR_TL_TR, float2 weightsXY) GetBilinearSampleData(float2 coord, bool rectSpace = true)
        {
            return VMath.ComputeBilinearSampleData(coord, MinInt, MaxInt);
        }
        
        /// <summary>Automatically bilinear sample from data that is 'Lerpable'</summary>
        /// <param name="coord">Rect or World Coord to Sample</param>
        /// <param name="valueArray">Data to sample from</param>
        /// <param name="rectSpace">True = Translate coord from "world-space" into "rect-space". False = Use coord as-is.</param>
        /// <typeparam name="T">Lerpable Type (ie: LFloat3x2)</typeparam>
        /// <typeparam name="U">Lerpable's Internal Type (ie: float3x2)</typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly U BilinearSampleWithData<T, U>(float2 coord, NativeArray<T> valueArray, bool rectSpace = true)
            where T : struct, ILerpable<U>
            where U : struct
        {
            var (pointsBLBRTLTR, weightsXY) = GetBilinearSampleData(coord);

            //Compute Indices
            int width = WidthInt;
            int2 min = MinInt;
            int4 coordsX4 = new int4(pointsBLBRTLTR.c0.x, pointsBLBRTLTR.c1.x, pointsBLBRTLTR.c2.x, pointsBLBRTLTR.c3.x);
            int4 coordsY4 = new int4(pointsBLBRTLTR.c0.y, pointsBLBRTLTR.c1.y, pointsBLBRTLTR.c2.y, pointsBLBRTLTR.c3.y);
            
            //Shift into rect space, clamp
            if (rectSpace)
            {
                coordsX4 -= min.x;
                coordsY4 -= min.y;
                //Clamp Coordinates to Valid (Prevents Crashy Errors from Bad Coords...)
                int2 maxRectSpaceCoord = SizeInt - 1;
                coordsX4 = math.clamp(coordsX4, int4.zero, maxRectSpaceCoord.xxxx);
                coordsY4 = math.clamp(coordsY4, int4.zero, maxRectSpaceCoord.yyyy);
            }
            else
            {
                var max = MaxRoundInt - 1; //max with base coord 0 for indexing
                //Clamp Coordinates to Valid
                coordsX4 = math.clamp(coordsX4, min.xxxx, max.xxxx);
                coordsY4 = math.clamp(coordsY4, min.yyyy, max.yyyy);
            }
            
            int4 indices = coordsX4 + coordsY4 * width;

            //Read Values
            var valueBL = valueArray[indices[0]];
            var valueBR = valueArray[indices[1]];
            var valueTL = valueArray[indices[2]];
            var valueTR = valueArray[indices[3]];

            return VMath.Blerp<T, U>(valueBL, valueBR, valueTL, valueTR, weightsXY.x, weightsXY.y);
        }
        
        public RectIntEnumerator GetIntEnumerable(bool inclusive = false) => new(this, inclusive);

        /*public readonly struct RectIntEnumerable : IEnumerable<int2>
        {
            readonly RectNative rectCopy;
            
            
        }*/

        public struct RectIntEnumerator : IEnumerable<int2>, IEnumerator<int2>
        {
            RectNative rectCopy;
            int minX;
            int2 endXY;
            
            int2 current;
            
            public RectIntEnumerator(RectNative rectCopy, bool inclusive)
            {
                this.rectCopy = rectCopy;
                
                current = rectCopy.MinInt;
                minX = current.x;
                endXY = rectCopy.MaxInt;
                
                if (inclusive)
                    ++endXY;
            }
            
            public bool MoveNext()
            {
                ++current.x;
                // Fast exit if we're still in the same row
                if (current.x < endXY.x)
                    return true;
                
                // Move to next row
                current.x = minX;
                ++current.y;

                // Continue if this row is still within bounds
                return current.y < endXY.y;
            }

            public void Reset() => current = rectCopy.MinInt;

            public int2 Current => current;
            object IEnumerator.Current => Current;

            public void Dispose() { }
            
            public RectIntEnumerator GetEnumerator() => new(rectCopy, false);

            IEnumerator<int2> IEnumerable<int2>.GetEnumerator() => throw new System.NotImplementedException();
            IEnumerator IEnumerable.GetEnumerator() => throw new System.NotImplementedException();
        }

#if UNITY_EDITOR
    /// <summary>
    /// Allows you to map the 2D rect to 3D axes.
    /// </summary>
    /// <param name="c"></param>
    /// <param name="axisX">Map .X on vec2 to .[this] on vec3. </param>
    /// <param name="axisY">Map .Y on vec2 to .[this] on vec3.</param>
    /// <param name="axisZ">Map 'thirdValue' to .[this] on vec3.</param>
    /// <param name="thirdValue">Whatever the third axis "axisZ" is set to, this is the value to use for it.</param>
    /// <param name="duration"></param>
        public void EditorOnlyDebugDraw(Color c, Axis axisX, Axis axisY, Axis axisZ, float thirdValue, float duration)
        {
            int x = (int)axisX;
            int y = (int)axisY;
            int z = (int)axisZ;

            Debug.DrawLine(CornerBL.ToFloat3(x, y, z, thirdValue), CornerBR.ToFloat3(x, y, z, thirdValue), c, duration);
            Debug.DrawLine(CornerBR.ToFloat3(x, y, z, thirdValue), CornerTR.ToFloat3(x, y, z, thirdValue), c, duration);
            Debug.DrawLine(CornerTR.ToFloat3(x, y, z, thirdValue), CornerTL.ToFloat3(x, y, z, thirdValue), c, duration);
            Debug.DrawLine(CornerTL.ToFloat3(x, y, z, thirdValue), CornerBL.ToFloat3(x, y, z, thirdValue), c, duration);
        }
#endif

        public void DebugDraw(Color c, Axis axisX, Axis axisY, Axis axisZ, float thirdValue, CommandBuilder draw, float duration = 0)
        {
            int x = (int)axisX;
            int y = (int)axisY;
            int z = (int)axisZ;

            if (duration > .01f)
                draw.PushDuration(duration);
            draw.Line(CornerBL.ToFloat3(x, y, z, thirdValue), CornerBR.ToFloat3(x, y, z, thirdValue), c);
            draw.Line(CornerBR.ToFloat3(x, y, z, thirdValue), CornerTR.ToFloat3(x, y, z, thirdValue), c);
            draw.Line(CornerTR.ToFloat3(x, y, z, thirdValue), CornerTL.ToFloat3(x, y, z, thirdValue), c);
            draw.Line(CornerTL.ToFloat3(x, y, z, thirdValue), CornerBL.ToFloat3(x, y, z, thirdValue), c);
            if (duration > .01f)
                draw.PopDuration();
        }
    }
}