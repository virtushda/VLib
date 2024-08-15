using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace VLib
{
    public enum TryGetResult
    {
        Null,
        WrongType,
        Valid
    }
    
    public static unsafe class VNativeSentinels
    {
        static Dictionary<IntPtr, NativeSentinelBase> pointerSentinelMapUnsafe;
        static Dictionary<IntPtr, NativeSentinelBase> PointerSentinelMap => pointerSentinelMapUnsafe ??= new Dictionary<IntPtr, NativeSentinelBase>();

        static Dictionary<uint, NativeSentinelBase> idSentinelMap;
        static Dictionary<uint, NativeSentinelBase> IDSentinelMap => idSentinelMap ??= new Dictionary<uint, NativeSentinelBase>();

        /// <summary> Create or Fetch an Existing NativeSentinel for any type of NativeArray. Invalid input will return null!
        /// Resizing the NativeArray will break this! </summary>
        public static NativeSentinel<NativeArray<T>> SentinelForNativeCollection<T>(NativeArray<T> arrayToGuard)
            where T : unmanaged
        {
            //Too damned difficult to reliably check the status of a native collection!
            try
            {
                IntPtr arrayPnt = (IntPtr)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(arrayToGuard);
                //Old code would do a read check on a native array that was just scheduled, throwing an exception.
                //I don't need read-access, just the buffer pointer.
                //IntPtr arrayPnt = (IntPtr)arrayToGuard.GetUnsafeReadOnlyPtr();
                return GetSentinelFromPtr(arrayPnt, arrayToGuard);
            }
            catch
            {
                return null;
            }
        }

        static NativeSentinel<TObjToGuard> GetSentinelFromPtr<TObjToGuard>(IntPtr ptr, TObjToGuard objToGuard)
            where TObjToGuard : struct
        {
            if (PointerSentinelMap.TryGetValue(ptr, out var existingSentinel))
            {
                if (existingSentinel != null)
                    return (NativeSentinel<TObjToGuard>)existingSentinel;
                else
                    PointerSentinelMap.Remove(ptr);
            }

            var newSentinel = new NativeSentinel<TObjToGuard>(ref objToGuard);

            PointerSentinelMap.Add(ptr, newSentinel);
            return newSentinel;
        }
        
        /// <summary> Ideal way to get a sentinel, creates and maintains a sentinel for each unique ID. </summary>
        /// <param name="idable">IDable object, such as a NativeArray wrapped with IDd.</param>
        /// <returns>Sentinel matching the ID</returns>
        public static NativeSentinel<T> GetSentinelFromID<T>(ref T idable)
            where T : struct, IUniqueID32U
        {
            if (TryGetResult.Valid == TryGetSentinelFromID<T>(idable, out NativeSentinel<T> nativeSentinel))
                return nativeSentinel;

            return NewSentinelFor(idable);
        }

        public static TryGetResult TryGetSentinelFromID<T>(T idable, out NativeSentinel<T> nativeSentinel)
            where T : struct, IUniqueID32U
        {
            return TryGetSentinelFromIDInternal(idable.UniqueID, out nativeSentinel, true);
        }

        public static void CompleteRemoveExistingSentinelForID<T>(T idable)
            where T : struct, IUniqueID32U
        {
            UnsafeCompleteRemoveExistingSentinelForID<T>(idable.UniqueID);
        }

        public static void UnsafeCompleteRemoveExistingSentinelForID<T>(uint id)
            where T : struct, IUniqueID32U
        {
            if (TryGetResult.Valid == TryGetSentinelFromIDInternal<T>(id, out var sentinel, true))
            {
                sentinel.CompleteClearAllJobs();
                RemoveSentinelByID(id);
            }
        }
        
        #region Internal

            static TryGetResult TryGetSentinelFromIDInternal<T>(uint id, out NativeSentinel<T> nativeSentinel, bool suppressErrors = false)
                where T : struct, IUniqueID32U
            {
                if (IDSentinelMap.TryGetValue(id, out var sentinelBase))
                {
                    if (sentinelBase is NativeSentinel<T> sentinelCast)
                    {
                        nativeSentinel = sentinelCast;
                        return TryGetResult.Valid;
                    }

                    if (!suppressErrors)
                        Debug.LogError("Native Sentinel of wrong type??? Please investigate.");
                    nativeSentinel = null;
                    return TryGetResult.WrongType;
                }

                nativeSentinel = null;
                return TryGetResult.Null;
            }

            static NativeSentinel<T> NewSentinelFor<T>(T idable)
                where T : struct, IUniqueID32U
            {
                //Check for old incorrect sentinel and handle
                if (idSentinelMap.TryGetValue(idable.UniqueID, out var sentinelBase))
                {
                    sentinelBase.CompleteClearAllJobs();
                    idSentinelMap.Remove(idable.UniqueID);
                }

                var sentinel = new NativeSentinel<T>(ref idable);
                idSentinelMap.Add(idable.UniqueID, sentinel);
                return sentinel;
            }

            static void RemoveSentinelByID(uint id) => idSentinelMap.Remove(id);

        #endregion
    }

    public abstract class NativeSentinelBase
    {
        HashList<JobHandle> activeJobs;

        protected NativeSentinelBase()
        {
            activeJobs = new HashList<JobHandle>();
        }

        public bool CheckJobDependencies(bool generateDependencyHandle, out JobHandle dependencyHandle)
        {
            bool hasDependency = false;
            dependencyHandle = default;

            for (int i = 0; i < activeJobs.Count; i++)
            {
                if (activeJobs[i].IsCompleted)
                {
                    activeJobs[i].Complete();
                    activeJobs.RemoveAt(i);
                    i--;
                    continue;
                }

                if (generateDependencyHandle)
                    dependencyHandle = JobHandle.CombineDependencies(dependencyHandle, activeJobs[i]);

                hasDependency = true;
            }

            return hasDependency;
        }

        public void CompleteClearAllJobs()
        {
            for (int i = 0; i < activeJobs.Count; i++)
                activeJobs[i].Complete();

            activeJobs.Clear();
        }

        public void AddDependentJob(ref JobHandle handle)
        {
            activeJobs.Add(handle);
        }

        public void AddDependentJob(JobHandle handle)
        {
            activeJobs.Add(handle);
        }

        public void RemoveDependentJob(ref JobHandle handle)
        {
            activeJobs.Remove(handle);
        }

        public void RemoveDependentJob(JobHandle handle)
        {
            activeJobs.Remove(handle);
        }

        /*public JobHandle ChainJob(JobHandle handle)
        {
            if (CheckJobDependencies(true, out var newDeps))
        }*/

        public void ClearJobDependencies()
        {
            for (int i = 0; i < activeJobs.Count; i++)
            {
                if (!activeJobs[i].IsCompleted)
                    Debug.LogError($"Uncompleted JobHandle dependency '{activeJobs[i]}' was cleared...");
            }

            activeJobs.Clear();
        }
    }

    /// <summary>
    /// Use to protect native collections that are being modified
    /// </summary>
    public class NativeSentinel<T> : NativeSentinelBase
        where T : struct
    {
        T objectToGuard;

        //public T ObjectToGuard { get => objectToGuard; }
        public ref T ObjectToGuard()
        {
            return ref objectToGuard;
        }

        /// <summary> The preferred way to use this class is to call NativeJobSentinelManager.SentinelForNativeCollection or whatever you need! </summary>
        public NativeSentinel(ref T objectToGuard) : base()
        {
            this.objectToGuard = objectToGuard;
        }
    }
}