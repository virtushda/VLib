using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Jobs;
using UnityEngine.Profiling;
using VLib.Threading;
using Debug = UnityEngine.Debug;

namespace VLib
{
    public static class ITrackedJobExt
    {
        static readonly SimpleListPool<TrackedDependency> ListPool = new SimpleListPool<TrackedDependency>(4, 8);
        
        /// <summary> Auto grabs the right type of list from a pool. </summary>
        public static List<TrackedDependency> GrabDepsTrackingList<T>(ref this T tracked)
            where T : struct, ITrackedJob
        {
            return ListPool.Fetch(); // ListPool<TrackedDependency>.Get(); Unity's list pool is slow as hell
        }
        
        public static void ReleaseDepsTrackingList<T>(ref this T tracked, List<TrackedDependency> list)
            where T : struct, ITrackedJob
        {
            ListPool.Repool(list); //ListPool<TrackedDependency>.Release(list); Unity's list pool is slow as hell
        }
        
        static JobHandle DetermineDependencies<J>(ref this J job, List<TrackedDependency> trackedIDs)
            where J : struct, ITrackedJob
        {
            Profiler.BeginSample(nameof(DetermineDependencies));
            
            // Gather dependencies
            var dependencies = TrackedCollectionManager.GetDependencyHandleMainThread(trackedIDs);
            
            Profiler.EndSample();
            return dependencies;
        }
        
        static void RegisterNewDependenciesAndCleanup(in List<TrackedDependency> trackedIDs, in JobHandle handle)
        {
            Profiler.BeginSample(nameof(RegisterNewDependenciesAndCleanup));
            
            // Send requirements back in
            foreach (var trackedTuple in trackedIDs)
                TrackedCollectionManager.SetDependencyHandleMainThread(trackedTuple.id, trackedTuple.writeAccess, handle);

            // Return list
            ListPool.Repool(trackedIDs);
            
            Profiler.EndSample();
        }
        
        /// <summary> Special scheduler for tracked IJobs </summary>
        public static JobHandle ScheduleTracked<J>(ref this J job, List<TrackedDependency> dependencies, JobHandle addInDeps = default)
            where J : struct, IJob, ITrackedJob
        {
            Profiler.BeginSample(nameof(ScheduleTracked));
            
            var dependsOn = DetermineDependencies(ref job, dependencies);
            
            // Schedule job
            var handle = job.Schedule(JobHandle.CombineDependencies(dependsOn, addInDeps));
            
            RegisterNewDependenciesAndCleanup(dependencies, handle);

            Profiler.EndSample();
            
            // Return handle
            return handle;
        }

        ///<summary> Performs an extra struct copy, but is useful in tight spots where you can't ref a job struct nicely. </summary>
        public static JobHandle ScheduleTrackedCopy<J>(this J job, List<TrackedDependency> dependencies, JobHandle addInDeps = default)
            where J : struct, IJob, ITrackedJob
        {
            return job.ScheduleTracked(dependencies, addInDeps);
        }

        /// <summary> Special scheduler for tracked IJobParallelFors </summary>
        public static JobHandle ScheduleTracked_ParallelFor<J>(ref this J job, int arrayLength, int innerloopBatchCount, List<TrackedDependency> dependencies, JobHandle addInDeps = default)
            where J : struct, IJobParallelFor, ITrackedJob
        {
            Profiler.BeginSample(nameof(ScheduleTracked_ParallelFor));
            
            var dependsOn = DetermineDependencies(ref job, dependencies);
            
            // Schedule job
            var handle = job.Schedule(arrayLength, innerloopBatchCount, JobHandle.CombineDependencies(dependsOn, addInDeps));
            
            RegisterNewDependenciesAndCleanup(dependencies, handle);

            Profiler.EndSample();
            
            // Return handle
            return handle;
        }

        /// <summary> Special scheduler for tracked IJobParallelFors </summary>
        public static JobHandle ScheduleTracked_ParallelForBatch<J>(ref this J job, int arrayLength, List<TrackedDependency> dependencies, JobHandle addInDeps = default, float approxBatchesPerThread = 4f, int minBatchSize = 2)
            where J : struct, IJobParallelForBatch, ITrackedJob
        {
            Profiler.BeginSample(nameof(ScheduleTracked_ParallelForBatch));

            if (arrayLength < 1)
            {
                Debug.LogError("Cannot schedule a job with an array length of 0!");
                Profiler.EndSample();
                return default;
            }
            
            var dependsOn = DetermineDependencies(ref job, dependencies);
            
            // Schedule job
            var handle = job.ScheduleAutoBatched(arrayLength, JobHandle.CombineDependencies(dependsOn, addInDeps), approxBatchesPerThread, minBatchSize);
            
            RegisterNewDependenciesAndCleanup(dependencies, handle);

            Profiler.EndSample();
            
            // Return handle
            return handle;
        }
        
        public static JobHandle ScheduleTracked_ParallelForTransform<J>(ref this J job, in TransformAccessArray transforms, List<TrackedDependency> dependencies, JobHandle addInDeps = default)
            where J : struct, IJobParallelForTransform, ITrackedJob
        {
            Profiler.BeginSample(nameof(ScheduleTracked_ParallelForTransform));
            
            var dependsOn = DetermineDependencies(ref job, dependencies);
            
            // Schedule job
            var handle = job.ScheduleByRef(transforms, JobHandle.CombineDependencies(dependsOn, addInDeps));
            
            RegisterNewDependenciesAndCleanup(dependencies, handle);

            Profiler.EndSample();
            
            // Return handle
            return handle;
        }
        
        public static JobHandle ScheduleTracked_ParallelForTransformReadonly<J>
            (ref this J job, in TransformAccessArray transforms, int batchSize, List<TrackedDependency> dependencies, JobHandle addInDeps = default)
            where J : struct, IJobParallelForTransform, ITrackedJob
        {
            Profiler.BeginSample(nameof(ScheduleTracked_ParallelForTransformReadonly));
            
            var dependsOn = DetermineDependencies(ref job, dependencies);
            
            // Schedule job
            var handle = job.ScheduleReadOnlyByRef(transforms, batchSize, JobHandle.CombineDependencies(dependsOn, addInDeps));
            
            RegisterNewDependenciesAndCleanup(dependencies, handle);

            Profiler.EndSample();
            
            // Return handle
            return handle;
        }
    }
}