using System.Collections.Generic;
using UnityEngine.Pool;

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
        /// <summary> The list you receive is pulled from UnityEngine.Pool.ListPool </summary>
        public static List<ulong> AutoGetTrackIDsWithPoolList<T>(this T container)
            where T : ITrackedContainer
        {
            var trackIDs = ListPool<ulong>.Get();
            container.GetTrackIDs(ref trackIDs);
            return trackIDs;
        }
    }
}