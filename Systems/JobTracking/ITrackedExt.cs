using System;
using Unity.Jobs;

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
        
        public static TrackedDependency AsReadDependency<T>(this T tracked)
            where T : struct, ITracked
        {
            return new TrackedDependency(tracked.ID, false);
        }
        
        public static TrackedDependency AsWriteDependency<T>(this T tracked)
            where T : struct, ITracked
        {
            return new TrackedDependency(tracked.ID, true);
        }
        
        public static JobHandle GetDependencyHandle<T>(this T tracked, bool writeAccess)
            where T : ITracked
        {
            return TrackedCollectionManager.GetDependencyHandleMainThread(tracked.ID, writeAccess);
        }

        public static void CompleteClearDependencies<T>(this T tracked)
            where T : unmanaged, ITracked
        {
            if (tracked.IsCreated)
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