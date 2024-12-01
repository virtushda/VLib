using System.Collections.Generic;

namespace VLib
{
    public interface IInstanceTrackingMulti<TTracker, TTrack>
        where TTrack : IInstanceTrack
    {
        Dictionary<TTracker, List<TTrack>> InstanceMap { get; }
    }

    /*public interface IInstanceTrackingMultiPooling<TTracker, TTrack>
        where TTrack : IInstanceTrack
    {
        AutoConcurrentListPool<TTrack> ListPool { get; }
    }*/
}