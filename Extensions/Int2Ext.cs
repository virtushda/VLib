using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace VLib
{
    public static class Int2Ext
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int ToVector2Int(this int2 value)
        {
            return new Vector2Int(value.x, value.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 DivideRounded(this int2 lhs, float rhs)
        {
            return new int2((int)(lhs.x / rhs + .5f), (int)(lhs.y / rhs + .5f));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 DivideCeil(this int2 lhs, float rhs)
        {
            return new int2((int)(lhs.x / rhs + .99999999f), (int)(lhs.y / rhs + .99999999f));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWithinMinMax(this int2 myInt2, int2 min, int2 max)
        {
            return math.all(new bool4(myInt2 >= min, myInt2 <= max));

            /*bool xValid = myInt2.x >= min.x && myInt2.x <= max.x;
            bool yValid = myInt2.y >= min.y && myInt2.y <= max.y;
            return xValid && yValid;*/
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RectInt ToRectInt(this int2 center, int expansion, int margin)
        {
            return new RectInt(center.x - expansion / 2 - margin, center.y - expansion / 2 - margin, expansion + margin * 2, expansion + margin * 2);
        }
    }
}