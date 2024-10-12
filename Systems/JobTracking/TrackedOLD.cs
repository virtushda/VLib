/*using System;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

namespace VLib
{
    /// <summary>
    /// Wrapped for native collections that integrates with the Native Sentinel Seth Shit, Simplifies chaining unrelated jobs.
    /// Multiply to create compound job handle.
    /// Add to create lists of Tracked-T.
    /// </summary>
    public struct Tracked<TVal> : IDisposable//, ITracked
        where TVal : struct, IDisposable
    {
        public TVal value;
        bool hasID;
        uint id;
        //bool keepSentinel;

        public bool HasID => hasID;

        public uint ID
        {
            get => hasID ? id : throw new UnityException("ID'd Collection has not been initialized correctly.");
            //set => throw new UnityException("Setting the ID directly is illegal on this type.");
        }

        public bool IsCreated => hasID;

        /*public bool KeepSentinel
        {
            get => keepSentinel;
            set => keepSentinel = value;
        }#1#

        public Tracked(in TVal value) : this()
        {
            this.value = value;

            id = TrackedCollectionManager.FetchID();
            hasID = true;

            //TrackedCollectionManager.InputBoxed(this);
        }

        public void Dispose() => Dispose(true);

        public void Dispose(bool reportException)
        {
            //Ensure Jobs on collection are complete and handle sentinel object
            if (hasID)
            {
                /*if (keepSentinel)
                {
                    if (TryGetResult.Valid == VNativeSentinels.TryGetSentinelFromID(this, out NativeSentinel<Tracked<TVal>> sentinel))
                        sentinel.CompleteClearAllJobs();
                }
                else
                    VNativeSentinels.CompleteRemoveExistingSentinelForID(this);#1#
            }

            //Gotta protect this call, some code is awkward and disposes this before it's initialized...
            try
            {
                value.Dispose();
            }
            catch (Exception e)
            {
                if (reportException)
                {
                    Debug.LogError("Caught exception while disposing internal value... Logging exception and continuing...");
                    Debug.LogException(e);
                }
                else
                    Debug.LogWarning($"Caught intentionally muted exception while disposing internal value, msg: {e.Message}");
            }

            if (hasID)
            {
                //TrackedCollectionManager.RemoveBoxed(this);
                TrackedCollectionManager.ReturnID(id);
            }

            hasID = false;
        }

        public int CompareTo(uint other) => id.CompareTo(other);

        //Conversion

        public static implicit operator TVal(Tracked<TVal> tracked) => tracked.value;
        public static implicit operator uint(Tracked<TVal> tracked) => tracked.id;

        //Sentinel Stuff

        //public static explicit operator JobHandle(Tracked<TVal> tracked) => tracked.GetJobHandle();

        //Multiply
        /*public static TrackedWith<Tracked<TVal>, JobHandle> operator *(Tracked<TVal> tA, JobHandle inDeps) => tA.With(inDeps);
        public static TrackedWith<Tracked<TVal>, JobHandle> operator *(JobHandle inDeps, Tracked<TVal> tA) => tA.With(inDeps);#1#

        //Add -> Compound List
        /// <summary>
        /// Generates a compound list of ITracked objects from cached boxed values.
        /// Implicitly casts to the id type to uint to support generic use.
        /// </summary>
        /*public static List<ITracked> operator +(Tracked<TVal> tA, uint tB)
        {
            var list = TrackedCollectionManager.FetchTrackerList();
            list.Add(TrackedCollectionManager.GetTrackedByID(tA.id));
            list.Add(TrackedCollectionManager.GetTrackedByID(tB));
            return list;
        }

        public static List<ITracked> operator +(List<ITracked> list, Tracked<TVal> tB)
        {
            list.Add(TrackedCollectionManager.GetTrackedByID(tB.id));
            return list;
        }

        public static List<ITracked> operator +(Tracked<TVal> tB, List<ITracked> list)
        {
            list.Add(TrackedCollectionManager.GetTrackedByID(tB.id));
            return list;
        }#1#

        /*public JobHandle GetJobHandle()
        {
            TrackedCollectionManager.
            
            var sentinel = VNativeSentinels.GetSentinelFromID(ref this);
            sentinel.CheckJobDependencies(true, out var newDeps);
            return newDeps;
        }

        public JobHandle GetJobHandleWith(JobHandle inDeps) => JobHandle.CombineDependencies(GetJobHandle(), inDeps);

        public JobHandle GetJobHandleWith(Tracked<TVal> inDeps) => JobHandle.CombineDependencies(GetJobHandle(), inDeps.GetJobHandle());

        public void AddDependency(JobHandle job)
        {
            VNativeSentinels.GetSentinelFromID(ref this).AddDependentJob(job);
        }

        public void CompleteClearDependencies()
        {
            var sentinel = VNativeSentinels.GetSentinelFromID(ref this);
            sentinel.CompleteClearAllJobs();
        }#1#
    }

    /*public struct TrackedWith<TTracked, TAdd> : ITracked
        where TTracked : struct, IDisposable, ITracked
        where TAdd : struct
    {
        TTracked tracked;
        TAdd attachedValue;

        public uint ID
        {
            get => tracked.ID; 
            set => tracked.ID = value;
        }

        public TrackedWith(TTracked tracked, TAdd attachedValue)
        {
            this.tracked = tracked;
            this.attachedValue = attachedValue;
        }

        public int CompareTo(uint other)
        {
            throw new NotImplementedException();
        }
        public JobHandle GetJobHandle()
        {
            throw new NotImplementedException();
        }

        public JobHandle GetJobHandleWith(JobHandle inDeps)
        {
            throw new NotImplementedException();
        }

        public void AddDependency(JobHandle job)
        {
            throw new NotImplementedException();
        }
    }#1#
}*/