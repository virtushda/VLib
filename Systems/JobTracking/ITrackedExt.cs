using System;

namespace VLib
{
    public static class ITrackedExt
    {
        /// <summary> Wrap the structure in a special tracking structure with a unique identifier </summary>
        public static Tracked<T> Track<T>(this T objToTrack)
            where T : unmanaged, IDisposable
        {
            return new Tracked<T>(objToTrack);
        }

        public static void CompleteClearDependencies<T>(this T tracked)
            where T : unmanaged, ITracked
        {
            TrackedCollectionManager.CompleteAllJobsFor(tracked.ID);
        }

        /// <summary> DisposeRefToDefault for Tracked Structs that provides more control! </summary>
        public static void DisposeTrackedToDefault<T>(this ref T tracked, bool logException = true)
            where T : unmanaged, ITracked
        {
            tracked.Dispose(logException);
            tracked = default;
        }
    }
}