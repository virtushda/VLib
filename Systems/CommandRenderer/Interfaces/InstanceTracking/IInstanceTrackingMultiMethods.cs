using System.Collections.Generic;

namespace VLib
{
    public static class IInstanceTrackingMultiMethods
    {
        public static List<TTrack> AddGetTracker<TTracker, TTrack>(this IInstanceTrackingMulti<TTracker, TTrack> tracking, TTracker tracker)
            where TTrack : IInstanceTrack
        {
            if (!tracking.InstanceMap.TryGetValue(tracker, out var tracks))
            {
                tracks = new List<TTrack>();
                tracking.InstanceMap.Add(tracker, tracks);
                return tracks;
            }
            return tracks;
        }

        public static bool RemoveTracker<TTracker, TTrack>(this IInstanceTrackingMulti<TTracker, TTrack> tracking, TTracker tracker)
            where TTrack : IInstanceTrack
        {
            if (tracking.InstanceMap.ContainsKey(tracker))
            {
                tracking.InstanceMap.Remove(tracker);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes tracker, but also returns a ref to the internal list. (For pooling)
        /// </summary>
        public static bool RemoveTracker<TTracker, TTrack>(this IInstanceTrackingMulti<TTracker, TTrack> tracking, TTracker tracker, out List<TTrack> trackList)
            where TTrack : IInstanceTrack
        {
            if (tracking.InstanceMap.TryGetValue(tracker, out trackList))
            {
                tracking.InstanceMap.Remove(tracker);
                return true;
            }
            trackList = default;
            return false;
        }

        public static void ClearTracker<TTracker, TTrack>(this IInstanceTrackingMulti<TTracker, TTrack> tracking, TTracker tracker)
            where TTrack : IInstanceTrack
        {
            AddGetTracker(tracking, tracker).Clear();
        }

        public static void AddTrack<TTracker, TTrack>(this IInstanceTrackingMulti<TTracker, TTrack> tracking, TTracker tracker, TTrack track)
            where TTrack : IInstanceTrack
        {
            AddGetTracker(tracking, tracker).Add(track);
        }

        public static void RemoveTrack<TTracker, TTrack>(this IInstanceTrackingMulti<TTracker, TTrack> tracking, TTracker tracker, TTrack track)
            where TTrack : IInstanceTrack
        {
            AddGetTracker(tracking, tracker).Remove(track);
        }

        public static void ClearAll<TTracker, TTrack>(this IInstanceTrackingMulti<TTracker, TTrack> tracking)
           where TTrack : IInstanceTrack
        {
            tracking.InstanceMap.Clear();
        }

        public static bool TryGetTrackedBy<TTracker, TTrack>(this IInstanceTrackingMulti<TTracker, TTrack> tracking, TTracker tracker, out List<TTrack> tracks)
            where TTrack : IInstanceTrack
        {
            return tracking.InstanceMap.TryGetValue(tracker, out tracks);
        }
    }
}