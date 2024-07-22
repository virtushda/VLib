using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace VLib
{
    public static class Vec2IntExt
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RectInt ExpandPointToRectInt(this Vector2Int vec, int expansion, int margin)
        {
            return new RectInt(vec.x - expansion / 2 - margin, vec.y - expansion / 2 - margin, expansion + margin * 2, expansion + margin * 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 ToInt2(this Vector2Int vec)
        {
            return new int2(vec.x, vec.y);
        }
    }
}