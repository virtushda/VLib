using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

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
            
            public byte accessKey;
            
            public UnsafeParallelHashMap<int, byte> hyperAccessIDsTaken;
            public bool HyperAccessActive => hyperAccessIDsTaken.IsCreated;
        }

        RefStruct<Internal> data;
        public RefStruct<Internal> InternalData => data;

        public bool IsCreated => data.IsCreated;
        public static implicit operator bool(VirtualValueTransformTree tree) => tree.IsCreated;

        public ref Internal InternalRef => ref data.ValueRef;
        
        public UnsafeList<VirtualValueTransform>.ReadOnly TransformsReadOnly => InternalRef.transforms.AsReadOnly();

        public ulong OwnerIndex => InternalRef.ownerIndex;

        unsafe byte* AccessKey => (byte*) UnsafeUtility.AddressOf(ref data.ValueRef.accessKey);

        public VirtualValueTransformTree(ulong ownerID, List<Transform> transforms) : this()
        {
            var dataStruct = new Internal
            {
                ownerIndex = ownerID,
                transforms = new UnsafeList<VirtualValueTransform>(transforms.Count, Allocator.Persistent),
                idToIndexMap = new UnsafeParallelHashMap<int, int>(transforms.Count, Allocator.Persistent),
                // Create key with access granted
                accessKey = 1,
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
            VirtualValueTransformTreeEvents.InvokePreDispose(this);
            
            ref var dataRef = ref InternalRef;
            dataRef.accessKey = 0;
            
            DisableHyperAccess();
            
            if (disposeIndividualTransforms)
                foreach (var transform in dataRef.transforms)
                    transform.Dispose();
            
            dataRef.transforms.Dispose();
            dataRef.idToIndexMap.Dispose();
            data.Dispose();
            
            VirtualValueTransformTreeEvents.DisposeHandler(this);
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
        
        public bool HyperAccessActive => InternalRef.HyperAccessActive;
        
        /// <summary> Incredibly dangerous if used wrong! Must call <see cref="DisableHyperAccess"/> after before trying to dispose the tree! </summary>
        public void EnableHyperAccess()
        {
            ref var dataRef = ref InternalRef;
            if (dataRef.HyperAccessActive)
                return;
            BurstAssert.False(dataRef.hyperAccessIDsTaken.IsCreated); // Must not be created yet
            dataRef.hyperAccessIDsTaken = new UnsafeParallelHashMap<int, byte>(dataRef.transforms.Length, Allocator.Persistent);
        }

        public void DisableHyperAccess()
        {
            ref var dataRef = ref InternalRef;
            if (!dataRef.HyperAccessActive)
                return;
            
            // Log any IDs that were not returned
            foreach (var id in dataRef.hyperAccessIDsTaken)
                Debug.LogError($"Hyper access ID {id.Key} was not returned, held {id.Value} times!");
            
            dataRef.hyperAccessIDsTaken.Dispose();
        }

        /// <summary> Must enable access guard with <see cref="EnableHyperAccess"/> to use. Must also call <see cref="ReturnGuardedRef"/> when done! </summary>
        public bool TryGetGuardedRef(Transform t, out VirtualValueTransform virtualTransform)
        {
            if (t == null)
            {
                virtualTransform = default;
                return false;
            }
            return TryGetGuardedRef(t.GetInstanceID(), out virtualTransform);
        }

        /// <summary> <inheritdoc cref="TryGetGuardedRef(UnityEngine.Transform,out VLib.VirtualValueTransform)"/> </summary>
        public bool TryGetGuardedRef(int id, out VirtualValueTransform virtualTransform)
        {
            ref var dataRef = ref InternalRef;
            if (!dataRef.HyperAccessActive)
            {
                Debug.LogError("Hyper access is not active!");
                virtualTransform = default;
                return false;
            }
            
            if (!dataRef.idToIndexMap.TryGetValue(id, out var transformIndex))
            {
                virtualTransform = default;
                return false;
            }
            
            virtualTransform = dataRef.transforms[transformIndex];
            
            // Increment counter
            if (dataRef.hyperAccessIDsTaken.TryGetValue(id, out var counter))
            {
                var newCounterValue = (byte)(counter + 1);
                if (newCounterValue >= 255)
                {
                    Debug.LogError($"Hyper access ID {id} has been taken {counter} times, this is the maximum allowed!");
                    return false;
                }
                dataRef.hyperAccessIDsTaken[id] = newCounterValue;
            }
            else
                dataRef.hyperAccessIDsTaken.Add(id, 1);
            return true;
        }

        /// <summary> <inheritdoc cref="ReturnGuardedRef(int)"/> </summary>
        public void ReturnGuardedRef(VirtualValueTransform virtualTransform) => ReturnGuardedRef(virtualTransform.TransformID);

        /// <summary> Not thread-safe. Required! </summary>
        public void ReturnGuardedRef(int id)
        {
            ref var dataRef = ref InternalRef;
            if (!dataRef.HyperAccessActive)
            {
                Debug.LogError("Hyper access is not active!");
                return;
            }
            if (!dataRef.hyperAccessIDsTaken.TryGetValue(id, out var count))
                return;
            // Decrement and return to map
            count--;
            if (count > 0)
                dataRef.hyperAccessIDsTaken[id] = count;
            else
                dataRef.hyperAccessIDsTaken.Remove(id);
        }
        
        #endregion

        #region Tree Construction

        /// <summary> Will auto expand inputTransforms to include all necessary transforms </summary>
        [BurstDiscard]
        void AutoConstructTreeFrom(List<Transform> inputTransforms)
        {
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
                unsafe
                {
                    dataRef.transforms.Add(new VirtualValueTransform(AccessKey, transform));
                }
            }

            // Establish relationships
            foreach (var transform in inputTransforms)
            {
                var instanceID = transform.GetInstanceID();
                if (instanceID == 0)
                    continue;

                var virtualTransformIndex = dataRef.idToIndexMap[instanceID];
                ref var virtualTransformRef = ref dataRef.transforms.ElementAt(virtualTransformIndex);
                
                if (transform.parent != null)
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