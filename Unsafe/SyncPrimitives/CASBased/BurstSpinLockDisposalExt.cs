namespace VLib
{
    public static class BurstSpinLockDisposalExt
    {
        public static void DisposeRefToDefault(ref this BurstSpinLock burstLock) 
        {
            burstLock.DisposeUnsafe();
            burstLock = default;
        }
        
        public static void DisposeRefToDefault(ref this BurstSpinLockReadWrite burstLock) 
        {
            burstLock.DisposeUnsafe();
            burstLock = default;
        }
    }
}