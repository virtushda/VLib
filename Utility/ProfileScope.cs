using System;
using System.Runtime.CompilerServices;
using UnityEngine.Profiling;

namespace VLib
{
    /// <summary> A simple utility to establish profiling  </summary>
    public struct ProfileScope : IDisposable
    {
        #if ENABLE_PROFILER
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ProfileScope Auto([CallerMemberName] string autoPopulatedTag = "")
        {
            Profiler.BeginSample(autoPopulatedTag);
            return new ProfileScope();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => Profiler.EndSample();
        
        #else // When the profiler is disabled, this struct does as little as possible.
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ProfileScope Auto([CallerMemberName] string autoPopulatedTag = "") => default;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ProfileScope WithTag(string profileTag) => default;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() { }
            
        #endif
    }
}