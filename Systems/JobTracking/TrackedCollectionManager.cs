using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Pool;
using VLib.Threading;

namespace VLib
{
    public static class TrackedCollectionManager
    {
        static readonly Dictionary<ulong, CollectionHandles> IDToHandlesMap = new();

        struct CollectionHandles
        {
            /// <summary> The latest job in the chain to read </summary>
            JobHandle? readHandle;
            /// <summary> The latest job in the chain to write to the data </summary>
            JobHandle? writeHandle;
            
            public JobHandle ReadHandle
            {
                get => Getter(ref readHandle);
                set => Setter(ref readHandle, value);
            }
            
            public JobHandle WriteHandle
            {
                get => Getter(ref writeHandle);
                set => Setter(ref writeHandle, value);
            }
            
            public JobHandle ReadHandleDirect => readHandle ?? default;
            public JobHandle WriteHandleDirect => writeHandle ?? default;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            JobHandle Getter(ref JobHandle? handleHolder)
            {
                if (handleHolder.HasValue)
                {
                    var handle = handleHolder.Value;
                    // Try complete to keep things clean
                    if (handle.IsCompleted)
                    {
                        handle.Complete();
                        handleHolder = null;
                    }
                    else
                        return handle;
                }
                return default;
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void Setter(ref JobHandle? handleHolder, in JobHandle value)
            {
                if (handleHolder.HasValue)
                    handleHolder = JobHandle.CombineDependencies(handleHolder.Value, value);
                else
                    handleHolder = value;
            }
        }

        /// <summary> NOT THREAD SAFE </summary>
        public static JobHandle GetDependencyHandleMainThread(ulong id, bool writeAccess)
        {
            if (IDToHandlesMap.TryGetValue(id, out var handles))
            {
                // If write, await both read and write jobs already scheduled
                if (writeAccess)
                    return JobHandle.CombineDependencies(handles.WriteHandle, handles.ReadHandle);
                
                // If we're readonly, we only need to await the latest write job
                return handles.WriteHandle;
            }
            return default;
        }

        /// <summary> NOT THREAD SAFE </summary>
        public static void SetDependencyHandleMainThread(ulong id, bool writeAccess, JobHandle jobHandle)
        {
            if (!IDToHandlesMap.TryGetValue(id, out var handles))
                handles = new CollectionHandles();
            
            if (writeAccess)
                handles.WriteHandle = jobHandle;
            else
                handles.ReadHandle = jobHandle;
            
            IDToHandlesMap[id] = handles;
        }

        /// <summary> Calls <see cref="GetDependencyHandleMainThread(long,bool)"/> on each ID in the list and combines all the resulting handles. </summary>
        public static JobHandle GetDependencyHandleMainThread(List<TrackedDependency> trackedIDs)
        {
            JobHandle handle = default;
            foreach (var dependency in trackedIDs)
                handle = JobHandle.CombineDependencies(handle, GetDependencyHandleMainThread(dependency.id, dependency.writeAccess));
            return handle;
        }

        /// <summary> Will complete any jobs dependent on the tracked struct </summary>
        public static void CompleteAllJobsFor(ulong id)
        {
#if UNITY_EDITOR
            if (!MainThread.OnMain())
                throw new UnityException("CRITICAL: TrackedCollectionManager.CompleteAllJobsFor() called from non-main thread!");
#endif
            if (IDToHandlesMap.TryGetValue(id, out var handles))
            {
                handles.ReadHandleDirect.Complete();
                handles.WriteHandleDirect.Complete();
                IDToHandlesMap.Remove(id);
            }
        }

        /// <summary> Will complete any jobs dependent on the tracked structures </summary>
        public static void CompleteAllJobsFor(List<ulong> ids, bool sendListToUnityListPool)
        {
#if UNITY_EDITOR
            if (!MainThread.OnMain())
                throw new UnityException("CRITICAL: TrackedCollectionManager.CompleteAllJobsFor() called from non-main thread!");
#endif
            foreach (var id in ids)
                CompleteAllJobsFor(id);
            if (sendListToUnityListPool)
                ListPool<ulong>.Release(ids);
        }
    }
}