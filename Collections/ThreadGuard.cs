using System;
using System.Threading;

namespace VLib
{
    public enum LockType : byte { Unlocked, ConcurrentRead, Upgradable, ReadWrite }
    
    /// <summary> Custom guard object. </summary>
    public class ThreadGuard : IThreadGuard
    {
        public const int DefaultTimeout = 10000;
        
        protected VReaderWriterLockSlim vRWLock = new();

        public LockType LockState
        {
            get
            {
                if (vRWLock.internalLock.IsWriteLockHeld)
                    return LockType.ReadWrite;
                if (vRWLock.internalLock.IsUpgradeableReadLockHeld)
                    return LockType.Upgradable;
                if (vRWLock.internalLock.IsReadLockHeld)
                    return LockType.ConcurrentRead;
                return LockType.Unlocked;
            }
        }

        /// <summary> Exclusive </summary>
        public void EngageLock() => vRWLock.internalLock.EnterWriteLock();

        /// <summary> Exclusive </summary>
        public void ReleaseLock()
        {
            if (vRWLock.internalLock.IsWriteLockHeld)
                vRWLock.internalLock.ExitWriteLock();
        }
    }
    
    /// <summary> Guard that escorts an object around. </summary>
    public class ThreadGuard<T> : ThreadGuard
    {
        T obj;

        public ThreadGuard(T obj)
        {
            this.obj = obj;
        }

        /// <summary> Bypass the lock </summary> 
        public ref T ForceGetRef() => ref obj;
        
        public ExclusiveLock ScopedExclusiveLock(out T outObj) => new(this, out outObj);
        public readonly struct ExclusiveLock : IDisposable, IThreadGuardLockExclusiveStruct<T>
        {
            readonly ThreadGuard<T> guard;
            readonly VRWLockHoldScoped rwLockHold;
            
            public ref T ObjRef => ref guard.obj;

            public ExclusiveLock(ThreadGuard<T> guard, out T outObj)
            {
                this.guard = guard;
                rwLockHold = guard.vRWLock.ScopedExclusiveLock();
                outObj = guard.obj;
            }

            public void Dispose() => rwLockHold.Dispose();
        }

        public ReadLock ScopedReadLock(out T outObj) => new(this, out outObj);
        public readonly struct ReadLock : IDisposable, IThreadGuardLockStruct<T>
        {
            readonly ThreadGuard<T> guard;
            readonly VRWLockHoldScoped rwLockHold;
            
            public ref T ObjRef => ref guard.obj;

            public ReadLock(ThreadGuard<T> guard, out T outObj)
            {
                this.guard = guard;
                rwLockHold = guard.vRWLock.ScopedReadLock();
                outObj = guard.obj;
            }

            public void Dispose() => rwLockHold.Dispose();
        }
    }
}