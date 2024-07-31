using Unity.Collections;
using Unity.Mathematics;

public static class Voronoi2D
{
    public static int SelectClosestBruteForce(NativeArray<float2> cells, float2 point, out float distanceSq)
    {
        var closest = -1;
        distanceSq = float.MaxValue;

        for (var c = 0; c < cells.Length; c++)
        {
            var cell = cells[c];
            var distSq = math.distancesq(cell, point);
            if (distSq < distanceSq)
            {
                closest = c;
                distanceSq = distSq;
            }
        }
        return closest;
    }

    /*public static void GenerateVoronoiEdges(NativeArray<float2> points)
    {
        // Prepare input data
        InputData<float2> input = new InputData<float2>();
        input.Positions = points;
        
        // Prepare triangulator
        using var triangulator = new Triangulator<float2>(Allocator.Temp);
        triangulator.Input = input;
        
        triangulator.Run();
        
        // Generate voronoi edges
        UnsafeList<float2> voronoiPolygons = new UnsafeList<float2>(64, Allocator.Temp);
        triangulator.Output.
        
        triangulator.Output.Halfedges
    }*/
}