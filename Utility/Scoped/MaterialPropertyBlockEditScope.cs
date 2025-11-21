#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define SAFETY
#endif

using System;
using System.Collections.Generic;
using Unity.Burst;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using VLib;

/// <summary> Provides structure for and streamlines the editing of material property blocks. </summary>
public readonly struct MaterialPropertyBlockEditScope<T> : IDisposable
    where T : Renderer
{
    // Main-thread only, shared context
    static readonly MaterialPropertyBlock SharedBlock = new();
    static readonly List<Renderer> RendererList = new(8);

#if SAFETY
    /// <summary> Global handle to block opening multiple edit scopes simultaneously. </summary>
    static VManagedSafetyHandle.User currentGlobalEditHandle;
    /// <summary> Handle for this scope. </summary>
    readonly VManagedSafetyHandle.User safetyHandle;
#endif
    
    readonly List<Renderer> targets;

    /// <summary>
    /// Constructor: Takes a seed renderer and any ICollection of renderers to apply the MPB to.
    /// </summary>
    public MaterialPropertyBlockEditScope(Renderer seed, ICollection<T> targets)
    {
        // Validate inputs
        Assert.IsNotNull(seed, "Seed renderer is null!");
        Assert.IsTrue(targets.Count > 0, "No targets provided!");
#if SAFETY
        Assert.IsFalse(currentGlobalEditHandle.IsValid, "Cannot open multiple MaterialPropertyBlockEditScope simultaneously!");
#endif
        
        Profiler.BeginSample("MaterialPropertyBlockEditScope");

        // Fetch block to shared context
        seed.GetPropertyBlock(SharedBlock);
        
        // Copy targets to shared context
        RendererList.Clear();
        RendererList.AddRange(targets);
        
        // Initialize structure
        this.targets = RendererList;
#if SAFETY
        safetyHandle = VManagedSafetyHandle.AllocateUser();
        currentGlobalEditHandle = safetyHandle;
#endif
    }

    /// <summary>
    /// Constructor: Takes a seed renderer and params array of renderers to apply the MPB to.
    /// </summary>
    public MaterialPropertyBlockEditScope(Renderer seed, params Renderer[] targets) : this(seed, (ICollection<T>)targets)
    { }

    public MaterialPropertyBlock Block => SharedBlock;

    [BurstDiscard]
    public void Dispose()
    {
#if SAFETY
        if (!safetyHandle.IsValid)
            throw new InvalidOperationException("MaterialPropertyBlockEditScope is not valid and cannot be disposed!");
        // It should not be possible to even hit this assertion, this protects against any future edits, which might break the preceeding safety check in construction.
        Assert.IsTrue(currentGlobalEditHandle == safetyHandle, "Cannot dispose MaterialPropertyBlockEditScope out of order!");
        safetyHandle.Dispose();
#endif
        foreach (var r in targets)
            r.SetPropertyBlock(SharedBlock);
        Profiler.EndSample();
    }
}

public static class MaterialPropertyBlockEditScopeExt
{
    /// <summary> Opens an editing context, pulling the material property block from the seed renderer, which will be applied to all targets when the scope is disposed. </summary>
    public static MaterialPropertyBlockEditScope<T> MaterialPropertyBlockScopedEditor<T>(this Renderer seed, ICollection<T> targets)
        where T : Renderer
    {
        return new MaterialPropertyBlockEditScope<T>(seed, targets);
    }
}