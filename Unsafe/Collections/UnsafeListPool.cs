using System;
using System.Threading;
using System.Web.UI.WebControls;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace VLib
{
    /*[Obsolete("Use UnsafeBufferClaim system instead.")]
    [GenerateTestsForBurstCompatibility]
    public unsafe struct UnsafeListPool<T> : IDisposable
        where T : unmanaged
    {
        public UnsafeList<IntPtr>* listPool;
        public UnsafeParallelHashSet<IntPtr> fetchedLists;
        
        VUnsafeRef<int> lockValue;

        public int Capacity => listPool->m_capacity;
        public int Count => listPool->m_length;
        public int FetchedCount => fetchedLists.Count();
        /// <summary> The number of fetched and unfetched lists tracked by this collection. </summary>
        public int TotalCount => Count + FetchedCount;
        
        public UnsafeListPool(int initialCapacity = 64)
        {
            listPool = UnsafeList<IntPtr>.Create(initialCapacity, Allocator.Persistent);
            fetchedLists = new UnsafeParallelHashSet<IntPtr>(initialCapacity, Allocator.Persistent);

            lockValue = new VUnsafeRef<int>(0, Allocator.Persistent);
        }

        public void Dispose()
        {
            if (lockValue.Value != 0)
                throw new Exception("Cannot dispose UnsafeListPool while it is locked for multi-threaded operations...");
            
            ForceFetchedBackIntoPool();
            for (int i = 0; i < listPool->m_length; i++)
            {
                var list = (UnsafeList<T>*) listPool->Ptr[i];
                if (list != null)
                    UnsafeList<T>.Destroy(list); //list->Dispose(); WRONG WAY TO DISPOSE!
            }
            listPool->Dispose();
            fetchedLists.Dispose();
            lockValue.Dispose();
        }

        public UnsafeList<T>* this[int index] => (UnsafeList<T>*) listPool->Ptr[index];

        /// <summary>Try to fetch a list from the pool. Not thread safe, use parallel methods.</summary>
        /// <param name="listPtr">Ptr to de-pooled list.</param>
        /// <param name="allowNewAllocations">If pool is empty, expand on-demand?</param>
        /// <returns>True if list fetched successfully.</returns>
        public bool TryFetch(out UnsafeList<T>* listPtr, bool allowNewAllocations = false)
        {
            if (listPool->m_length > 0)
            {
                var lastIndex = listPool->m_length - 1;
                var listIntPtr = listPool->Ptr[lastIndex];
                listPool->RemoveAt(lastIndex);
                fetchedLists.Add(listIntPtr);
                listPtr = (UnsafeList<T>*)listIntPtr;
                return true;
            }

            if (allowNewAllocations)
            {
                listPtr = UnsafeList<T>.Create(8, Allocator.Persistent);
                fetchedLists.Add((IntPtr) listPtr);
                return true;
            }

            listPtr = null;
            return false;
        }

        public bool TryFetchParallel(out UnsafeList<T>* listPtr)
        {
            Lock();
            bool fetch = TryFetch(out listPtr);
            Unlock();
            return fetch;
        }

        /// <summary>Add list ptr back to pool</summary>
        /// <param name="strict">List Ptr will only be pooled if initially pulled from this pool.</param>
        public void Return(UnsafeList<T>* listPtr, bool strict = false)
        {
            var iPtr = (IntPtr) listPtr;
            bool existedInPool = fetchedLists.Remove(iPtr);
            if (!strict || existedInPool)
                listPool->Add(iPtr);
        }

        public void ReturnParallel(UnsafeList<T>* listPtr, bool strict = false)
        {
            Lock();
            Return(listPtr, strict);
            Unlock();
        }

        /// <summary> Populate to internal capacity. </summary>
        /// <param name="desiredCount">Desired valid elements in the pool. Negative value == Fill to internal list current capacity.</param>
        /// <param name="allowShrinking">If desired count under current count, shrink?</param>
        public void PopulatePool_MainThread(int desiredCount = -1, bool allowShrinking = true)
        {
            if (desiredCount < 0) 
                desiredCount = listPool->m_capacity;

            int poolDeltaToTarget = desiredCount - listPool->m_length;
            
            //Ensure Capacity
            listPool->Capacity = math.max(listPool->Capacity, desiredCount);

            //Grow
            if (poolDeltaToTarget > 0)
            {
                for (int i = 0; i < poolDeltaToTarget; i++)
                {
                    listPool->Add((IntPtr) UnsafeList<T>.Create(8, Allocator.Persistent));
                }
            }
            //Shrink
            else if (allowShrinking && poolDeltaToTarget < 0)
            {
                int poolDeltaAbs = math.abs(poolDeltaToTarget);
                for (int i = 0; i < poolDeltaAbs; i++)
                {
                    var lastIndex = listPool->m_length - 1;
                    var lastElement = listPool->Ptr[lastIndex];
                    ((UnsafeList<T>*)lastElement)->Dispose();
                    listPool->RemoveAt(lastIndex);
                }
            }
        }

        /// <summary> Will consider ALL lists reclaimed to the pool. </summary>
        public void ForceFetchedBackIntoPool()
        {
            var lists = fetchedLists.ToNativeArray(Allocator.Temp);
            listPool->Capacity = math.max(listPool->Capacity, listPool->Length + lists.Length);
            
            foreach (var intPtr in lists)
            {
                listPool->AddNoResize(intPtr);
            }

            lists.Dispose();
        }
        
        #region Multithreading
        
        public void Lock()
        {
            //Wait until lock value == 0, then claim it
            while (Interlocked.CompareExchange(ref *lockValue.TPtr, 1, 0) != 0) { }
        }

        public void Unlock()
        {
            Interlocked.Exchange(ref *lockValue.TPtr, 0);
        }
        
        #endregion
    }*/
}