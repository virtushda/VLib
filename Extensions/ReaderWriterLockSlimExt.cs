using System;
using System.Threading;

namespace VLib
{
    public static class ReaderWriterLockSlimExt
    {
        ///<summary> Returns a wrapper struct where the constructor locks (for concurrent READ) and the dispose call unlocks, use with 'using' keyword. </summary>
        public static RWLockSlimReadScoped ScopedReadLock(this ReaderWriterLockSlim rwLock, int timeoutMS = -1) => new(rwLock, timeoutMS);

        ///<summary> Returns a wrapper struct where the constructor locks (EXCLUSIVE) and the dispose call unlocks, use with 'using' keyword. </summary>
        public static RWLockSlimWriteScoped ScopedExclusiveLock(this ReaderWriterLockSlim rwLock, int timeoutMS = -1) => new(rwLock, timeoutMS);
    }
    
    public readonly struct RWLockSlimReadScoped : IDisposable
    {
        private readonly ReaderWriterLockSlim rwLock;
        public readonly bool isLocked;

        ///<summary> Automatically takes the read lock with an optional timeout... </summary>
        ///<param name="rwLock"> The ReaderWriterLockSlim to take the read lock on </param>
        ///<param name="timeoutMS"> The timeout in milliseconds to wait for the lock, or -1 to wait indefinitely </param>
        public RWLockSlimReadScoped(ReaderWriterLockSlim rwLock, int timeoutMS)
        {
            this.rwLock = rwLock;
            
            if (timeoutMS < 0)
                this.rwLock.EnterReadLock();
            else if (!rwLock.TryEnterReadLock(timeoutMS))
                throw new TimeoutException($"Failed to acquire read lock within {timeoutMS}ms!");
            
            isLocked = true;
        }

        public void Dispose() => rwLock.ExitReadLock();
    }
    
    public readonly struct RWLockSlimWriteScoped : IDisposable
    {
        private readonly ReaderWriterLockSlim rwLock;
        public readonly bool isLocked;

        ///<summary> Automatically takes the write lock with an optional timeout... </summary>
        ///<param name="rwLock"> The ReaderWriterLockSlim to take the write lock on </param>
        ///<param name="timeoutMS"> The timeout in milliseconds to wait for the lock, or -1 to wait indefinitely </param>
        public RWLockSlimWriteScoped(ReaderWriterLockSlim rwLock, int timeoutMS)
        {
            this.rwLock = rwLock;
            
            if (timeoutMS < 0)
                this.rwLock.EnterWriteLock();
            else if (!rwLock.TryEnterWriteLock(timeoutMS))
                throw new TimeoutException($"Failed to acquire write lock within {timeoutMS}ms!");
            
            isLocked = true;
        }

        public void Dispose() => rwLock.ExitWriteLock();
    }
}