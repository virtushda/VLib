#if UNITY_EDITOR
#define CHECK_EARLY_UPDATE_CALLED
#endif

using System;
using System.Diagnostics;
using Unity.Mathematics;
using UnityEngine;
using VLib;

namespace VLib.Systems
{
    public class VTimeManager
    {
        static bool onEarlyUpdateInvoked;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init() => onEarlyUpdateInvoked = false;

        [Conditional("CHECK_EARLY_UPDATE_CALLED")]
        public static void CheckEarlyUpdateCalled()
        {
            if (!onEarlyUpdateInvoked)
                throw new InvalidOperationException("VTimeManager.OnEarlyUpdate() was not called at the start of the frame.");
        }
        
        public static bool HasEarlyUpdated => onEarlyUpdateInvoked;
        
        /// <summary> This must be called as early as possible in the frame. Much functionality in this class will NOT work without this method being reliably invoked at the start of the frame. </summary>
        public static void OnEarlyUpdate()
        {
            onEarlyUpdateInvoked = true;
            
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

        public static ulong SecondsToNanoSeconds(float seconds) => VTime.SecondsToNanoSeconds(seconds);
        
        // WRITING TO ANY OF THESE VALUES IS NOT GOOD VERY BAD, THEY ARE PURE CACHED FIELDS FOR PERFORMANCE REASONS
        
        /// <summary> These static fields/properties will only work when <see cref="OnEarlyUpdate"/> is called externally, otherwise these values will be default. <br/>
        /// Cached Time Values (to avoid Unity's extern properties that are not cheap according to mr.profiler) </summary>
        public static float currentTimeScale;
        /// <summary> These static fields/properties will only work when <see cref="OnEarlyUpdate"/> is called externally, otherwise these values will be default. <br/>
        /// Cached Time Values (to avoid Unity's extern properties that are not cheap according to mr.profiler) </summary>
        public static float time;

        /// <summary> These static fields/properties will only work when <see cref="OnEarlyUpdate"/> is called externally, otherwise these values will be default. <br/>
        /// Cached Time Values (to avoid Unity's extern properties that are not cheap according to mr.profiler) </summary>
        public static double timePrecise;
        /// <summary> These static fields/properties will only work when <see cref="OnEarlyUpdate"/> is called externally, otherwise these values will be default. <br/>
        /// Cached Time Values (to avoid Unity's extern properties that are not cheap according to mr.profiler) </summary>
        public static float timeUnscaled;

        /// <summary> These static fields/properties will only work when <see cref="OnEarlyUpdate"/> is called externally, otherwise these values will be default. <br/>
        /// Cached Time Values (to avoid Unity's extern properties that are not cheap according to mr.profiler) </summary>
        public static double timeUnscaledPrecise;
        /// <summary> These static fields/properties will only work when <see cref="OnEarlyUpdate"/> is called externally, otherwise these values will be default. <br/>
        /// Cached Time Values (to avoid Unity's extern properties that are not cheap according to mr.profiler) </summary>
        public static float deltaTime;
        /// <summary> These static fields/properties will only work when <see cref="OnEarlyUpdate"/> is called externally, otherwise these values will be default. <br/>
        /// Cached Time Values (to avoid Unity's extern properties that are not cheap according to mr.profiler) </summary>
        public static float smoothDeltaTime;
        /// <summary> These static fields/properties will only work when <see cref="OnEarlyUpdate"/> is called externally, otherwise these values will be default. <br/>
        /// Cached Time Values (to avoid Unity's extern properties that are not cheap according to mr.profiler) </summary>
        public static float unscaledDeltaTime;
        /// <summary> These static fields/properties will only work when <see cref="OnEarlyUpdate"/> is called externally, otherwise these values will be default. <br/>
        /// Cached Time Values (to avoid Unity's extern properties that are not cheap according to mr.profiler) </summary>
        public static int frameCount;
        
        // Integer time
        /// <summary> When cast to uint, runs for 4085 years. </summary>
        public static long Minutes => (long)(timePrecise / 60);
        /// <summary> When cast to uint, runs for 68 years. </summary>
        public static long Seconds => (long)timePrecise;
        /// <summary> When cast to uint, runs for 17 years. </summary>
        public static long QuarterSeconds => (long)(timePrecise * 4);
        /// <summary> When cast to uint, runs for 6.8 years. </summary>
        public static long TenthsOfASecond => (long)(timePrecise * 10);
        /// <summary> When cast to uint, runs for 24.8 days. </summary>
        public static long Milliseconds => (long)(timePrecise * 1000);
        
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

        public static readonly int GTimeUnscaledSin = Shader.PropertyToID("GTimeUnscaledSin");
        
        public static float ConstrainTimeStep(float step, float min = DeltaTime1000FPS, float max = DeltaTime10FPS) => math.clamp(step, min, max);

        /// <summary> Returns delta time constrained to be at max: .1f (10fps) </summary>
        public static float DeltaTimeConstrained => ConstrainTimeStep(deltaTime, 0);

        /// <summary> Returns delta time constrained to between .001f (1000fps) and .1f (10fps) </summary>
        public static float DeltaTimeConstrainedNonZero => ConstrainTimeStep(deltaTime);
    }
}