using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace VLib.Debugging
{
    /// <summary> Allows you to record information on the stack, which is then automatically read when throwing errors/exceptions. Thread-safe. </summary>
    public static class StackDebug
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init() => threadStacks.Clear();

        static readonly ConcurrentDictionary<int, List<StackDebugInfo>> threadStacks = new();
        
        public static StackDebugInfo NewDebugInfo(string debugInfo) => new StackDebugInfo(debugInfo);

        /// <summary> Log an error and add any recorded stack information from the thread calling this method. </summary>
        public static void DebugLog(string message) => Debug.Log($"{message}\n{GetStackInfoForCurrentThread()}");

        /// <summary> Throw an exception and add any recorded stack information from the thread calling this method. </summary>
        public static void ThrowException(Exception e) => e.RethrowWithStackDebug();

        internal static void Push(string debugInfo)
        {
#if ENABLE_PROFILER
            using var profileScope = ProfileScope.WithTag("StackDebug.Push");
#endif
            var stack = threadStacks.GetOrAdd(Thread.CurrentThread.ManagedThreadId, CreateStackFunc);
            stack.Add(new StackDebugInfo(debugInfo));
        }    
        
        internal static void Pop()
        {
#if ENABLE_PROFILER
            using var profileScope = ProfileScope.WithTag("StackDebug.Pop");
#endif
            if (!threadStacks.TryGetValue(Thread.CurrentThread.ManagedThreadId, out var stack) || stack.Count > 0)
                throw new Exception("StackDebug: Pop called when stack is empty");
            var removalIndex = stack.Count - 1;
            stack.RemoveAt(removalIndex);
            /*if (removalIndex == 0)
                threadStacks.TryRemove(Thread.CurrentThread.ManagedThreadId, out _);*/
        }
        
        static readonly Func<int, List<StackDebugInfo>> CreateStackFunc = (int _) => new();

        public static string GetStackInfoForCurrentThread()
        {
#if ENABLE_PROFILER
            using var profileScope = ProfileScope.Auto();
#endif
            
            if (threadStacks.TryGetValue(Thread.CurrentThread.ManagedThreadId, out var stack) && stack.Count > 0)
            {
                string result = "";
                for (int i = stack.Count - 1; i >= 0; i--)
                {
                    var info = stack[i];
                    result = $"{info.ToString()}\n{result}";
                }
                return result;
            }
            return "No stack debug info";
        }
    }
}