using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Assertions;
using VLib;

/// <summary> A low-level representation of a set of LOD thresholds with support for hysteresis. </summary>
public struct LODSet : IDisposable
{
    VUnsafeArray<float> distances;
    readonly float hysteresisHalf;

    public LODSet(in Span<float> baseDistances, float hysteresisHalf, Allocator allocator)
    {
        Assert.IsTrue(baseDistances is {IsEmpty: false, Length: > 0});
        distances = new VUnsafeArray<float>(baseDistances.Length, allocator);
        this.hysteresisHalf = hysteresisHalf;
        SetDistances(baseDistances);
    }

    public void Dispose()
    {
        if (distances.IsCreated)
            distances.Dispose();
    }
    
    public void SetDistances(in ReadOnlySpan<float> newDistances)
    {
        if (newDistances.IsEmpty)
            return;
        var min = math.min(distances.Length, newDistances.Length);
        for (int i = 0; i < min; i++)
            distances[i] = newDistances[i];
    }

    public readonly int CalculateNewLOD(int currentLOD, float previousDist, float currentDist)
    {
        int numBoundaries = distances.Length;
        if (numBoundaries == 0) return currentLOD;

        int numLevels = numBoundaries + 1; // e.g., 3 boundaries -> 4 LOD levels (0 to 3)
        if (currentLOD < 0 || currentLOD >= numLevels) 
            return currentLOD; // Clamp invalid input

        // Upward transition: to coarser LOD (higher index)
        if (currentLOD < numLevels - 1)
        {
            float baseBoundary = distances[currentLOD]; // Boundary after current LOD
            float exitThreshold = baseBoundary + hysteresisHalf;
            if (currentDist > exitThreshold && previousDist <= exitThreshold)
                return currentLOD + 1;
        }

        // Downward transition: to finer LOD (lower index)
        if (currentLOD > 0)
        {
            float baseBoundary = distances[currentLOD - 1]; // Boundary before current LOD
            float enterThreshold = baseBoundary - hysteresisHalf;
            if (currentDist < enterThreshold && previousDist >= enterThreshold)
                return currentLOD - 1;
        }

        // No threshold crossed: retain provided current LOD
        return currentLOD;
    }
}