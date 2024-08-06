using System;
using Unity.Jobs;
using UnityEngine.Profiling;

namespace VLib
{
    public struct BackgroundProcessingJob : IJob
    {
        public void Execute()
        {
            if (BackgroundTaskSystem.instance == null)
                return;
            
            var system = BackgroundTaskSystem.instance;

            var budget = BackgroundTaskSystem.BudgetPerJob;
            while (budget > 0 && system.TakeWork(out var task))
            {
                Profiler.BeginSample($"Task: {task.GetType()}");
                
                // Manage job budget
                var taskSize = task.Size;
                budget -= (int) taskSize;
                
                try
                {
                    task.Execute();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError(e);
                    task.State = BackgroundTaskState.Error;
                    Profiler.EndSample();
                    continue;
                }
                
                system.completeQueue.Enqueue(task);
                Profiler.EndSample();
            }
        }
    }
}