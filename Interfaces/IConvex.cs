using Unity.Collections;
using Unity.Mathematics;

namespace VLib
{
    public interface IConvex
    {
        public int Sides { get; }
    }
    
    public interface IConvex<T> : IConvex
    {
        public T GetVertex(int index);
        
        public T GetSide(int index);

        public T GetNormal(int index);
    }

    public static class IConvexExt
    {
        [GenerateTestsForBurstCompatibility]
        public static bool Intersects2D<T, U>(this T convex, U otherConvex)
            where T : struct, IConvex<float2>
            where U : struct, IConvex<float2>
        {
            //Check other convex outside of all of this convex sides
            int sides = convex.Sides;
            for (int i = 0; i < sides; i++)
            {
                if (otherConvex.AllVertsOutsideLine2D(convex.GetVertex(i), convex.GetNormal(i)))
                    return false;
            }
            //Check this convex outside of all other convex sides
            int otherSides = otherConvex.Sides;
            for (int j = 0; j < otherSides; j++)
            {
                if (convex.AllVertsOutsideLine2D(otherConvex.GetVertex(j), otherConvex.GetNormal(j)))
                    return false;
            }

            //Could not prove no convex overlap!
            return true;
        }
        
        /// <summary> Test each vertex against an infinite line, optimal for convex<->convex intersection testing. </summary>
        public static bool AllVertsOutsideLine2D<T>(this T convex, float2 linePnt, float2 lineNorm)
            where T : IConvex<float2>
        {
            int sides = convex.Sides;
            for (int i = 0; i < sides; i++)
            {
                var lineToVert = convex.GetVertex(i) - linePnt;
                // If any vertex is behind line, the result must be false.
                if (math.dot(lineNorm, lineToVert) <= 0)
                    return false;
            }
            
            // All vertices are proven to be outside the line
            return true;
        }
    }
}