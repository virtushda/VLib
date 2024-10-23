/*using System;
using System.Threading;
using Unity.Collections;
using UnityEngine.Profiling;

namespace VLib
{
    // TODO: Shift this approach to SharedStatic<T>, will be simpler and faster.
    
    /// <summary> Designed to be created once and updated at a high frequency. </summary>
    [GenerateTestsForBurstCompatibility]
    public struct GlobalBurstTimer : IDisposabl
    {
        public static bool isInSpecialSetupMode = false;
        
        // Is a field to prevent copies, yes this is dangerous, don't touch this struct unless you know what you're doing.
        /// <summary> Don't set this, it is a field for technical reasons. </summary>
        public VUnsafeRef<float> time;

        public readonly bool IsCreated => time.IsCreated;
        
        public readonly float Time => IsCreated ? time.Value : throw new InvalidOperationException("GlobalBurstTimer struct invalid, time is not created.");

        /// <summary> Faster, but does not check that the timer is properly created. </summary>
        public readonly float TimeUnsafe => time.Value;
        
        public GlobalBurstTimer(bool dummyValue)
        {
            time = new VUnsafeRef<float>(Allocator.Persistent);
        }

        public void Dispose()
        {
            time.Dispose();
        }

        // Launch a thread that updates the time value of a globalbursttimer
        public static Thread CreateUpdateThread(GlobalBurstTimer timer)
        {
            var thread = new Thread(() =>
            {
                Profiler.BeginThreadProfiling("GlobalBurstTimer", "GlobalBurstTimer");
                
                var stopwatch = ValueStopwatch.StartNew();
                var spinwait = new SpinWait();
                
                while (true)
                {
                    // Wait for setup
                    if (GlobalBurstTimer.isInSpecialSetupMode)
                    {
                        if (spinwait.NextSpinWillYield)
                            spinwait.Reset();
                        spinwait.SpinOnce();
                    }
                    // Specifically bypass the safety check, because the thread handle safety feature should have an identical lifecycle to this thread!!!
                    if (!timer.time.IsCreated)
                        break;
                    
                    if (timer.Time < stopwatch.ElapsedSecondsF)
                    {
                        var t = timer.time;
                        t.Value = stopwatch.ElapsedSecondsF;
                    }
                    if (spinwait.NextSpinWillYield)
                        spinwait.Reset();
                    spinwait.SpinOnce();
                }
                
                Profiler.EndThreadProfiling();
            });
            
            // Don't start here, we need to adjust some stuff first
            // Undo this LOL
            thread.Start();
            return thread;
        }
    }
}*/