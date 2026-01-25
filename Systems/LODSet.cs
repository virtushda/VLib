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
        if (numBoundaries == 0) 
            return 0;

        int maxLOD = numBoundaries;
        int lod = math.clamp(currentLOD, 0, maxLOD);

        // Compute sticky band for current LOD
        float lower = lod > 0 ? distances[lod - 1] - hysteresisHalf : float.NegativeInfinity;
        float upper = lod < numBoundaries ? distances[lod] + hysteresisHalf : float.PositiveInfinity;

        // If both inside the sticky band, exit early
        if (previousDist >= lower && previousDist <= upper && currentDist >= lower && currentDist <= upper)
            return lod;

        // Otherwise, scan outward for target LOD
        // If moved farther, try coarser LODs
        if (currentDist > upper)
        {
            // Find coarsest LOD whose sticky band contains currentDist
            for (int l = lod + 1; l <= maxLOD; l++)
            {
                float nextUpper = l < numBoundaries ? distances[l] + hysteresisHalf : float.PositiveInfinity;
                if (currentDist <= nextUpper)
                    return l;
            }
            return maxLOD; // Farthest LOD if past all bands
        }
        // If moved closer, try finer LODs

        if (currentDist < lower)
        {
            for (int l = lod - 1; l >= 0; l--)
            {
                float nextLower = l > 0 ? distances[l - 1] - hysteresisHalf : float.NegativeInfinity;
                if (currentDist >= nextLower)
                    return l;
            }
            return 0; // Closest LOD if inside all bands
        }

        // Otherwise, should not reach here, but stick to currentLOD by default
        return lod;
    }

}