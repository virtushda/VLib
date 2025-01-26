using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using static Unity.Mathematics.math;

namespace VLib
{
    public static class VJobsUtil
    {
        /// <summary>Gets an automatically optimized batch size. Info used are total available job threads, and a value for how many batches per thread.
        /// The splits allow the job scheduler to efficiently saturate threads, while still gaining the benefits of batching.</summary>
        public static int EfficientParallelBatchSize(int totalIterations, float approxSplitsPerThread = 4, int minBatchSize = 2)
        {
            return (int) max(minBatchSize, ceil(totalIterations / (float) JobsUtility.JobWorkerMaximumCount / approxSplitsPerThread));
        }

        /// <summary>Schedules a job with an automatically optimized batch size. Info used are total available job threads, and a value for how many batches per thread.
        /// The splits allow the job scheduler to efficiently saturate threads, while still gaining the benefits of batching.</summary>
        public static JobHandle ScheduleAutoBatched<T>(this T jobstruct, int totalIterations, JobHandle indeps = default, 
            float approxBatchesPerThread = 4, int minBatchSize = 2, bool logOnZeroIterationCount = true)
            where T : struct, IJobParallelForBatch
        {
            if (totalIterations <= 0)
            {
                if (logOnZeroIterationCount)
                    Debug.LogError("Total iterations must be greater than 0.");
                return indeps;
                //throw new System.ArgumentOutOfRangeException(nameof(totalIterations), "Total iterations must be greater than 0.");
            }
            int batchSize = EfficientParallelBatchSize(totalIterations, approxBatchesPerThread, minBatchSize);
            return jobstruct.ScheduleBatch(totalIterations, batchSize, indeps);
        }
    }
}