using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace VLib
{
    /// <summary>
    /// Oriented rect struct optimized for mathematics/burst.
    /// Oriented on XZ Plane, Rotates around Y.
    /// Center Position of aligned rect should be ZERO!
    /// </summary>
    public struct ORectNative
    {
        #region Operators Vars & Properties
        
        public static bool operator ==(ORectNative r1, ORectNative r2) => math.all(r1.LocalCorners == r2.LocalCorners) && r1.matrixL2W.Equals(r2.matrixL2W);
        public static bool operator !=(ORectNative r1, ORectNative r2) => !math.all(r1.LocalCorners == r2.LocalCorners) || !r1.matrixL2W.Equals(r2.matrixL2W);

        /// <summary>
        /// XY: BottomLeft, ZW: TopRight
        /// </summary>
        RectNative localAlignedRect;
        /// <summary>
        /// Abusing a 3d matrix to do 2d work, woops!
        /// </summary>
        float4x4 matrixL2W;
        float4x4 matrixW2L;

        public RectNative InternalAlignedRect => localAlignedRect;
        public float4 LocalCorners { get => localAlignedRect.corners; set => localAlignedRect.corners = value; }
        public float4x4 Matrix_L2W => matrixL2W;
        public float4x4 MatrixInv_W2L => matrixW2L;

        public ORectNative(RectNative rect, float2 upDir)
        {
            var (matrix, matrixInv) = MatricesFromUpDir(rect.Center, upDir);
            matrixL2W = matrix;
            matrixW2L = matrixInv;
            
            rect.Center = float2.zero;
            localAlignedRect = rect;
        }

        public ORectNative(float2 bottomL, float2 bottomR, float2 topL, float2 topR)
        {
            float2 upVec = topL - bottomL;
            float2 center = (bottomL + bottomR + topL + topR) * .25f;
            
            var (matrix, matrixInv) = MatricesFromUpDir(center, upVec);
            matrixL2W = matrix;
            matrixW2L = matrixInv;

            localAlignedRect = new RectNative(float2.zero, new float2(math.distance(bottomR, bottomL), math.length(upVec)));
        }

        public ORectNative(OBoundsNative orientedBounds, float2 rectUpDir, Axis projAxisA, Axis projAxisB)
        {
            //this.rectUpDir = math.normalizesafe(rectUpDir);

            int projA = (int)projAxisA;
            int projB = (int)projAxisB;
            
            float2 center = new float2(orientedBounds.Center[projA], orientedBounds.Center[projB]);
            var (matrix, matrixInv) = MatricesFromUpDir(center, rectUpDir);
            matrixL2W = matrix;
            matrixW2L = matrixInv;

            //Establish Rect
            localAlignedRect = new RectNative(float2.zero);

            //Expand, Get World Bound Points, Encapsulate
            var BBL = orientedBounds.BottomBL;
            var BBR = orientedBounds.BottomBR;
            var BTL = orientedBounds.BottomTL;
            var BTR = orientedBounds.BottomTR;
            var TBL = orientedBounds.TopBL;
            var TBR = orientedBounds.TopBR;
            var TTL = orientedBounds.TopTL;
            var TTR = orientedBounds.TopTR;

            Encapsulate(new float2(BBL[projA], BBL[projB]));
            Encapsulate(new float2(BBR[projA], BBR[projB]));
            Encapsulate(new float2(BTL[projA], BTL[projB]));
            Encapsulate(new float2(BTR[projA], BTR[projB]));
            Encapsulate(new float2(TBL[projA], TBL[projB]));
            Encapsulate(new float2(TBR[projA], TBR[projB]));
            Encapsulate(new float2(TTL[projA], TTL[projB]));
            Encapsulate(new float2(TTR[projA], TTR[projB]));

            //localAlignedRect.EditorOnlyDebugDraw(Color.yellow, Axis.X, Axis.Z, Axis.Y, orientedBounds.Center.y, 0);
        }

        public ORectNative(ORectNative rectToCopy)
        {
            localAlignedRect = rectToCopy.localAlignedRect;
            this.matrixL2W = rectToCopy.matrixL2W;
            this.matrixW2L = rectToCopy.matrixW2L;
        }

        public float Width => localAlignedRect.corners.z - localAlignedRect.corners.x;
        public int WidthInt => (int)Width;
        public int WidthRoundInt => (int)(Width + .5f);

        public float Height => localAlignedRect.corners.w - localAlignedRect.corners.y;
        public int HeightInt => (int)Height;
        public int HeightRoundInt => (int)(Height + .5f);

        public float2 Size => localAlignedRect.Size;
        public int2 SizeInt => (int2)Size;
        public int2 SizeRoundInt => (int2)(Size + .5f);

        public int CountInt
        {
            get
            {
                int2 widthHeightInt = (int2)Size;
                return widthHeightInt.x * widthHeightInt.y;
            }
        }

        public float2 Min => localAlignedRect.Min;
        public int2 MinInt => (int2)Min;
        public int2 MinRoundInt => (int2)(Min + .5f);

        public float2 Max => localAlignedRect.Max;
        public int2 MaxInt => (int2)Max;
        public int2 MaxRoundInt => (int2)(Max + .5f);

        public float2 Center
        {
            get => TransformLocalToWorld(localAlignedRect.Center);
            set => localAlignedRect.Center = TransformWorldToLocal(value);
        }
        public int2 CenterInt => (int2)Center;
        public int2 CenterRoundInt => (int2)(Center + .5f);

        public float2 LocalCenter => localAlignedRect.Center;
        public int2 LocalCenterInt => (int2)LocalCenter;
        public int2 LocalCenterRoundInt => (int2)(LocalCenter + .5f);

        public float2 CornerBL => TransformLocalToWorld(localAlignedRect.corners.xy);
        public float2 CornerBR => TransformLocalToWorld(localAlignedRect.corners.zy);
        public float2 CornerTL => TransformLocalToWorld(localAlignedRect.corners.xw);
        public float2 CornerTR => TransformLocalToWorld(localAlignedRect.corners.zw);

        public float2 RootPosition => TransformLocalToWorld(float2.zero);

        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(float2 worldPoint)
        {
            float2 localPoint = TransformWorldToLocal(worldPoint);

            bool4 validXY = new bool4
                (localPoint.x >= localAlignedRect.corners.x,
                localPoint.x <= localAlignedRect.corners.z,
                localPoint.y >= localAlignedRect.corners.y,
                localPoint.y <= localAlignedRect.corners.w);

            return math.all(validXY);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 DistToPointSplit(float2 worldPoint)
        {
            float2 localPoint = TransformWorldToLocal(worldPoint);

            float2 distMin = localAlignedRect.corners.xy - localPoint;
            float2 distMax = localPoint - localAlignedRect.corners.zw;

            return math.clamp(math.max(distMin, distMax), 0, float.MaxValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ORectNative Encapsulate(float2 worldPoint)
        {
            float2 pointAligned = TransformWorldToLocal(worldPoint);
            localAlignedRect.Encapsulate(pointAligned);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RectNative ComputeEncapsulatingAlignedRect()
        {
            var rect = new RectNative(CornerBL);
            rect.Encapsulate(CornerBR);
            rect.Encapsulate(CornerTL);
            rect.Encapsulate(CornerTR);

            return rect;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ORectNative EnsureValid(float minSize = 0.005f)
        {
            float2 avgPoint = (localAlignedRect.Min + localAlignedRect.Max) * .5f;
            float2 minSizeOffset = new float2(minSize);

            float2 avgPointXY = avgPoint - minSizeOffset;
            float2 avgPointZW = avgPoint + minSizeOffset;

            localAlignedRect.Min = math.min(localAlignedRect.Min, avgPointXY);
            localAlignedRect.Max = math.max(localAlignedRect.Max, avgPointZW);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Expand(float expansion)
        {
            localAlignedRect.corners.xy -= expansion;
            localAlignedRect.corners.zw += expansion;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 TransformLocalToWorld(float2 pointLocal)
        {
            float2 pointWorld = math.transform(matrixL2W, new float3(pointLocal.x, 0, pointLocal.y)).xz;
            return pointWorld;

            //Offset to rect center
            /*point -= localAlignedRect.Center;
            //Rotate locally
            point = math.rotate(rotation, new float3(point.x, 0, point.y)).xz;
            //Offset back
            point += localAlignedRect.Center;*/
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 TransformWorldToLocal(float2 pointWorld)
        {
            float2 pointLocal = math.transform(matrixW2L, new float3(pointWorld.x, 0, pointWorld.y)).xz;
            return pointLocal;

            //Offset to rect center
            /*point -= localAlignedRect.Center;
            //Rotate locally
            point = math.rotate(rotationInv, new float3(point.x, 0, point.y)).xz;
            //Offset back
            point += localAlignedRect.Center;*/

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ORectNative GetCopy()
        {
            return new ORectNative(this);
        }

#if UNITY_EDITOR
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
        #endregion

        #region Jobs
        /// <summary>
        /// Encapsulates this Oriented Rect with an Axis-Aligned Rect.
        /// Then generates a solid distance field, or a soft mask!
        /// You are responsible for disposing of the native array!
        /// </summary>
        /// <returns>Values in the distfield: 1 == point on/in ORect, 0 == point beyond max distance from ORect</returns>
        public (RectNative pixelRect, NativeArray<float> distFieldArray) ComputeEncapsulatedNormalizedDistField(float maxDistance, Allocator allocator)
        {
            RectNative expPixelRect = ComputeEncapsulatingAlignedRect();
            expPixelRect.ExpandInt(maxDistance);

            NativeArray<float> distFieldArray = new NativeArray<float>(expPixelRect.CountInt, allocator);

            ORectNativeToDistField jab = new ORectNativeToDistField()
            {
                distField = distFieldArray,
                maxDist = maxDistance,
                oRect = this,
                pixelRect = expPixelRect,
                //terrainDataWidth = terrainHeightNative.TerrainDataWidthCached,
            };
            
                  jab.ScheduleBatch(distFieldArray.Length, 32).Complete();

            return (expPixelRect, distFieldArray);
        }

        [BurstCompile]
        struct ORectNativeToDistField : IJobParallelForBatch
        {
            [WriteOnly] public NativeArray<float> distField;
            public RectNative pixelRect;
            public ORectNative oRect;
            public float maxDist;

            public void Execute(int startIndex, int count)
            {
                for (int i = startIndex; i < startIndex + count; i++)
                {
                    //TerrainHeightNative.Convert_LocalIndex_To_PixelData(i, out int2 localCoords, out int pixelIndex, out int2 pixelCoords, pixelRect.WidthInt, terrainDataWidth, pixelRect.MinInt);
                    //float2 worldPoint = TerrainHeightNative.Convert_PixelCoord_To_WorldPositionXZ(pixelCoords, terrainDataSize, terrainDataWidth, terrainMatrix_L2W);

                    int2 localCoords = new int2(i % pixelRect.WidthInt, i / pixelRect.WidthInt);
                    int2 pixelCoords = pixelRect.MinInt + localCoords;
                    distField[i] = math.saturate(1 - (math.length(oRect.DistToPointSplit((float2)pixelCoords + .5f)) / maxDist));
                }
            }
        }
        #endregion
        
        #region Helper

        static (float4x4 matrix, float4x4 matrixInv) MatricesFromUpDir(float2 worldCenter, float2 worldUpDir)
        {
            worldUpDir = math.normalizesafe(worldUpDir);
            quaternion rotation = Quaternion.FromToRotation(Vector3.forward, new float3(worldUpDir.x, 0, worldUpDir.y));

            float3 worldCenter3 = worldCenter.ToFloat3(0, 2, 1, 0);
            
            var matrix = float4x4.TRS(worldCenter3, rotation, new float3(1));
            var matrixInv = math.inverse(matrix);
            return (matrix, matrixInv);
        }
        
        #endregion
    }
}