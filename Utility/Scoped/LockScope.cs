#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEBUG_LOCK_SCOPE
#endif

using System;
using System.Threading;

namespace VLib.Scoped
{
    /// <summary> Lock scope that can internally validate the state of the lock at all times, and is safe against value copies. <br/>
    /// This is the most robust lock scope, ideal for situations where you're copying the lock scope struct. </summary>
    public readonly struct LockScopeValidated : IDisposable
    {
        readonly object lockObj;
        readonly VManagedSafetyHandle.User safetyHandle;
        
        public bool IsValid => safetyHandle.IsValid;
        public static implicit operator bool(LockScopeValidated lockScope) => lockScope.IsValid;
        
        internal LockScopeValidated(object lockObj, int? timeoutMS)
        {
            this.lockObj = lockObj;
            if (Monitor.TryEnter(lockObj, timeoutMS.GetValueOrDefault(Timeout.Infinite)))
                safetyHandle = VManagedSafetyHandle.AllocateUser();
            else
                throw new TimeoutException("Failed to acquire lock within timeout!");
        }

        public void Dispose()
        {
            if (!safetyHandle.IsValid)
                throw new InvalidOperationException("LockScopeValidated is not valid and cannot be disposed!");
            Monitor.Exit(lockObj);
            safetyHandle.Dispose();
        }
        
        public void CheckValid()
        {
            if (!IsValid)
                throw new InvalidOperationException("LockScopeValidated is not valid!");
        }
    }

    /// <summary> Lightweight scoped object lock, ideal for plain using statement lock scopes. <br/>
    /// Very fast, no validation (unless DEBUG_LOCK_SCOPE), and not safe against value copies. (harder to debug improper usage) </summary>
    public readonly struct LockScopeUnsafe : IDisposable
    {
        readonly object lockObj;

#if DEBUG_LOCK_SCOPE
        readonly VManagedSafetyHandle.User safetyHandle;
#endif

        internal LockScopeUnsafe(object lockObj, int? timeoutMS)
        {
            if (!Monitor.TryEnter(lockObj, timeoutMS.GetValueOrDefault(Timeout.Infinite)))
                throw new TimeoutException($"Failed to acquire lock within timeout {timeoutMS}ms!");
            this.lockObj = lockObj;
#if DEBUG_LOCK_SCOPE
            safetyHandle = VManagedSafetyHandle.AllocateUser();
#endif
        }

        public void Dispose()
        {
#if DEBUG_LOCK_SCOPE
            if (!safetyHandle.IsValid)
                throw new InvalidOperationException("LockScopeUnsafe is not valid and cannot be disposed!");
            safetyHandle.Dispose();
#endif
            Monitor.Exit(lockObj);
        }
        
        public void CheckValid()
        {
#if DEBUG_LOCK_SCOPE
            if (!safetyHandle.IsValid)
                throw new InvalidOperationException("LockScopeUnsafe is not valid!");
#endif
            if (lockObj == null || !Monitor.IsEntered(lockObj))
                throw new InvalidOperationException("LockScopeUnsafe is not valid!");
        }
    }
    
    public static class LockScopeExt
    {
        /// <inheritdoc cref="LockScopeValidated"/>
        /// <param name="lockObj">Object to lock over, ensure this is a proper object to lock on. (Would you 'lock(this)' typically?).</param>
        /// <param name="timeoutMS">Timeout in milliseconds, or null for infinite. </param>
        public static LockScopeValidated ScopedLockValidated(this object lockObj, int? timeoutMS = null)
        {
            if (lockObj.GetType() != typeof(object))
                throw new ArgumentException("Lock object must be of type 'object'!");
            return new LockScopeValidated(lockObj, timeoutMS);
        }

        /// <inheritdoc cref="LockScopeUnsafe"/>
        /// <param name="lockObj">Object to lock over, ensure this is a proper object to lock on. (Would you 'lock(this)' typically?).</param>
        /// <param name="timeoutMS">Timeout in milliseconds, or null for infinite. </param>
        public static LockScopeUnsafe ScopedLockUnsafe(this object lockObj, int? timeoutMS = null)
        {
            if (lockObj.GetType() != typeof(object))
                throw new ArgumentException("Lock object must be of type 'object'!");
            return new LockScopeUnsafe(lockObj, timeoutMS);
        }
    }
}