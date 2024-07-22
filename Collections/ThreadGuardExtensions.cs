using System;
using Unity.Collections;

namespace VLib
{
    public interface IThreadGuard
    {
        LockType LockState { get; }
        
        void EngageLock();
        
        void ReleaseLock();
    }
    
    public static class ThreadGuardExtensions
    {
        /// <summary> Avoid boilerplate at the cost of tiny alloc. We have incremental GC, shouldn't be a problem with moderate use. </summary>
        public static void RunSafelyInLock<TGuard>(this TGuard guard, Action action)
            where TGuard : IThreadGuard
        {
            guard.EngageLock();
            try
            {
                action();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
            finally
            {
                guard.ReleaseLock();
            }
        }
        
        /// <summary> Avoid boilerplate at the cost of tiny alloc. We have incremental GC, shouldn't be a problem with moderate use. </summary>
        public static T RunSafelyInLock<TGuard, T>(TGuard guard, Func<T> action)
            where TGuard : IThreadGuard
        {
            guard.EngageLock();
            try
            {
                return action();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
            finally
            {
                guard.ReleaseLock();
            }
            return default;
        }
    }
}