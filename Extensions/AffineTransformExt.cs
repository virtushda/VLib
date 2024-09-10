using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace VLib
{
    public static class AffineTransformExt
    {
        /// <summary> A proper lerp that doesn't cause distortions. </summary>
        public static AffineTransform Lerp(in this AffineTransform a, in AffineTransform b, float t)
        {
            var pos = lerp(a.t, b.t, t);
            
            a.rs.DecomposeRotScaleRaw(out var rotationA, out var scaleA);
            b.rs.DecomposeRotScaleRaw(out var rotationB, out var scaleB);
            
            var rot = slerp(rotationA, rotationB, t);
            var scale = lerp(scaleA, scaleB, t);
            
            return new AffineTransform(pos, rot, scale);
        }

        /// <summary> Direct linear lerp, can cause distortions, but is much cheaper. </summary>
        public static AffineTransform LerpCheap(in this AffineTransform a, in AffineTransform b, float t)
        {
            var newTransform = new AffineTransform();
            newTransform.t = lerp(a.t, b.t, t);
            newTransform.rs = a.rs.Lerp(b.rs, t);
            return newTransform;
        }
    }
}