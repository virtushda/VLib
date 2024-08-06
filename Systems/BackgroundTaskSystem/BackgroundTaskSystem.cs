using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Unity.Jobs;
using UnityEngine.Profiling;

namespace VLib
{
    /// <summary> A subsystem for scheduling work from anywhere and getting the results back.
    /// Create one of these systems, then create derivative classes of BackgroundTaskBase as vehicles for background processing. </summary>
    public class BackgroundTaskSystem
    {
        const int MaxJobs = 4;
        /// <summary> Consumption defined by <see cref="BackgroundTaskSize"/> </summary>
        public const int BudgetPerJob = 32;
        const int ThresholdForNewJob = BudgetPerJob * 2;

        internal static BackgroundTaskSystem instance;
        
        long nextTaskID = 0;
        int totalWorkEstimate = 0;

        /// <summary> Work comes in here </summary>
        ConcurrentQueue<BackgroundTaskBase> workQueue = new();
        /// <summary> Work goes out here </summary>
        internal ConcurrentQueue<BackgroundTaskBase> completeQueue = new();
        List<JobHandle> handles = new();
        
        public BackgroundTaskSystem() => instance = this;

        public void Dispose()
        {
            foreach (var handle in handles)
                handle.Complete();
            
            instance = null;
            workQueue = null;
            completeQueue = null;
            handles = null;
        }
        
        /// <summary> Main thread only, call as often as you want. </summary>
        public void UpdateSystem()
        {
            Profiler.BeginSample("BackgroundTasks-WorkComplete");
            
            // Check for job completion
            for (var handleIndex = handles.Count - 1; handleIndex >= 0; handleIndex--)
            {
                var handle = handles[handleIndex];
                if (handle.IsCompleted)
                {
                    handle.Complete();
                    handles.RemoveAt(handleIndex);
                }
            }
            
            // Complete tasks
            while (completeQueue.TryDequeue(out var task))
                task.Complete();
            
            Profiler.EndSample();
            
            Profiler.BeginSample("BackgroundTasks-JobSchedule");
            
            // If there are job threads available and work to do, schedule more work
            if (handles.Count < MaxJobs && totalWorkEstimate > 0)
            {
                // Schedule work with a variable number of job threads and a maximum number of concurrent jobs
                int workAmount = totalWorkEstimate;
                do
                {
                    workAmount -= ThresholdForNewJob;
                    var job = new BackgroundProcessingJob();
                    handles.Add(job.Schedule());
                } while (workAmount > ThresholdForNewJob && handles.Count < MaxJobs);
            }

            Profiler.EndSample();
        }
        
        /// <summary> Thread-safe </summary>
        internal void SubmitWork(BackgroundTaskBase task)
        {
            //task.TaskID = Interlocked.Increment(ref nextTaskID);
            workQueue.Enqueue(task);
            Interlocked.Add(ref totalWorkEstimate, (int)task.Size);
        }
        
        /// <summary> Thread-safe </summary>
        internal bool TakeWork(out BackgroundTaskBase task)
        {
            if (!workQueue.TryDequeue(out task))
                return false;
            
            Interlocked.Add(ref totalWorkEstimate, -(int)task.Size);
            AssertWorkEstimateValid();
            return true;
        }
        
        [Conditional("UNITY_EDITOR")]
        void AssertWorkEstimateValid()
        {
            if (totalWorkEstimate < 0)
                throw new Exception($"Work estimate is negative! Estimate: {totalWorkEstimate}");
            if (totalWorkEstimate > 99999)
                throw new Exception($"Work estimate is obscene! Estimate: {totalWorkEstimate}");
        }
    }
}