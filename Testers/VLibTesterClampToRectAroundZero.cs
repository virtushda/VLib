// Disabled for compilation

/*using Drawing;
using Unity.Mathematics;
using UnityEngine;

namespace VLib.Testers
{
    public class VLibTesterClampToRectAroundZero : MonoBehaviour
    {
        public RectNative rect = new Rect(Vector2.zero, new Vector2(1, 2));
        public Vector2 value = Vector2.zero;
        public Vector2 clampedValue = Vector2.zero;

        void Update()
        {
            clampedValue = VMath.ClampToRectAroundZero(value, rect);

            using var drawer = DrawingManager.GetBuilder(true);
            drawer.xz.WireRectangle(rect.Center, quaternion.identity, rect.Size, Color.green);
            drawer.xz.Arrow(float2.zero, value, Color.white);
            drawer.xz.Arrow(float2.zero, clampedValue, Color.blue);
        }
    }
}*/