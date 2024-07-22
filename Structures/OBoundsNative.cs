using System;
using System.Runtime.CompilerServices;
using Drawing;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace VLib
{
    /// <summary>
    /// Oriented Bounding Box implemented as a World-Space Axis-Aligned Bounding Box with a Matrix to orient it.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public struct OBoundsNative : IEquatable<OBoundsNative>
    {
        private BoundsNative localAABB;

        private float4x4 matrixW2L;
        private float4x4 matrixL2W;

        #region Properties
        public Bounds LocalBounds 
        { 
            readonly get => localAABB; 
            set => localAABB = value; 
        }

        public float4x4 Matrix
        {
            readonly get => matrixW2L;
            set
            {
                matrixW2L = value;
                matrixL2W = math.inverse(value);
            }
        }
        public readonly float4x4 InverseMatrix => matrixL2W;

        public float3 Center
        {
            readonly get => TransformPointL2W(LocalCenter); 
            set => localAABB.Center = TransformPointW2L(value);
        }
        public readonly float3 LocalCenter => localAABB.Center;
        public float3 LocalSize 
        { 
            readonly get => localAABB.Size; 
            set => localAABB.Size = value;
        }
        public float3 LocalExtents 
        { 
            readonly get => LocalSize * .5f;
            set => LocalSize = value * 2.0f;
        }
        public float3 LocalMin { readonly get => localAABB.Min; set => localAABB.Min = value; }
        public float3 LocalMax { readonly get => localAABB.Max; set => localAABB.Max = value; }

        /// <summary>
        /// Top left of bounding box with Y=Max
        /// </summary>
        public readonly float3 TopTL => TransformPointL2W(new float3(LocalMin.x, LocalMax.y, LocalMax.z));

        /// <summary>
        /// Top right of bounding box with Y=Max
        /// </summary>
        public readonly float3 TopTR => TransformPointL2W(LocalMax);

        /// <summary>
        /// Bottom right of bounding box with Y=Max
        /// </summary>
        public readonly float3 TopBR => TransformPointL2W(new float3(LocalMax.x, LocalMax.y, LocalMin.z));

        /// <summary>
        /// Bottom left of bounding box with Y=Max
        /// </summary>
        public readonly float3 TopBL => TransformPointL2W(new float3(LocalMin.x, LocalMax.y, LocalMin.z));

        /// <summary>
        /// Top left of bounding box with Y=Min
        /// </summary>
        public readonly float3 BottomTL => TransformPointL2W(new float3(LocalMin.x, LocalMin.y, LocalMax.z));

        /// <summary>
        /// Top right of bounding box with Y=Min
        /// </summary>
        public readonly float3 BottomTR => TransformPointL2W(new float3(LocalMax.x, LocalMin.y, LocalMax.z));

        /// <summary>
        /// Bottom right of bounding box with Y=Min
        /// </summary>
        public readonly float3 BottomBR => TransformPointL2W(new float3(LocalMax.x, LocalMin.y, LocalMin.z));

        /// <summary>
        /// Bottom left of bounding box with Y=Min
        /// </summary>
        public readonly float3 BottomBL => TransformPointL2W(LocalMin);

        /// <summary>
        /// Top left of bounding box with Y=0
        /// </summary>
        public readonly float3 LocalTL => TransformPointL2W(new float3(LocalMin.x, LocalCenter.y, LocalMax.z));

        /// <summary>
        /// Top right of bounding box with Y=0
        /// </summary>
        public readonly float3 LocalTR => TransformPointL2W(new float3(LocalMax.x, LocalCenter.y, LocalMax.z));

        /// <summary>
        /// Bottom right of bounding box with Y=0
        /// </summary>
        public readonly float3 LocalBR => TransformPointL2W(new float3(LocalMax.x, LocalCenter.y, LocalMin.z));

        /// <summary>
        /// Bottom left of bounding box with Y=0
        /// </summary>
        public readonly float3 LocalBL => TransformPointL2W(new float3(LocalMin.x, LocalCenter.y, LocalMin.z));

        public readonly float3 Forward => TransformDirL2W(new float3(0, 0, 1));
        public readonly float3 Up => TransformDirL2W(new float3(0, 1, 0));
        public readonly float3 Right => TransformDirL2W(new float3(1, 0, 0));
        public readonly quaternion Rotation => matrixL2W.RotationDelta();

        #endregion

        #region Methods
        
        /// <summary> Create an oriented bounding box from an axis-aligned bounding box and a matrix. </summary>
        /// <param name="boundsSpace">What space is the AABB in? If world, we'll transform it's center with the input matrix from world -> local.</param>
        /// <param name="matrixW2L">Matrix (W2L) defining the orientation of the local bounds FROM the obounds. (Inverse typical transform matrix)</param>
        public OBoundsNative(in Bounds aabb, Space boundsSpace, in Matrix4x4 matrixW2L)
        {
            localAABB = aabb;
            this.matrixW2L = matrixW2L;
            matrixL2W = math.inverse(matrixW2L);

            if (boundsSpace == Space.World)
                localAABB.Center = TransformPointW2L(localAABB.Center);
        }
        
        /// <summary> Create an oriented bounding box from an axis-aligned bounding box and a matrix. </summary>
        /// <param name="boundsSpace">What space is the AABB in? If world, we'll transform it's center with the input matrix from world -> local.</param>
        /// <param name="matrixW2L">Matrix (W2L) defining the orientation of the local bounds FROM the obounds. (Inverse typical transform matrix)</param>
        public OBoundsNative(in BoundsNative aabb, Space boundsSpace, in float4x4 matrixL2W, in float4x4 matrixW2L)
        {
            localAABB = aabb;
            this.matrixW2L = matrixW2L;
            this.matrixL2W = matrixL2W;

            if (boundsSpace == Space.World)
                localAABB.Center = TransformPointW2L(localAABB.Center);
        }

        public OBoundsNative(BoxCollider col, float expandBy = 0f)
        {
            localAABB = new Bounds(col.center, col.size);
            if (expandBy > 0f)
                localAABB.Expand(expandBy);

            matrixW2L = col.transform.worldToLocalMatrix;
            matrixL2W = math.inverse(matrixW2L);
        }

        public OBoundsNative(Transform objTransform, Mesh mesh)
        {
            localAABB = mesh.bounds;

            matrixW2L = objTransform.worldToLocalMatrix;
            matrixL2W = math.inverse(matrixW2L);
        }

        public OBoundsNative(in float4x4 matrixL2W, in Bounds lAABB)
        {
            localAABB = lAABB;

            matrixW2L = math.inverse(matrixL2W);
            this.matrixL2W = matrixL2W;
        }

        public OBoundsNative(Transform objTransform)
        {
            localAABB = new Bounds(Vector3.zero, Vector3.zero);

            matrixW2L = objTransform.worldToLocalMatrix;
            matrixL2W = math.inverse(matrixW2L);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Contains(float3 worldPos) => localAABB.Contains(TransformPointW2L(worldPos));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <summary>
        /// Infinite projection check
        /// </summary>
        public readonly bool ContainsXZ(float3 worldPos)
        {
            var lpos = (worldPos);
            lpos.y = localAABB.Center.y;
            if (localAABB.Contains(lpos))
                return true;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Encapsulate(float3 worldPos)
        {
            localAABB.Encapsulate(TransformPointW2L(worldPos));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Encapsulate(in OBoundsNative orientedBounds)
        {
            //Bottom 4 points
            Encapsulate(orientedBounds.BottomBL);
            Encapsulate(orientedBounds.BottomBR);
            Encapsulate(orientedBounds.BottomTL);
            Encapsulate(orientedBounds.BottomTR);
            //Top 4 points
            Encapsulate(orientedBounds.TopBL);
            Encapsulate(orientedBounds.TopBR);
            Encapsulate(orientedBounds.TopTL);
            Encapsulate(orientedBounds.TopTR);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly BoundsNative EncapsulatingAABB()
        {
            BoundsNative b = new BoundsNative(TopTL, Vector3.zero);
            b.Encapsulate(TopTR);
            b.Encapsulate(TopBR);
            b.Encapsulate(TopBL);

            b.Encapsulate(BottomTL);
            b.Encapsulate(BottomTR);
            b.Encapsulate(BottomBR);
            b.Encapsulate(BottomBL);

            return b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float3 TransformPointW2L(float3 worldPoint) => math.transform(matrixW2L, worldPoint);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float3 TransformPointL2W(float3 localPoint) => math.transform(matrixL2W, localPoint);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float3 TransformDirW2L(float3 worldDir) => math.rotate(matrixW2L, worldDir);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float3 TransformDirL2W(float3 localDir) => math.rotate(matrixL2W, localDir);

        /// <returns>XYZ: Circle viewport center, W: Radius</returns>
        [GenerateTestsForBurstCompatibility]
        public readonly float4 ProjectToEncapsulatingViewportCircle(in float4x4 worldToCamMatrix, in float4x4 projectionMatrix, in float3x3 cameraDirs, out float3 topTRWorld)
        {
            //Get two world-space comparison points so that matrix scale is accounted for
            var centerPoint = Center;
            topTRWorld = TopTR;
            float maxBoundsSize = math.distance(centerPoint, topTRWorld);
            var offsetPoint = centerPoint + cameraDirs.c1 * maxBoundsSize;

            float3 centerView = CameraUtils.WorldToViewportPointNative(centerPoint, worldToCamMatrix, projectionMatrix);
            float3 offsetView = CameraUtils.WorldToViewportPointNative(offsetPoint, worldToCamMatrix, projectionMatrix);

            return new float4(centerView, math.distance(centerView, offsetView));
        }
        
        [GenerateTestsForBurstCompatibility]
        public readonly ShapeOverlap TestAgainstCameraRectNative(in float4x4 worldToCamMatrix, in float4x4 projectionMatrix, in RectNative viewportRect, in float3x3 cameraDirsXYZ, 
                                                                out RectNative boundsViewRect, float cullBehind = 1)
        {
            boundsViewRect = default;
            //Project circle to screen as FAST check
            var viewportCircle = ProjectToEncapsulatingViewportCircle(worldToCamMatrix, projectionMatrix, cameraDirsXYZ, out var topTRCached);
            
            //Check the circle to see if it breaches the near culling plane
            float circleCenterFromCullPlane = viewportCircle.z - cullBehind;
            //Circle Completely Behind Cull Plane
            if (circleCenterFromCullPlane < -viewportCircle.w)
                return ShapeOverlap.Outside;

            var circleOverlap = viewportRect.TestCircleOverlap(new float3(viewportCircle.xy, viewportCircle.w));

            if (circleOverlap == ShapeOverlap.Inside)
                return ShapeOverlap.Inside;
            else if (circleOverlap == ShapeOverlap.Outside)
                return ShapeOverlap.Outside;
            
            //Get Viewport Points
            var vp0 = CameraUtils.WorldToViewportPointNative(BottomBL, worldToCamMatrix, projectionMatrix);
            var vp1 = CameraUtils.WorldToViewportPointNative(BottomBR, worldToCamMatrix, projectionMatrix);
            var vp2 = CameraUtils.WorldToViewportPointNative(BottomTL, worldToCamMatrix, projectionMatrix);
            var vp3 = CameraUtils.WorldToViewportPointNative(BottomTR, worldToCamMatrix, projectionMatrix);
            var vp4 = CameraUtils.WorldToViewportPointNative(TopBL, worldToCamMatrix, projectionMatrix);
            var vp5 = CameraUtils.WorldToViewportPointNative(TopBR, worldToCamMatrix, projectionMatrix);
            var vp6 = CameraUtils.WorldToViewportPointNative(TopTL, worldToCamMatrix, projectionMatrix);
            var vp7 = CameraUtils.WorldToViewportPointNative(topTRCached, worldToCamMatrix, projectionMatrix); //Reuse computed value

            //Cull behind camera
            float4 z03 = new float4(vp0.z, vp1.z, vp2.z, vp3.z);
            float4 z47 = new float4(vp4.z, vp5.z, vp6.z, vp7.z);
            if (math.cmin(z03) < cullBehind && math.cmin(z47) < cullBehind)
                return ShapeOverlap.Outside;
            
            //Construct Rect Around Bounds Viewport Points
            boundsViewRect = new RectNative(vp0.xy);
            boundsViewRect.Encapsulate(vp1.xy);
            boundsViewRect.Encapsulate(vp2.xy);
            boundsViewRect.Encapsulate(vp3.xy);
            boundsViewRect.Encapsulate(vp4.xy);
            boundsViewRect.Encapsulate(vp5.xy);
            boundsViewRect.Encapsulate(vp6.xy);
            boundsViewRect.Encapsulate(vp7.xy);

            //Compare with rect
            if (viewportRect.Contains(boundsViewRect))
                return ShapeOverlap.Inside;
            return viewportRect.Overlaps(boundsViewRect) ? ShapeOverlap.Intersecting : ShapeOverlap.Outside;
        }
        
        /// <summary>Returns 0f if the point is inside the bounds</summary>
        [GenerateTestsForBurstCompatibility]
        public readonly float DistanceToPosition(float3 position)
        {
            var localPos = math.transform(Matrix, position);
            var distX = math.max(0f, math.abs(localPos.x) - LocalExtents.x);
            var distY = math.max(0f, math.abs(localPos.y) - LocalExtents.y);
            var distZ = math.max(0f, math.abs(localPos.z) - LocalExtents.z);
            return math.length(new float3(distX, distY, distZ));
        }

        [BurstDiscard]
        public void DebugDraw(Color col, float time)
        {
            //Bottom face
            Debug.DrawLine(BottomTL, BottomTR, col, time);
            Debug.DrawLine(BottomTR, BottomBR, col, time);
            Debug.DrawLine(BottomBR, BottomBL, col, time);
            Debug.DrawLine(BottomBL, BottomTL, col, time);

            //Top face
            Debug.DrawLine(TopTL, TopTR, col, time);
            Debug.DrawLine(TopTR, TopBR, col, time);
            Debug.DrawLine(TopBR, TopBL, col, time);
            Debug.DrawLine(TopBL, TopTL, col, time);

            //vertical edges
            Debug.DrawLine(TopTL, BottomTL, col, time);
            Debug.DrawLine(TopTR, BottomTR, col, time);
            Debug.DrawLine(TopBL, BottomBL, col, time);
            Debug.DrawLine(TopBR, BottomBR, col, time);
        }
        
        public void AlineDraw(CommandBuilder draw, Color col, float time)
        {
            draw.PushColor(col);
            if (time > 0.01f)
                draw.PushDuration(time);
            
            //Bottom face
            draw.Line(BottomTL, BottomTR);
            draw.Line(BottomTR, BottomBR);
            draw.Line(BottomBR, BottomBL);
            draw.Line(BottomBL, BottomTL);

            //Top face
            draw.Line(TopTL, TopTR);
            draw.Line(TopTR, TopBR);
            draw.Line(TopBR, TopBL);
            draw.Line(TopBL, TopTL);

            //vertical edges
            draw.Line(TopTL, BottomTL);
            draw.Line(TopTR, BottomTR);
            draw.Line(TopBL, BottomBL);
            draw.Line(TopBR, BottomBR);
            
            draw.PopColor();
            if (time > 0.01f)
                draw.PopDuration();
        }
        #endregion

        public readonly bool Equals(OBoundsNative other) => localAABB.Equals(other.localAABB) && matrixL2W.Equals(other.matrixL2W);
        public readonly override bool Equals(object obj) => obj is OBoundsNative other && Equals(other);

        public readonly override int GetHashCode()
        {
            unchecked
            {
                return (localAABB.GetHashCode() * 397) ^ matrixL2W.GetHashCode();
            }
        }
        
        public static bool operator ==(in OBoundsNative left, in OBoundsNative right) => left.Equals(right);
        public static bool operator !=(in OBoundsNative left, in OBoundsNative right) => !(left == right);
    }
}