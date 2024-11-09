using System;
using Unity.Jobs;

namespace VLib
{
    /// <summary> Tracker that holds no value, slightly lighter weight than <see cref="Tracked{TVal}"/>. </summary>
    public struct JobTracker : IDisposable, ITracked
    {
        public static implicit operator bool(JobTracker tracked) => tracked.IsCreated;
        public static implicit operator ulong(JobTracker tracked) => tracked.ID;

        /// <summary> Assumes write-access job to be on the safe side. </summary>
        public static JobTracker operator +(JobTracker tracked, JobHandle handle)
        {
            tracked.AddHandle(handle);
            return tracked;
        }

        VSafetyHandle safetyHandle;

        public bool IsCreated => safetyHandle.IsValid;
        public ulong ID
        {
            get
            {
                safetyHandle.ConditionalCheckValid();
                return safetyHandle.safetyIDCopy;
            }
        }

        JobTracker(VSafetyHandle safetyHandle) => this.safetyHandle = safetyHandle;

        public static JobTracker Create() => new(VSafetyHandle.Create());

        public void Dispose(bool logException = true) => Dispose();

        public void Dispose()
        {
            // Try to complete jobs first
            this.CompleteClearDependencies();
            safetyHandle.Dispose();
        }
        
        public void AddHandle(JobHandle handle, bool writeAccess = true)
        {
            safetyHandle.ConditionalCheckValid();
            TrackedCollectionManager.SetDependencyHandleMainThread(ID, writeAccess, JobHandle.CombineDependencies(this.GetDependencyHandle(writeAccess), handle));
        }
    }
}