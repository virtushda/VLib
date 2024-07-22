using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace VLib
{
    public static class StopwatchExt
    {
        public static void LogStopwatchResult(this Stopwatch watch, string prepend = "", string append = "")
        {
            Debug.Log($"{prepend} {((float)watch.Elapsed.TotalSeconds).AsTimeToPrint()} {append}");
        }
    }
}