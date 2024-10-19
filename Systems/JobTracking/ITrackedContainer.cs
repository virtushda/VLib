using System.Collections.Generic;

namespace VLib
{
    /// <summary> A utility interface to help with fetching tracked IDs from systems without having to dig through the system. </summary>
    public interface ITrackedContainer
    {
        /// <summary> Recommend <see cref="TrackedContainerExtensions.AutoGetTrackIDsWithPoolList{T}"/> instead for easier usage.
        /// Results can be used with the <see cref="TrackedCollectionManager"/></summary>
        public void GetTrackIDs(ref List<ulong> trackIDs);
    }
    
    public static class TrackedContainerExtensions
    {
        /// <summary> The list you receive is pulled from a pool and needs to be returned. </summary>
        public static List<ulong> AutoGetTrackIDs<T>(this T container)
            where T : ITrackedContainer
        {
            var trackIDs = TrackedCollectionManager.GrabIDListFromPool();
            container.GetTrackIDs(ref trackIDs);
            return trackIDs;
        }
    }
}