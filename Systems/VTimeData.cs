using System.Threading;
using Unity.Burst;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace VLib.Systems
{
    /// <summary> A class responsible for handling the shared static time data. </summary>
    internal unsafe class VTimeData
    {
        #region Statics
        
        internal static readonly SharedStatic<TimeData> timeNative = SharedStatic<TimeData>.GetOrCreate<VTimeData>();
#if TIME_PTR_AS_PROPERTY
        internal static TimeData* rawTimeDataPtr => (TimeData*) timeNative.UnsafeDataPointer;
#else
        /// <summary> This cached pointer speeds up access to the time data by a factor of 2-3x by removing a property call. </summary>
        internal static TimeData* rawTimeDataPtr;
#endif

        static Thread timeBackgroundUpdateThread;

        internal struct TimeData
        {
            public float currentTimeScale;
            public float time;
            public double timePrecise;
            public float timeUnscaled;
            public double timeUnscaledPrecise;
            public float deltaTime;
            public float smoothDeltaTime;
            public float unscaledDeltaTime;
            public int frameCount;

            /// <summary> This uses a background thread to constantly inject an updated time. Allows intra-frame timing. <br/>
            /// Regular time properties are not updated intra-frame, and are only updated at the start of the frame. </summary>
            public double externallyUpdatedTime;

            public void SetFromMain()
            {
                time = Time.time;
                timePrecise = Time.timeAsDouble;
                timeUnscaled = Time.unscaledTime;
                timeUnscaledPrecise = Time.unscaledTimeAsDouble;
                deltaTime = Time.deltaTime;
                smoothDeltaTime = Time.smoothDeltaTime;
                unscaledDeltaTime = Time.unscaledDeltaTime;
                frameCount = Time.frameCount;

                currentTimeScale = Time.timeScale;
            }

            public void SetExternal(double timeFromExternal) => externallyUpdatedTime = timeFromExternal;
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
#if !TIME_PTR_AS_PROPERTY
            rawTimeDataPtr = (TimeData*) timeNative.UnsafeDataPointer; // This should never move in memory during the lifetime of the application.
#endif
            if (timeBackgroundUpdateThread != null)
            {
                try
                {
                    timeBackgroundUpdateThread.Abort();
                }
                catch (ThreadAbortException e)
                {
                    UnityEngine.Debug.LogError("Failed to abort time background update thread.");
                    UnityEngine.Debug.LogException(e);
                }
                finally
                {
                    timeBackgroundUpdateThread = null;
                }
            }
            
            // Start the background thread
            timeBackgroundUpdateThread = new Thread(() =>
            {
                while (true)
                {
                    var stopwatch = ValueStopwatch.StartNew();
                    //var spinwait = new SpinWait();
                
                    while (true)
                    {
                        Profiler.BeginThreadProfiling("VTimeExternalTime", "VTimeExternalTime");
                        // Update time
                        timeNative.Data.SetExternal(stopwatch.Elapsed.TotalSeconds);
                        
                        // New wait, just sleep, should be often enough for intra-frame, around 1ms
                        Thread.Sleep(1);
                        
                        // Old wait, vastly more precise, probably overkill!
                        // Spinwait until the next frame
                        /*if (spinwait.NextSpinWillYield)
                            spinwait.Reset();
                        spinwait.SpinOnce();*/
                        Profiler.EndThreadProfiling();
                    }
                
                }
            });
            timeBackgroundUpdateThread.Start();
            
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= OnReenterEditMode;
            EditorApplication.playModeStateChanged += OnReenterEditMode;
#endif
        }
        
#if UNITY_EDITOR
        static void OnReenterEditMode(PlayModeStateChange stateChange)
        {
            if (stateChange != PlayModeStateChange.EnteredEditMode)
                return;
            
            // Cleanup
            timeBackgroundUpdateThread.Abort();
            timeBackgroundUpdateThread = null;
        }
#endif
        
        #endregion
    }
}