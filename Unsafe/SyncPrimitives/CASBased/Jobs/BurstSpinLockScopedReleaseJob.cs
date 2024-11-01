using Unity.Jobs;

namespace VLib.Jobs
{
    public struct BurstSpinLockScopedReleaseJob : IJob
    {
        BurstSpinLockScoped scopedLock;
        
        public BurstSpinLockScopedReleaseJob(BurstSpinLockScoped scopedLock) => this.scopedLock = scopedLock;
        
        public void Execute() => scopedLock.Dispose();
    }
}