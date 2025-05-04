using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace VLib.Utility
{
    public static class VDebugLog
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            UniqueDebugMessages.Clear();
            uniqueCount = 0;
        }
        
        static readonly ConcurrentDictionary<string, bool> UniqueDebugMessages = new();
        static int uniqueCount;
        const int uniqueSoftLimit = 100000;

        public static void LogUnique(string message, bool alwaysLogInEditor = true)
        {
            if (UniqueDebugMessages.TryAdd(message, true))
            {
                IncrementCheckUniqueCount();
                Debug.Log(message);
            }
#if UNITY_EDITOR
            else if (alwaysLogInEditor)
                Debug.Log(message);
#endif
        }
        
        public static void LogUniqueWarning(string message, bool alwaysLogInEditor = true)
        {
            if (UniqueDebugMessages.TryAdd(message, true))
            {
                IncrementCheckUniqueCount();
                Debug.LogWarning(message);
            }
#if UNITY_EDITOR
            else if (alwaysLogInEditor)
                Debug.LogWarning(message);
#endif
        }
        
        public static void LogUniqueError(string message, bool alwaysLogInEditor = true)
        {
            if (UniqueDebugMessages.TryAdd(message, true))
            {
                IncrementCheckUniqueCount();
                Debug.LogError(message);
            }
#if UNITY_EDITOR
            else if (alwaysLogInEditor)
                Debug.LogError(message);
#endif
        }
        
        static void IncrementCheckUniqueCount()
        {
            var thisUniqueCount = Interlocked.Increment(ref uniqueCount);
            if (thisUniqueCount == uniqueSoftLimit)
                Debug.LogError($"Unique debug message soft limit reached! Limit: {uniqueSoftLimit}");
        }
    }
}