using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

namespace VLib.EventHandling
{
    /// <summary> Automatically invoke events with a optimal timeslicing algorithm. </summary>
    public class TimeslicedActionEvent
    {
        public List<Action> Actions = new();

        /// <summary> The point around which the time period cycles </summary>
        public float TimeBase { get; private set; }

        /// <summary> How long to complete a revolution of the events if we can execute at full speed </summary>
        public float TimePeriod { get; private set; }

        /// <summary> Where are we currently in execution </summary>
        public int ExecutionIndex { get; private set; }

        /// <summary> What is the max tolerated execution time? </summary>
        public float MaxMS { get; private set; }

        public static TimeslicedActionEvent operator +(TimeslicedActionEvent t, Action a)
        {
            t.Actions.Add(a);
            return t;
        }

        public static TimeslicedActionEvent operator -(TimeslicedActionEvent t, Action a)
        {
            t.Actions.Remove(a);
            return t;
        }

        public TimeslicedActionEvent(float timePeriod, float maxMS)
        {
            TimePeriod = timePeriod;
            MaxMS = maxMS;
            
            if (timePeriod < .01f)
                Debug.LogError("TimeslicedActionEvent: Time period is too small, will execute all actions in one frame");
        }

        public void AutoInvoke()
        {
            if (Actions.Count < 1)
                return;

            // Determine target chunk of work
            int endIndex = ExecutionIndex + 1;
            endIndex += TimePeriod < .01f ? endIndex = Actions.Count : (int) math.ceil(Systems.VTime.unscaledDeltaTime / TimePeriod);
            endIndex = math.min(endIndex, Actions.Count);

            var stopwatch = ValueStopwatch.StartNew();

            for (int i = ExecutionIndex; i < endIndex; i++)
            {
                if (stopwatch.ElapsedMillisecondsF >= MaxMS)
                    break;
                if (Actions.TryGet(i, out var action))
                {
                    Profiler.BeginSample($"T-{action.Target.GetType()} Method-{action.Method.Name}");
                    try
                    {
                        action.Invoke();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                    Profiler.EndSample();
                }
                else
                    Debug.LogError($"TimeslicedActionEvent: Action at index {i} is null");

                ExecutionIndex++;
            }

            if (ExecutionIndex >= Actions.Count)
                ExecutionIndex = 0;
        }
    }
}