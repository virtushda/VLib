using System.Collections.Generic;

namespace VLib
{
    /// <summary> <para> This job uses automatic job dependency handling. </para>
    /// <para>Usage:</para>
    /// <para>Jobs implementing ITrackedJob are expected to collect and report their dependencies in their constructor. </para>
    /// <para> Use this.GrabDepsTrackingList() to call an ITrackedJob extension that will give you a pooled list of the correct type for collecting dependencies. </para>
    /// <para> 'Out' this list from the constructor, and it can be used to schedule the jobs with automatic dependency handling using the ScheduleTracked_* extension methods. </para>
    /// <para> The pooled list will be automatically repooled in the ScheduleTracked_* call. </para></summary>
    public interface ITrackedJob
    {
        // We can't really enforce tracking code architecture nicely, it is best done in the constructor of a job
        
        /// <summary> When the job schedules, the caller needs to know what tracked IDs you are submitting and whether they require write access to the structures they are tracking. </summary>
        //public void OnTrackedSchedule(in List<TrackedDependency> dependencies);
    }

    /*// TEST
    public struct JobStruct : IJob, ITrackedJob
    {
        Tracked<UnsafeList<int>> list;

        public void Execute()
        {
            throw new System.NotImplementedException();
        }

        public void OnTrackedSchedule(ref List<TrackedDependency> trackedIDs) => trackedIDs.Add(list.AsDependency(false));
    }*/
}