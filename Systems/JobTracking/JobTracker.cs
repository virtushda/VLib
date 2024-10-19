using System;

namespace VLib
{
    /// <summary> Tracker that holds no value, slightly lighter weight than <see cref="Tracked{TVal}"/>. </summary>
    public struct JobTracker : IDisposable, ITracked
    {
        public static implicit operator bool(JobTracker tracked) => tracked.IsCreated;
        public static implicit operator ulong(JobTracker tracked) => tracked.ID;
        
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
    }
}