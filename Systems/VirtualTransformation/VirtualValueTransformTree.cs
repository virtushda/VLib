using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Assertions;

namespace VLib
{
    /// <summary> A tree structure for holding burst-compatible virtual transforms. <br/>
    /// The tree itself is burst-compatible for convenience since it contains virtual transform references, but only a few of its methods are designed to be used directly in burst. <br/>
    /// This struct is COPY-SAFE! (All data is held in an allocated collection) </summary>
    public struct VirtualValueTransformTree
    {
        public struct Internal
        {
            public ulong ownerIndex;
            public UnsafeList<VirtualValueTransform> transforms;
            public UnsafeParallelHashMap<int, int> idToIndexMap;

            /// <summary> Key: TransformID, Value: Accessor unique ID set </summary>
            public UnsafeParallelHashMap<int, UnsafeHashSet<long>> hyperAccessIDsTaken;
            public bool HyperAccessActive => hyperAccessIDsTaken.IsCreated;
        }

        RefStruct<Internal> data;
        public RefStruct<Internal> InternalData => data;

        public bool IsCreated => data.IsCreated;
        public static implicit operator bool(VirtualValueTransformTree tree) => tree.IsCreated;

        public ref Internal InternalRef => ref data.ValueRef;
        
        public UnsafeList<VirtualValueTransform>.ReadOnly TransformsReadOnly => InternalRef.transforms.AsReadOnly();

        public ulong OwnerIndex => InternalRef.ownerIndex;

        public VirtualValueTransformTree(ulong ownerID, List<Transform> transforms) : this()
        {
            var dataStruct = new Internal
            {
                ownerIndex = ownerID,
                transforms = new UnsafeList<VirtualValueTransform>(transforms.Count, Allocator.Persistent, NativeArrayOptions.ClearMemory),
                idToIndexMap = new UnsafeParallelHashMap<int, int>(transforms.Count, Allocator.Persistent),
            };
            data = RefStruct<Internal>.Create(dataStruct);
            
            AutoConstructTreeFrom(transforms);
        }

        /// <summary> Disposal method. Must be called from managed-land. </summary>
        [BurstDiscard]
        public void Dispose_Managed(bool disposeIndividualTransforms = true)
        {
            if (!data.IsCreated)
                return;
            
            // Other systems are expected to use this event to return guarded refs they've taken out
            VirtualValueTransformTreeEvents.InvokePreDispose(this, true);
            
            ref var dataRef = ref InternalRef;
            
            DisableHyperAccess();
            
            if (disposeIndividualTransforms)
                foreach (var transform in dataRef.transforms)
                    transform.Dispose();
            
            dataRef.transforms.Dispose();
            dataRef.idToIndexMap.Dispose();
            BurstAssert.True(data.IsCreated, 99779977);
            data.Dispose();
        }

        // NOTE: If this is reenabled, it will need to support deterministic sorting! (see: AutoConstructTreeFrom)
        /*public bool TryAdd(int id, VirtualValueTransform transform)
        {
            ref var dataRef = ref InternalRef;
            if (dataRef.HyperAccessActive)
            {
                Debug.LogError("Cannot add to transform tree while hyper access is active!");
                return false;
            }
            
            if (dataRef.idToIndexMap.ContainsKey(id))
                return false;
            
            int index = dataRef.transforms.Length;
            dataRef.transforms.Add(transform);
            dataRef.idToIndexMap.Add(id, index);
            
            return true;
        }*/

        /// <summary> This system bypasses the trees safety system. Check out <see cref="TryGetGuardedRef(UnityEngine.Transform,out VLib.VirtualValueTransform)"/> </summary>
        [BurstDiscard]
        public bool TryGetTransformUNSAFE(Transform t, out VirtualValueTransform transform) => TryGetTransformUNSAFE(t.GetInstanceID(), out transform);

        /// <summary> <inheritdoc cref="TryGetTransformUNSAFE(UnityEngine.Transform,out VLib.VirtualValueTransform)"/> </summary>
        public bool TryGetTransformUNSAFE(int id, out VirtualValueTransform transform)
        {
            ref var dataRef = ref InternalRef;
            if (!dataRef.idToIndexMap.TryGetValue(id, out var transformIndex))
            {
                transform = default;
                return false;
            }
            transform = dataRef.transforms[transformIndex];
            return true;
        }
        
        #region Hyper Access - Secure SAFE access to low-level virtual transforms

        public struct TransformAccessor
        {
            static long accessIDSource = 0;
            static long GetNextAccessID() => Interlocked.Increment(ref accessIDSource);
            
            readonly VirtualValueTransformTree tree;
            public readonly VirtualValueTransform transform;
            public readonly long accessID;
            
            public TransformAccessor(VirtualValueTransformTree tree, VirtualValueTransform transform)
            {
                this.tree = tree;
                this.transform = transform;
                accessID = GetNextAccessID();
            }

            public void Dispose()
            {
                if (accessID <= 0)
                    return;
                if (tree.IsCreated)
                    tree.ReturnGuardedRef(this);
                else
                    Debug.LogError("Tree is not created, cannot return guarded ref!");
            }
        }

        public bool HyperAccessActive => InternalRef.HyperAccessActive;
        
        /// <summary> Incredibly dangerous if used wrong! Must call <see cref="DisableHyperAccess"/> after before trying to dispose the tree! </summary>
        public void EnableHyperAccess()
        {
            ref var dataRef = ref InternalRef;
            if (dataRef.HyperAccessActive)
                return;
            BurstAssert.False(dataRef.hyperAccessIDsTaken.IsCreated); // Must not be created yet
            dataRef.hyperAccessIDsTaken = new UnsafeParallelHashMap<int, UnsafeHashSet<long>>(dataRef.transforms.Length, Allocator.Persistent);
        }

        public void DisableHyperAccess()
        {
            ref var dataRef = ref InternalRef;
            if (!dataRef.HyperAccessActive)
                return;
            
            // Log any IDs that were not returned
            foreach (var id in dataRef.hyperAccessIDsTaken)
            {
                Assert.IsTrue(id.Value is {IsCreated: true, Count: > 0}, "Hyper access IDs list must be populated to be in the map at all.");
                string error = $"Hyper access ID {id.Key} was not returned, held by access IDs:\n";
                foreach (var accessID in id.Value)
                    error += $"  {accessID}\n";
                Debug.LogError(error);
                
                id.Value.Dispose();
            }

            dataRef.hyperAccessIDsTaken.Dispose();
        }

        /// <summary> Must enable access guard with <see cref="EnableHyperAccess"/> to use. Must dispose! </summary>
        public bool TryGetGuardedRef(Transform t, out TransformAccessor virtualTransformAccessor)
        {
            if (t == null)
            {
                virtualTransformAccessor = default;
                return false;
            }
            return TryGetGuardedRef(t.GetInstanceID(), out virtualTransformAccessor);
        }

        /// <summary> <inheritdoc cref="TryGetGuardedRef(UnityEngine.Transform,out VLib.VirtualValueTransform)"/> </summary>
        bool TryGetGuardedRef(int id, out TransformAccessor virtualTransformAccessor)
        {
            ref var dataRef = ref InternalRef;
            if (!dataRef.HyperAccessActive)
            {
                Debug.LogError("Hyper access is not active!");
                virtualTransformAccessor = default;
                return false;
            }
            
            if (!dataRef.idToIndexMap.TryGetValue(id, out var transformIndex))
            {
                virtualTransformAccessor = default;
                return false;
            }
            
            var virtualTransform = dataRef.transforms[transformIndex];
            virtualTransformAccessor = new TransformAccessor(this, virtualTransform);
            
            // Track takers
            if (dataRef.hyperAccessIDsTaken.TryGetValue(id, out var takers))
            {
                takers.Add(virtualTransformAccessor.accessID);
                Assert.IsFalse(takers.Count > 512, "Ridiculous number of hyper accessors!");
                dataRef.hyperAccessIDsTaken[id] = takers;
            }
            else
            {
                var newList = new UnsafeHashSet<long>(1, Allocator.Persistent);
                newList.Add(virtualTransformAccessor.accessID);
                dataRef.hyperAccessIDsTaken.Add(id, newList);
            }

            return true;
        }

        /// <summary> Not thread-safe. Required! </summary>
        readonly void ReturnGuardedRef(TransformAccessor accessor)
        {
            ref var dataRef = ref InternalRef;
            if (!dataRef.HyperAccessActive)
            {
                Debug.LogError("Hyper access is not active!");
                return;
            }
            if (!dataRef.hyperAccessIDsTaken.TryGetValue(accessor.transform.TransformID, out var takers))
                return;
            // Decrement and return to map
            takers.Remove(accessor.accessID);
            if (takers.IsEmpty)
                dataRef.hyperAccessIDsTaken.Remove(accessor.transform.TransformID);
            else
                dataRef.hyperAccessIDsTaken[accessor.transform.TransformID] = takers;
        }
        
        #endregion

        #region Tree Construction

        /// <summary> Will auto expand inputTransforms to include all necessary transforms </summary>
        [BurstDiscard]
        void AutoConstructTreeFrom(List<Transform> inputTransforms)
        {
#if ENABLE_PROFILER
            using var profileScope = ProfileScope.Auto();
#endif
            
            if (!data.IsCreated)
            {
                Debug.LogError("VirtualValueTransformTree not initialized!");
                return;
            }
            
            ref var dataRef = ref InternalRef;
            
            dataRef.transforms.Clear();
            dataRef.idToIndexMap.Clear();
            
            ProcessTransformListDeterministic(ref inputTransforms);

            // Create virtual transforms
            for (var i = 0; i < inputTransforms.Count; i++)
            {
                var transform = inputTransforms[i];
                var instanceID = transform.GetInstanceID();
                if (instanceID == 0)
                    continue;

                dataRef.idToIndexMap.Add(instanceID, i);
                // Create virtual transforms without parents or children, these will be established later as a given parent could not be created before its children or vice-versa
                // Child transforms will use the same safety handle as the tree itself
                dataRef.transforms.Add(new VirtualValueTransform(transform));
            }

            // Establish relationships
            foreach (var transform in inputTransforms)
            {
                var instanceID = transform.GetInstanceID();
                if (instanceID == 0)
                    continue;

                var virtualTransformIndex = dataRef.idToIndexMap[instanceID];
                ref var virtualTransformRef = ref dataRef.transforms.ElementAt(virtualTransformIndex);
                
                if (transform.parent)
                {
                    var parentInstanceID = transform.parent.GetInstanceID();
                    var parentVirtualTransformIndex = dataRef.idToIndexMap[parentInstanceID];
                    var parentVirtualTransform = dataRef.transforms[parentVirtualTransformIndex];
                    
                    virtualTransformRef.parent = parentVirtualTransform;
                }
            }
            
            // Clear any non-initialized virtual transforms
            for (int i = dataRef.transforms.Length - 1; i >= 0; i--)
            {
                if (dataRef.transforms[i].TransformID == 0)
                    dataRef.transforms.RemoveAt(i);
            }
        }

        public static void ProcessTransformListDeterministic(ref List<Transform> transforms)
        {
            AutoExpandTransformListWithAllRequired(ref transforms);
            transforms.AutoSortTransformListDepthFirst();
        }

        [BurstDiscard]
        static void AutoExpandTransformListWithAllRequired(ref List<Transform> transforms)
        {
            // Remove nulls
            for (int i = transforms.Count - 1; i >= 0; i--)
            {
                if (transforms[i] == null)
                    transforms.RemoveAt(i);
            }
            
            // Dump all into hashset for defensive adds
            using var hashset = new UnsafeHashSet<int>(128, Allocator.TempJob);
            foreach (var transform in transforms)
            {
                if (transform == null)
                    continue;
                var id = transform.GetInstanceID();
                if (id == 0)
                    continue;
                hashset.Add(transform.GetInstanceID());
            }

            // Copy to array for iterative adds
            var transformsIn = transforms.ToArray();
            
            // Travel up ancestry and add all required parents to build a tree
            foreach (var transform in transformsIn)
            {
                if (transform == null)
                    continue;
                var parent = transform.parent;
                while (parent != null)
                {
                    var parentInstanceID = parent.GetInstanceID();
                    if (!hashset.Add(parentInstanceID))
                        break;
                    transforms.Add(parent);
                    parent = parent.parent;
                }
            }
        }

        #endregion

        #region Event Support
        [BurstDiscard]
        public void Subscribe_OnPreDispose(Action<VirtualValueTransformTree> preDisposeAction)
        {
            VirtualValueTransformTreeEvents.GetHandler(this).Subscribe_OnPreDispose(preDisposeAction);
        }
        
        [BurstDiscard]
        public void Unsubscribe_OnPreDispose(Action<VirtualValueTransformTree> preDisposeAction)
        {
            VirtualValueTransformTreeEvents.GetHandler(this).Unsubscribe_OnPreDispose(preDisposeAction);
        }
        #endregion
    }
}