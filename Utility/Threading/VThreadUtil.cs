using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Burst;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

namespace VLib.Threading
{
    public static class VThreadUtil
    {
        /// <summary> Gets the current thread ID. <br/>
        /// If in a job, get the job thread ID. <br/>
        /// If in managed code, get the actual 'Thread' ID. <br/>
        /// If in a burst function, we may fail. (no way to do this yet?) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VThreadID CurrentThreadID()
        {
            if (JobsUtility.IsExecutingJob)
            {
                // Managed/Burst Job
                return new VThreadID(VThreadID.ThreadIDType.JobsUtilityThreadIndex, JobsUtility.ThreadIndex);
            }
            else
            {
                // Managed, outside of job
                int threadID = int.MinValue;
                ManagedThreadID(ref threadID);
                // Check if the function was stripped (we may be in burst, but not in a job if burst func?)
                if (threadID == int.MinValue)
                    throw new InvalidOperationException("Cannot get thread ID! (Not in job, or managed thread).");
                return new VThreadID(VThreadID.ThreadIDType.ManagedThreadID, threadID);
            }
            throw new InvalidOperationException("Failed to get thread ID!");
        }

        /// <summary> TryGet version of <see cref="CurrentThreadID"/> </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetCurrentThreadID(out VThreadID threadID, bool logging = false)
        {
            if (JobsUtility.IsExecutingJob)
            {
                // Managed/Burst Job
                threadID = new VThreadID(VThreadID.ThreadIDType.JobsUtilityThreadIndex, JobsUtility.ThreadIndex);
                return true;
            }

            // Managed, outside of job
            int threadIDTemp = int.MinValue;
            ManagedThreadID(ref threadIDTemp);
            
            // Check if the function was stripped (we may be in burst, but not in a job if burst func?)
            if (threadIDTemp == int.MinValue)
            {
                if (logging)
                    Debug.LogError("Cannot get thread ID! (Not in job, or managed thread).");
                threadID = default;
                return false;
            }
            
            threadID = new VThreadID(VThreadID.ThreadIDType.ManagedThreadID, threadIDTemp);
            return true;
        }
        
        [BurstDiscard, MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ManagedThreadID(ref int threadID) => threadID = Thread.CurrentThread.ManagedThreadId;
    }
}