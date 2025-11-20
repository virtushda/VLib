using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace VLib
{
    /// <summary> A simple utility to profile directly with a <see cref="ValueStopwatch"/>. <br/>
    /// Better for longer processes, where <see cref="ProfileScope"/> struggles. </summary>
    public struct ProfileWatchScope : IDisposable
    {
#if ENABLE_PROFILER

        readonly ValueStopwatch watch;
        readonly string profileTag;
        
        ProfileWatchScope(ValueStopwatch watch, string profileTag)
        {
            this.watch = watch;
            this.profileTag = profileTag;
        }

        public void Dispose() => Debug.Log($"ProfileWatchScope: {profileTag} took {watch.Elapsed.TotalMilliseconds}ms");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ProfileWatchScope Auto([CallerMemberName] string autoPopulatedTag = "")
        {
            var newWatch = ValueStopwatch.StartNew();
            return new ProfileWatchScope(newWatch, autoPopulatedTag);
        }
        
#else
        
        public void Dispose() { }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ProfileWatchScope Auto([CallerMemberName] string autoPopulatedTag = "") => default;
        
#endif
    }
}