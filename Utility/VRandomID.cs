using System;
using System.Collections.Generic;

namespace VLib
{
    public static class VRandomPool
    {
        static Dictionary<int, System.Random> randomDict;

        public static System.Random Fetch(int seed)
        {
            return new Random(seed);
        }
    }
    
    public static class VRandomID
    {
        static HashSet<ulong> usedIDs;
        
        public static ulong FetchNewRandom()
        {
            if (usedIDs == null)
                usedIDs = new HashSet<ulong>();

            int tries = 10000;
            while (tries > 0)
            {
                tries--;

                //Generates garbage, will fix later
                ulong randomID = (ulong)(VRandomPool.Fetch(12345678).NextDouble() * long.MaxValue);

                if (usedIDs.Add(randomID))
                    return randomID;
            }

            return 0;
        }

        public static void ReleaseID(ulong id)
        {
            if (usedIDs == null)
                return;

            if (usedIDs.Contains(id))
                usedIDs.Remove(id);
        }
    }
}