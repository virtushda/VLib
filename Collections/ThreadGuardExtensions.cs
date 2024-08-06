using System;
using System.Collections.Generic;
using Unity.Collections;

namespace VLib
{
    public interface IThreadGuard
    {
        LockType LockState { get; }
        
        void EngageLock();
        
        void ReleaseLock();
    }

    public interface IThreadGuardLockStruct<T>
    {
        bool IsLocked { get; }
        ref T ObjRef { get; }
    }
    
    public interface IThreadGuardLockExclusiveStruct<T> : IThreadGuardLockStruct<T> { }

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

        /*public static int Count<TList, TElement>(this IThreadGuardLockStruct<TList> guardStruct)
            where TList : ICollection<TElement>
        {
            return guardStruct.ObjRef.Count;
        }
        
        public static TElement Get<TList, TElement>(this IThreadGuardLockStruct<TList> guardStruct, int index)
            where TList : IList<TElement>
        {
            return guardStruct.ObjRef[index];
        } 
        
        public static void Set<TList, TElement>(this IThreadGuardLockExclusiveStruct<TList> guardStruct, int index, in TElement value)
            where TList : IList<TElement>
        {
            guardStruct.ObjRef[index] = value;
        }
        
        public static void Add<TList, TElement>(this IThreadGuardLockExclusiveStruct<TList> guardStruct, in TElement value)
            where TList : ICollection<TElement>
        {
            guardStruct.ObjRef.Add(value);
        }
        
        public static void Remove<TList, TElement>(this IThreadGuardLockExclusiveStruct<TList> guardStruct, in TElement value)
            where TList : ICollection<TElement>
        {
            guardStruct.ObjRef.Remove(value);
        }
        
        public static void CopyTo<TList, TElement>(this IThreadGuardLockStruct<TList> guardStruct, TElement[] array, int arrayIndex)
            where TList : ICollection<TElement>
        {
            guardStruct.ObjRef.CopyTo(array, arrayIndex);
        }*/
    }
}