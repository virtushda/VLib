using System.Collections.Generic;

namespace VLib
{
    /// <summary> Auto-expanding pool of lists. </summary>
    public class AutoListPool<T> : VPool<List<T>>
    {
        public AutoListPool(int initPoolCapacity = 8, int initListCapacity = 16)
            : base(initPoolCapacity,
                repoolPreProcess: list => list.Clear(),
                creationAction: () => new List<T>(initListCapacity))
        { }
    }
}