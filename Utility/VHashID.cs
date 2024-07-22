using System.Collections.Generic;
using FastHashes;

namespace VLib
{
    public interface IHashProvider
    {
        FastHash64 Fetch(ulong seed);
    }
    
    public class VHashID : IHashProvider
    {
        static Dictionary<ulong, FastHash64> hashCache;

        public FastHash64 Fetch(ulong seed)
        {
            if (hashCache == null)
                hashCache = new Dictionary<ulong, FastHash64>();

            if (hashCache.TryGetValue(seed, out var hash))
                return hash;

            var newHash = new FastHash64(seed);
            hashCache.Add(seed, newHash);
            
            return newHash;
        }
    }
    
    public class VHashIDContained : IHashProvider
    {
        Dictionary<ulong, FastHash64> hashCache;

        public FastHash64 Fetch(ulong seed)
        {
            if (hashCache == null)
                hashCache = new Dictionary<ulong, FastHash64>();

            if (hashCache.TryGetValue(seed, out var hash))
                return hash;

            var newHash = new FastHash64(seed);
            hashCache.Add(seed, newHash);
            
            return newHash;
        }
    }
}