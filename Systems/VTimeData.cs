using System.Diagnostics;
using System.Threading;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using VLib.Utility;
using Debug = UnityEngine.Debug;

namespace VLib.Systems
{
    /// <summary> A class responsible for handling the shared static time data. </summary>
    internal class VTimeData
    {
        #region Statics
        
        internal static readonly SharedStatic<TimeData> timeNative = SharedStatic<TimeData>.GetOrCreate<VTimeData>();

        static Thread timeBackgroundUpdateThread;

        internal struct TimeData
        {
            internal bool timeUpdateInvoked;
            
            /// <summary> This value comes from Time class, but the time class value should be set by <see cref="VTime.SetTimeScale"/> </summary>
            internal float currentTimeScale;
            internal float time;
            internal double timePrecise;
            internal float timeUnscaled;
            internal double timeUnscaledPrecise;
            internal float deltaTime;
            internal float smoothDeltaTime;
            internal float unscaledDeltaTime;
            internal int frameCount;
            internal float maximumDeltaTime;

            /// <summary> <inheritdoc cref="externallyUpdatedUnscaledTime"/> <br/>
            /// This version is constrained by maximumDeltaTime. </summary>
            public double externallyUpdatedTime;

            /// <summary> This uses a background thread to constantly inject an updated time. Allows intra-frame timing. <br/>
            /// Regular time properties are not updated intra-frame, and are only updated at the start of the frame. </summary>
            public double externallyUpdatedUnscaledTime;

            public void SetFromMain()
            {
                timeUpdateInvoked = true;
                
                time = Time.time;
                timePrecise = Time.timeAsDouble;
                timeUnscaled = Time.unscaledTime;
                timeUnscaledPrecise = Time.unscaledTimeAsDouble;
                deltaTime = Time.deltaTime;
                smoothDeltaTime = Time.smoothDeltaTime;
                unscaledDeltaTime = Time.unscaledDeltaTime;
                frameCount = Time.frameCount;
                maximumDeltaTime = Time.maximumDeltaTime;

                currentTimeScale = Time.timeScale;
            }

            public void SetExternal(double timeFromExternalUnscaled)
            {
                // Constrain growth by maximumDeltaTime
                externallyUpdatedTime = math.min(timeFromExternalUnscaled, externallyUpdatedTime + maximumDeltaTime);
                externallyUpdatedUnscaledTime = timeFromExternalUnscaled;
            }
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            if (timeBackgroundUpdateThread != null)
            {
                try
                {
                    timeBackgroundUpdateThread.Abort();
                }
                catch (ThreadAbortException e)
                {
                    Debug.LogError("Failed to abort time background update thread.");
                    Debug.LogException(e);
                }
                finally
                {
                    timeBackgroundUpdateThread = null;
                }
            }

            timeNative.Data = default;
            
            // Start the background thread
            timeBackgroundUpdateThread = new Thread(() =>
            {
                var stopwatch = ValueStopwatch.StartNew();
                
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
            });
            timeBackgroundUpdateThread.Start();

            VApplicationMonitor.OnQuitAndAllScenesUnloaded -= OnQuitAct;
            VApplicationMonitor.OnQuitAndAllScenesUnloaded += OnQuitAct;
        }

        // Specify end event happening late.
        static SortedAction onQuitAct;
        static SortedAction OnQuitAct => onQuitAct ??= new(OnQuit, 1000);
        static void OnQuit()
        {
            // Cleanup the thread
            Debug.Log("Aborting background time update thread.");
            timeBackgroundUpdateThread?.Abort();
            timeBackgroundUpdateThread = null;
        }
        
        #endregion
    }
}