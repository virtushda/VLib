#if UNITY_EDITOR
#define CHECK_EARLY_UPDATE_CALLED
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Mathematics;
using UnityEngine;
using VLib.Threading;

namespace VLib.Systems
{
    /// <summary> Time, but thread-safe and burst-compatible. <br/>
    /// <see cref="OnEarlyUpdate"/> must be hooked into the game loop, at the earliest part of the frame. <br/>
    /// Immediately after hooking, you must call <see cref="OnEarlyUpdateConnected"/>. </summary>
    public class VTime //
    {
        #region Statics
        
        public static readonly int GTimeUnscaledSin = Shader.PropertyToID("GTimeUnscaledSin");
        
        #endregion

        [Conditional("CHECK_EARLY_UPDATE_CALLED")]
        public static void CheckEarlyUpdateCalled()
        {
            if (!VTimeData.timeNative.Data.timeUpdateInvoked)
                throw new InvalidOperationException("VTime.OnEarlyUpdate() was not called at the start of the frame. VTime is not appropriate for use in edit-mode, and must be properly hooked for play mode.");
        }

        /// <summary> This must be called as early as possible in the frame. Much functionality in this class will NOT work without this method being reliably invoked at the start of the frame. </summary>
        public static void OnEarlyUpdate()
        {
            VTimeData.timeNative.Data.SetFromMain();
        }

        public static void OnEarlyUpdateConnected() => OnEarlyUpdate();
        
        public const float DeltaTime5FPS = 1 / 5f;
        public const float DeltaTime10FPS = 1 / 10f;
        public const float DeltaTime12FPS = 1 / 12f;
        public const float DeltaTime15FPS = 1 / 15f;
        public const float DeltaTime24FPS = 1 / 24f;
        public const float DeltaTime30FPS = 1 / 30f;
        public const float DeltaTime50FPS = 1 / 50f;
        public const float DeltaTime60FPS = 1 / 60f;
        public const float DeltaTime120FPS = 1 / 120f;
        public const float DeltaTime144FPS = 1 / 144f;
        public const float DeltaTime1000FPS = 1 / 1000f;

        public const float DeltaMS5FPS = 1000 / 5f;
        public const float DeltaMS10FPS = 1000 / 10f;
        public const float DeltaMS12FPS = 1000 / 12f;
        public const float DeltaMS15FPS = 1000 / 15f;
        public const float DeltaMS24FPS = 1000 / 24f;
        public const float DeltaMS30FPS = 1000 / 30f;
        public const float DeltaMS50FPS = 1000 / 50f;
        public const float DeltaMS60FPS = 1000 / 60f;
        public const float DeltaMS120FPS = 1000 / 120f;
        public const float DeltaMS144FPS = 1000 / 144f;

        public static float TemporalMultiplier15FPS  => smoothDeltaTime / DeltaTime15FPS;
        public static float TemporalMultiplier24FPS  => smoothDeltaTime / DeltaTime24FPS;
        public static float TemporalMultiplier30FPS  => smoothDeltaTime / DeltaTime30FPS;
        public static float TemporalMultiplier50FPS  => smoothDeltaTime / DeltaTime50FPS;
        public static float TemporalMultiplier60FPS  => smoothDeltaTime / DeltaTime60FPS;
        public static float TemporalMultiplier120FPS => smoothDeltaTime / DeltaTime120FPS;
        public static float TemporalMultiplier144FPS => smoothDeltaTime / DeltaTime144FPS;
        
        /// <summary> These static fields/properties will only work when <see cref="OnEarlyUpdate"/> is called externally, otherwise these values will be default. <br/>
        /// Cached Time Values (to avoid Unity's extern properties that are not cheap according to mr.profiler) </summary>
        public static float currentTimeScale
        {
            get
            {
                CheckEarlyUpdateCalled();
                return VTimeData.timeNative.Data.currentTimeScale;
            }
        }

        /// <summary> These static fields/properties will only work when <see cref="OnEarlyUpdate"/> is called externally, otherwise these values will be default. <br/>
        /// Cached Time Values (to avoid Unity's extern properties that are not cheap according to mr.profiler) </summary>
        public static float time
        {
            get 
            {
                CheckEarlyUpdateCalled();
                return VTimeData.timeNative.Data.time; 
            }
        }

        /// <summary> These static fields/properties will only work when <see cref="OnEarlyUpdate"/> is called externally, otherwise these values will be default. <br/>
        /// Cached Time Values (to avoid Unity's extern properties that are not cheap according to mr.profiler) </summary>
        public static double timePrecise
        {
            get
            {
                CheckEarlyUpdateCalled();
                return VTimeData.timeNative.Data.timePrecise;
            }
        }

        /// <summary> These static fields/properties will only work when <see cref="OnEarlyUpdate"/> is called externally, otherwise these values will be default. <br/>
        /// Cached Time Values (to avoid Unity's extern properties that are not cheap according to mr.profiler) </summary>
        public static float timeUnscaled
        {
            get
            {
                CheckEarlyUpdateCalled();
                return VTimeData.timeNative.Data.timeUnscaled;
            }
        }

        /// <summary> These static fields/properties will only work when <see cref="OnEarlyUpdate"/> is called externally, otherwise these values will be default. <br/>
        /// Cached Time Values (to avoid Unity's extern properties that are not cheap according to mr.profiler) </summary>
        public static double timeUnscaledPrecise
        {
            get
            {
                CheckEarlyUpdateCalled();
                return VTimeData.timeNative.Data.timeUnscaledPrecise;
            }
        }

        /// <summary> These static fields/properties will only work when <see cref="OnEarlyUpdate"/> is called externally, otherwise these values will be default. <br/>
        /// Cached Time Values (to avoid Unity's extern properties that are not cheap according to mr.profiler) </summary>
        public static float deltaTime
        {
            get
            {
                CheckEarlyUpdateCalled();
                return VTimeData.timeNative.Data.deltaTime;
            }
        }

        /// <summary> These static fields/properties will only work when <see cref="OnEarlyUpdate"/> is called externally, otherwise these values will be default. <br/>
        /// Cached Time Values (to avoid Unity's extern properties that are not cheap according to mr.profiler) </summary>
        public static float smoothDeltaTime
        {
            get
            {
                CheckEarlyUpdateCalled();
                return VTimeData.timeNative.Data.smoothDeltaTime;
            }
        }

        /// <summary> These static fields/properties will only work when <see cref="OnEarlyUpdate"/> is called externally, otherwise these values will be default. <br/>
        /// Cached Time Values (to avoid Unity's extern properties that are not cheap according to mr.profiler) </summary>
        public static float unscaledDeltaTime
        {
            get
            {
                CheckEarlyUpdateCalled();
                return VTimeData.timeNative.Data.unscaledDeltaTime;
            }
        }

        /// <summary> These static fields/properties will only work when <see cref="OnEarlyUpdate"/> is called externally, otherwise these values will be default. <br/>
        /// Cached Time Values (to avoid Unity's extern properties that are not cheap according to mr.profiler) </summary>
        public static int frameCount
        {
            get
            {
                CheckEarlyUpdateCalled();
                return VTimeData.timeNative.Data.frameCount;
            }
        }

        public static float maximumDeltaTime
        {
            get
            {
                CheckEarlyUpdateCalled();
                return VTimeData.timeNative.Data.maximumDeltaTime;
            }
        }

        /// <summary> <inheritdoc cref="VTimeData.TimeData.externallyUpdatedTime"/> </summary>
        public static double intraFrameTime => VTimeData.timeNative.Data.externallyUpdatedTime;

        /// <summary> <inheritdoc cref="VTimeData.TimeData.externallyUpdatedUnscaledTime"/> </summary>
        public static double intraFrameTimeUnscaled => VTimeData.timeNative.Data.externallyUpdatedUnscaledTime;

        // Integer time
        /// <summary> When cast to uint, runs for 4085 years. </summary>
        public static long TimeMinutes => (long)(timePrecise / 60);
        
        /// <summary> <inheritdoc cref="TimeMinutes"/> </summary>
        public static uint TimeMinutesU => (uint)TimeMinutes;
        
        /// <summary> When cast to uint, runs for 68 years. </summary>
        public static long TimeSeconds => (long)timePrecise;
        
        /// <summary> <inheritdoc cref="TimeSeconds"/> </summary>
        public static uint TimeSecondsU => (uint)TimeSeconds;
        
        /// <summary> When cast to uint, runs for 17 years. </summary>
        public static long TimeQuarterSeconds => (long)(timePrecise * 4);
        
        /// <summary> <inheritdoc cref="TimeQuarterSeconds"/> </summary>
        public static uint TimeQuarterSecondsU => (uint)TimeQuarterSeconds;
        
        /// <summary> When cast to uint, runs for 6.8 years. </summary>
        public static long TimeDeciseconds => (long)(timePrecise * 10);
        
        /// <summary> <inheritdoc cref="TimeDeciseconds"/> </summary>
        public static uint TimeDecisecondsU => (uint) TimeDeciseconds;
        
        /// <summary> When cast to uint, runs for 24.8 days. </summary>
        public static long TimeMilliseconds => (long)(timePrecise * 1000);

        /// <summary> Changing the timescale with this method allows VTime to be perfectly up to date, allowing the timescale to be fetched from other threads & burst immediately after being set. </summary>
        public static void SetTimeScale(float factor, bool setUnityTimeScale = true)
        {
            if (setUnityTimeScale)
            {
                MainThread.Assert();
                Time.timeScale = factor;
            }
            VTimeData.timeNative.Data.currentTimeScale = factor;
        }
        
        public static ulong SecondsToNanoSeconds(double seconds) => (ulong)(seconds * 1000000000);
        public static long SecondsToTicks(double seconds) => (long)(seconds * 10000000);
        public static long MillisecondsToTicks(double ms) => (long)(ms * 10000);
        public static double QuarterSecondsToSeconds(double quarterSeconds) => quarterSeconds / 4f;
        public static double SecondsToQuarterSeconds(double seconds) => seconds * 4;
        public static double SecondsToMSFrac(double seconds) => seconds * 1000;
        public static double SecondsToMinutesFrac(double seconds) => seconds / 60f;
        public static double SecondsToHoursFrac(double seconds) => seconds / 3600f;
        public static double TicksToSeconds(long ticks) => ticks / 10000000f;
        
        public static int TimeSliceIndex(int slices)
        {
            CheckEarlyUpdateCalled();
            return frameCount % slices;
        }

        public static bool IsTimeSliceFrame(int slices, int sliceIndex)
        {
            CheckEarlyUpdateCalled();
            return frameCount % slices == sliceIndex;
        }

        /// <summary> Automatically compute a progressive step size </summary>
        public static int TimesliceProcessCount(int elementCount, float timeSpan, int min = 1)
        {
            CheckEarlyUpdateCalled();
            return timeSpan <= .001f ? elementCount : math.max(min, (int)(elementCount * math.saturate(deltaTime / timeSpan)));
        }

        /// <summary> Computes a range of indices to process for timeslicing and updates the start index for the next iteration </summary>
        public static (int startIndex, int end) TimesliceProcessRange(ref int startIndex, int totalElementCount, float timeSpan, int min = 1)
        {
            var count = TimesliceProcessCount(totalElementCount, timeSpan, min);
            // If off the end of the range, wrap around
            if (startIndex >= totalElementCount)
            {
                int end = math.min(count, totalElementCount);
                // Update start index for next iteration
                startIndex = end;
                return (0, end);
            }
            // Otherwise, return range extended by count.
            else
            {
                var start = startIndex;
                var end = math.min(startIndex + count, totalElementCount);
                // Update start index for next iteration
                startIndex = end;
                return (start, end);
            }
        }
        
        public static float ConstrainTimeStep(float step, float min = DeltaTime1000FPS, float max = DeltaTime10FPS) => math.clamp(step, min, max);

        /// <summary> When you want to know when a parallel job actually starts executing: <br/>
        /// - Add a double and an int field to your struct. <br/>
        /// - Call this at the beginning of execute. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CaptureConcurrentInitTime(ref double startTime, ref int initialized)
        {
            if (initialized > 0)
                return;
            
            // Race to initialize
            var previousInitValue = Interlocked.Exchange(ref initialized, 1);
            // Only the initializing thread can actually set the time
            if (previousInitValue == 0)
                Interlocked.Exchange(ref startTime, timePrecise); // Exchange to avoid partial read
        }
        
        /// <summary> Returns delta time constrained to be at max: .1f (10fps) </summary>
        public static float DeltaTimeConstrained => ConstrainTimeStep(deltaTime, 0);

        /// <summary> Returns delta time constrained to between .001f (1000fps) and .1f (10fps) </summary>
        public static float DeltaTimeConstrainedNonZero => ConstrainTimeStep(deltaTime);
    }
}