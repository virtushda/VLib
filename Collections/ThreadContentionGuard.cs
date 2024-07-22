using System;
using System.Threading;

namespace VLib
{
    /// <summary> Thread guard that handles read-write contention better. </summary>
    public class ThreadContentionGuard<T>
    {
        public static implicit operator T(ThreadContentionGuard<T> threadGuard) => threadGuard.obj;

        T obj;
        ReaderWriterLockSlim guard { get; }
        public int Timeout { get; set; }
        
        public ReaderWriterLockSlim Guard => guard;

        public ThreadContentionGuard(T obj, int millisecondTimeout = 10000)
        {
            this.obj = obj;
            Timeout = millisecondTimeout;
            guard = new();
        }
        
        /// <summary> Concurrent read lock, only locks writes.
        /// DON'T FORGET TO RELEASE THE LOCK! </summary> 
        public bool LockReadGet(out T objOut, bool logErrorOnBlock = true)
        {
            if (!guard.TryEnterReadLock(Timeout))
            {
                if (logErrorOnBlock)
                    UnityEngine.Debug.LogError($"Failed to get read lock for {obj.GetType().Name}!");
                objOut = default;
                return false;
            }

            objOut = obj;
            return true;
        }
        
        /// <summary> Concurrent read lock that can become a full write lock. 
        /// DON'T FORGET TO RELEASE THE LOCK! </summary> 
        public bool LockUpgradableGet(out T objOut, bool logErrorOnBlock = true)
        {
            if (!guard.TryEnterUpgradeableReadLock(Timeout))
            {
                if (logErrorOnBlock)
                    UnityEngine.Debug.LogError($"Failed to get upgradable lock for {obj.GetType().Name}!");
                objOut = default;
                return false;
            }

            objOut = obj;
            return true;
        }
        
        /// <summary> Concurrent write lock, locks all reads and writes.
        public bool LockWriteGet(out T objOut, bool logErrorOnBlock = true)
        {
            if (!guard.TryEnterWriteLock(Timeout))
            {
                if (logErrorOnBlock)
                    UnityEngine.Debug.LogError($"Failed to get write lock for {obj.GetType().Name}!");
                objOut = default;
                return false;
            }

            objOut = obj;
            return true;
        }
        
        /// <summary> Bypass the lock </summary> 
        public ref T ForceGetRef() => ref obj;

        public void UnlockRead()
        {
            if (guard.IsReadLockHeld)
                guard.ExitReadLock();
        }
        
        public void UnlockUpgradable()
        {
            if (guard.IsUpgradeableReadLockHeld)
                guard.ExitUpgradeableReadLock();
        }

        public void UnlockWrite()
        {
            if (guard.IsWriteLockHeld)
                guard.ExitWriteLock();
        }

        public Reader UseReader(out T outObj) => new(this, out outObj);
        public readonly struct Reader : IDisposable
        {
            private readonly ThreadContentionGuard<T> guard;

            public Reader(ThreadContentionGuard<T> guard, out T outObj)
            {
                this.guard = guard;
                guard.LockReadGet(out outObj);
            }

            public void Dispose()
            {
                guard.UnlockRead();
            }
        }

        public Writer UseWriter(out T objOut) => new(this, out objOut);
        public readonly struct Writer : IDisposable
        {
            private readonly ThreadContentionGuard<T> guard;

            public Writer(ThreadContentionGuard<T> guard, out T objOut)
            {
                this.guard = guard;
                guard.LockWriteGet(out objOut);
            }

            public void Dispose()
            {
                guard.UnlockWrite();
            }
        }
        
        public Upgradable UseUpgradable(out T outObj) => new(this, out outObj);
        public readonly struct Upgradable : IDisposable
        {
            private readonly ThreadContentionGuard<T> guard;

            public Upgradable(ThreadContentionGuard<T> guard, out T outObj)
            {
                this.guard = guard;
                this.guard.LockUpgradableGet(out outObj);
            }

            public void UpgradeToWrite()
            {
                guard.LockWriteGet(out _);
            }

            public void Dispose()
            {
                guard.UnlockWrite();
                guard.UnlockUpgradable();
            }
        }
    }
}