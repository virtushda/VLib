using System;
using Unity.Burst;
using UnityEngine;

namespace VLib.Debugging
{
    /// <summary> Allows you to track method execution with any level of granularity and automatically report the last known marker passed if an exception occurs. <br/>
    /// Not for burst. (Easy to make a bespoke burst version if needed) </summary>
    public struct RuntimeExceptionMarker : IDisposable
    {
        string name;
        string lastMarkName;
        bool reportMarkOnDispose;
        bool logToCloud;

        public RuntimeExceptionMarker(string name, bool logToCloud = true)
        {
            this.name = name;
            lastMarkName = "Constructed, no marks set.";
            reportMarkOnDispose = true;
            this.logToCloud = logToCloud;
        }

        /// <summary> Call this to set marks that will be reported if they are the last set. </summary>
        public void MarkMidpoint(string markName)
        {
            lastMarkName = markName;
            reportMarkOnDispose = true;
        }

        /// <summary> Call this at the end of the method to inform this marker that we've successfully reached the end! </summary>
        public void MarkEnd()
        {
            lastMarkName = "End";
            reportMarkOnDispose = false;
        }

        [BurstDiscard]
        public void Dispose()
        {
            if (reportMarkOnDispose)
            {
                if (logToCloud)
                    Debug.LogException(new UnityException($"RuntimeDebugMarker: {name} failed at {lastMarkName}"));
                else
                    Debug.LogError($"RuntimeDebugMarker: {name} failed at {lastMarkName}");
            }
        }
    }
}