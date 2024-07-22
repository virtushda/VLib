using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace VLib
{
    public static class RectIntExt
    {
        public struct RectIntCache
        {
            public int xMin;
            public int yMin;
            public int xMax;
            public int yMax;
            public int width;
            public int height;
            public int2 center;
            public Vector2 centerFloat;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RectIntCache GetCached(this RectInt rect)
        {
            var centerF = rect.center;
            int2 centerInt = new int2((int)(centerF.x + .5f), (int)(centerF.y + .5f));

            return new RectIntCache
            {
                xMin = rect.xMin,
                yMin = rect.yMin,
                xMax = rect.xMax,
                yMax = rect.yMax,
                width = rect.width,
                height = rect.height,
                center = centerInt,
                centerFloat = centerF
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RectInt GetClamped(this RectInt rect, int xMin, int yMin, int xMax, int yMax)
        {
            //Pull Values
            var currentXMin = rect.xMin;
            var currentYMin = rect.yMin;
            var currentXMax = rect.xMax;
            var currentYMax = rect.yMax;

            //Compare, Pick Innermost
            currentXMin = currentXMin < xMin ? xMin : currentXMin;
            currentYMin = currentYMin < yMin ? yMin : currentYMin;
            currentXMax = currentXMax > xMax ? xMax : currentXMax;
            currentYMax = currentYMax > yMax ? yMax : currentYMax;

            //Return new RectInt
            return new RectInt(currentXMin, currentYMin, currentXMax - currentXMin, currentYMax - currentYMin);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Encapsulate(this RectInt rect, RectInt rectToEncapsulate)
        {
            //Cache
            var rectEncapXMin = rectToEncapsulate.xMin;
            var rectEncapXMax = rectToEncapsulate.xMax;
            var rectEncapYMin = rectToEncapsulate.yMin;
            var rectEncapYMax = rectToEncapsulate.yMax;

            //Compare and Adjust
            if (rectEncapXMin < rect.xMin) rect.xMin = rectEncapXMin;
            if (rectEncapXMax > rect.xMax) rect.xMax = rectEncapXMax;
            if (rectEncapYMin < rect.yMin) rect.yMin = rectEncapYMin;
            if (rectEncapYMax > rect.yMax) rect.yMax = rectEncapYMax;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 CenterInt2(this RectInt rect)
        {
            var centerVec2 = rect.center;
            return new int2((int)(centerVec2.x + .5f), (int)(centerVec2.y + .5f));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RectInt GetExpanded(this RectInt rect, int expansion)
        {
            var expansion_x2 = expansion * 2;

            return new RectInt(rect.xMin - expansion,
                               rect.yMin - expansion,
                               rect.width + expansion_x2,
                               rect.height + expansion_x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 GetClosestPoint(this RectInt rect, int2 targetPoint)
        {
            int2 min = new int2(rect.xMin, rect.yMin);
            int2 max = new int2(rect.xMax, rect.yMax);

            return math.clamp(targetPoint, min, max);

            //Cache
            /*int2 min = new int2(rect.xMin, rect.yMin);
            int2 max = new int2(rect.xMax, rect.yMax);

            int newPointX = 0;
            int newPointY = 0;

            //Handle X
            if (targetPoint.x < min.x)
                newPointX = min.x;
            else if (targetPoint.x >= max.x)
                newPointX = max.x;
            else
                newPointX = targetPoint.x;

            //Handle Y
            if (targetPoint.y < min.y)
                newPointY = min.y;
            else if (targetPoint.y >= max.y)
                newPointY = max.y;
            else
                newPointY = targetPoint.y;

            return new int2(newPointX, newPointY);*/
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Count(this RectInt rect)
        {
            return rect.width * rect.height;
        }
    }
}