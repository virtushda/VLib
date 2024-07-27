using System;
using Unity.Mathematics;
using UnityEngine;
using VLib;

namespace VLib.Systems
{
    public class VTimeManager
    {
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

        public static float TemporalMultiplier15FPS  => Time.smoothDeltaTime / DeltaTime15FPS;
        public static float TemporalMultiplier24FPS  => Time.smoothDeltaTime / DeltaTime24FPS;
        public static float TemporalMultiplier30FPS  => Time.smoothDeltaTime / DeltaTime30FPS;
        public static float TemporalMultiplier50FPS  => Time.smoothDeltaTime / DeltaTime50FPS;
        public static float TemporalMultiplier60FPS  => Time.smoothDeltaTime / DeltaTime60FPS;
        public static float TemporalMultiplier120FPS => Time.smoothDeltaTime / DeltaTime120FPS;
        public static float TemporalMultiplier144FPS => Time.smoothDeltaTime / DeltaTime144FPS;

        public static ulong SecondsToNanoSeconds(float seconds) => VTime.SecondsToNanoSeconds(seconds);
        
        public static float currentTimeScale;
        /// <summary> These static fields/properties will only work with the TimeManager is created, move from Game to PKPersist if this is problem
        /// Cached Time Values (to avoid Unity's extern properties that are not cheap according to mr.profiler) </summary>
        public static float time;

        public static double timePrecise;
        /// <summary> These static fields/properties will only work with the TimeManager is created, move from Game to PKPersist if this is problem
        /// Cached Time Values (to avoid Unity's extern properties that are not cheap according to mr.profiler) </summary>
        public static float timeUnscaled;

        public static double timeUnscaledPrecise;
        /// <summary> These static fields/properties will only work with the TimeManager is created, move from Game to PKPersist if this is problem
        /// Cached Time Values (to avoid Unity's extern properties that are not cheap according to mr.profiler) </summary>
        public static float deltaTime;
        /// <summary> These static fields/properties will only work with the TimeManager is created, move from Game to PKPersist if this is problem
        /// Cached Time Values (to avoid Unity's extern properties that are not cheap according to mr.profiler) </summary>
        public static float smoothDeltaTime;
        /// <summary> These static fields/properties will only work with the TimeManager is created, move from Game to PKPersist if this is problem
        /// Cached Time Values (to avoid Unity's extern properties that are not cheap according to mr.profiler) </summary>
        public static float unscaledDeltaTime;
        /// <summary> These static fields/properties will only work with the TimeManager is created, move from Game to PKPersist if this is problem
        /// Cached Time Values (to avoid Unity's extern properties that are not cheap according to mr.profiler) </summary>
        public static int frameCount;

        public static int TimeSliceIndex(int slices) => frameCount % slices;
        public static bool IsTimeSliceFrame(int slices, int sliceIndex) => frameCount % slices == sliceIndex;

        /// <summary> Automatically compute a progressive step size </summary>
        public static int ProgressiveProcessCount(int elementCount, float timeSpan, int min = 1)
        {
            return timeSpan <= .01f ? elementCount : math.max(min, (int)(elementCount * math.saturate(deltaTime / timeSpan)));
        }

        public static int GTimeUnscaledSin = Shader.PropertyToID("GTimeUnscaledSin");
        
        public static float ConstrainTimeStep(float step, float min = DeltaTime1000FPS, float max = DeltaTime10FPS) => math.clamp(step, min, max);

        /// <summary> Returns delta time constrained to be at max: .1f (10fps) </summary>
        public static float DeltaTimeConstrained => ConstrainTimeStep(deltaTime, 0);

        /// <summary> Returns delta time constrained to between .001f (1000fps) and .1f (10fps) </summary>
        public static float DeltaTimeConstrainedNonZero => ConstrainTimeStep(deltaTime);

        public static void OnEarlyUpdate()
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
            //NVM, Enviro is poorly coded, there is no way to force a reflection update without disabling timeslicing >:(
            /*var enviro = EnviroSky.instance;
            if (enviro)
                enviro.UpdateReflections(true);*/
        }
    }
}