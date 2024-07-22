/*using System.Threading;

namespace VLib
{
    /// <summary>Adds high-speed burst compatible locking capabilities.</summary>
    public interface IInterLockable
    {
        VUnsafeRef<int> NativeLock { get; set; }
    }
    
    public static class IInterLockableExt
    {
        public static bool TryLock<T>(this T lockable, int spinBudget = 2048)
            where T : IInterLockable
        {
            int spins = spinBudget;

            // If threadlock isn't 0, the lock is taken, loop until our spinBudget is run out, then fail if lock is not acquired.
            while (Interlocked.CompareExchange(ref lockable.NativeLock.ValueRef, 1, 0) != 0)
            {
                
            }
        }
    }
}*/