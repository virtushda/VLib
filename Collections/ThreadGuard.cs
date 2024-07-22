using System;
using System.Threading;

namespace VLib
{
    public enum LockType : byte { Unlocked, ConcurrentRead, ReadWrite }
    
    /// <summary> Custom guard object. </summary>
    public class ThreadGuard : IThreadGuard
    {
        public const int DefaultTimeout = 10000;
        
        // Full thread guard
        protected object guard;
        public object Guard => guard;
        
        public LockType LockState => Monitor.IsEntered(guard) ? LockType.ReadWrite : LockType.Unlocked;
        
        public ThreadGuard() => guard = new();
        
        public void EngageLock() => Monitor.Enter(guard);

        public bool TryEngageLock(int millisecondsTimeout = DefaultTimeout) => Monitor.TryEnter(guard, millisecondsTimeout);

        public void ReleaseLock()
        {
            if (Monitor.IsEntered(guard))
                Monitor.Exit(guard);
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

        /// <summary> Don't forget to release the lock!! </summary> 
        public bool LockGetRef(out T objOut, int millisecondsTimeout = DefaultTimeout, bool logErrorOnBlock = true)
        {
            if (TryEngageLock(millisecondsTimeout))
            {
                objOut = obj;
                return true;
            }
            objOut = default;
            if (logErrorOnBlock)
                UnityEngine.Debug.LogError($"Failed to get lock for {obj.GetType().Name}!");
            return false;
        }

        /// <summary> Bypass the lock </summary> 
        public ref T ForceGetRef() => ref obj;

        public object GetGuardOutRef(out T objOut)
        {
            objOut = ForceGetRef();
            return guard;
        }
        
        public Lock UseLock(out T outObj) => new(this, out outObj);
        public readonly struct Lock : IDisposable
        {
            private readonly ThreadGuard<T> guard;

            public Lock(ThreadGuard<T> guard, out T outObj)
            {
                this.guard = guard;
                guard.LockGetRef(out outObj);
            }

            public void Dispose()
            {
                guard.ReleaseLock();
            }
        }
    }
}