using System.Threading;
using Unity.Mathematics;
using UnityEngine;

namespace VLib.Utility
{
    public static class VColorUtility
    {
        static int debugColorHash;
        
        public static Color NextDebugColor()
        {
            var hash = Interlocked.Increment(ref debugColorHash);
            return HashToColor((uint)hash);
        }

        public static Color HashToColor(uint hash, 
            float saturationMin = 0.5f, float saturationMax = 1f, 
            float valueMin = 0.5f, float valueMax = 1f,
            uint hashSpreader = 12345)
        {
            var rand = new Unity.Mathematics.Random(hash * hashSpreader);
            var satRand = rand.NextFloat();
            var valRand = rand.NextFloat();
            return Color.HSVToRGB(
                rand.NextFloat(),
                math.remap(0, 1, saturationMin, saturationMax, satRand),
                math.remap(0, 1, valueMin, valueMax, valRand));
        }
    }
}