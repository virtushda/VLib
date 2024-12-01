using System.Collections.Generic;

namespace VLib
{
    // TODO: Review usages of this...

    /// <summary> Concurrent-safe auto-expanding pool of lists. </summary>
    public class AutoConcurrentListPool<T> : ConcurrentVPool<List<T>>
    {
        public AutoConcurrentListPool(int initPoolCapacity = 8, int initListCapacity = 16)
            : base(initPoolCapacity,
                repoolPreProcess: list => list.Clear(),
                creationAction: () => new List<T>(initListCapacity))
        { }
    }
}