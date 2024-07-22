using System;
using UnityEngine;

namespace VLib
{
    public enum CustomBufferType
    {
        Int,
        Float,
        Vec4,
        Color
    }

    public static class CustomBufferTypeUtility
    {
        public static Type ToType(this CustomBufferType cbt)
        {
            switch (cbt)
            {
                case CustomBufferType.Int: return typeof(int);
                case CustomBufferType.Float: return typeof(float);
                case CustomBufferType.Vec4: return typeof(Vector4);
                case CustomBufferType.Color: return typeof(Color);
                default:
                    throw new NotImplementedException($"{cbt} is not implemented!");
            }
        }
    }
}