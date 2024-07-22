using System.Runtime.CompilerServices;
using UnityEngine;

namespace VLib
{
    public static class Vec2Ext
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToVec3_XZ(this Vector2 vec, float yValue = 0)
        {
            Vector3 newVec;
            newVec.x = vec.x;
            newVec.y = yValue;
            newVec.z = vec.y;
            return newVec;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rect ToRect(this Vector2 vec, float sizeX, float sizeY, float margin)
        {
            return new Rect(vec.x - sizeX / 2 - margin,
                vec.y - sizeY / 2 - margin,
                sizeX + margin * 2,
                sizeY + margin * 2);
        }
        
        /// <summary>Extension method to lerp a given vector2 to a target by an amount</summary> 
        public static Vector2 LerpTo(this Vector2 a, Vector2 b, float amount) => Vector2.Lerp(a, b, amount);
    }
}