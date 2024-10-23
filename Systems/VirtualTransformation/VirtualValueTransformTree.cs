using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace VLib
{
    /// <summary> A tree structure for holding burst-compatible virtual transforms.
    /// The tree is not designed to be used directly in burst, as that isn't currently needed. </summary>
    public unsafe class VirtualValueTransformTree
    {
        public struct Data
        {
            public ulong ownerIndex;
            public UnsafeList<VirtualValueTransform> transforms;
            public UnsafeParallelHashMap<int, int> idToIndexMap;
            
            public byte accessKey;
            
            public bool accessGuardActive;
            public UnsafeParallelHashMap<int, byte> hyperAccessIDsTaken;
        }

        RefStruct<Data> data;

        /// <summary> Leverage this event to return guarded access or to do disposal work before the tree comes down. </summary>
        public event Action<VirtualValueTransformTree> OnPreDispose;

        public bool IsCreated => data.IsCreated;

        public ref Data DataRef => ref data.ValueRef;
        
        public UnsafeList<VirtualValueTransform>.ReadOnly TransformsReadOnly => DataRef.transforms.AsReadOnly();

        public ulong OwnerIndex => DataRef.ownerIndex;

        byte* accessKey => &data.ValuePtr->accessKey;

        public VirtualValueTransformTree(ulong ownerID, List<Transform> transforms)
        {
            var dataStruct = new Data
            {
                ownerIndex = ownerID,
                transforms = new UnsafeList<VirtualValueTransform>(transforms.Count, Allocator.Persistent),
                idToIndexMap = new UnsafeParallelHashMap<int, int>(transforms.Count, Allocator.Persistent),
                // Create key with access granted
                accessKey = 1,
            };
            data = RefStruct<Data>.Create(dataStruct);
            
            AutoConstructTreeFrom(transforms);
        }

        public void Dispose(bool disposeIndividualTransforms = true)
        {
            if (!data.IsCreated)
                return;
            
            // Other systems are expected to use this event to return guarded refs they've taken out
            OnPreDispose?.Invoke(this);
            
            ref var dataRef = ref DataRef;
            dataRef.accessKey = 0;
            
            if (dataRef.accessGuardActive)
                DisableAccessGuard();
            
            if (disposeIndividualTransforms)
                foreach (var transform in dataRef.transforms)
                    transform.Dispose();
            
            dataRef.transforms.Dispose();
            dataRef.idToIndexMap.Dispose();
            data.Dispose();
        }

        public bool TryAdd(int id, VirtualValueTransform transform)
        {
            ref var dataRef = ref DataRef;
            if (dataRef.accessGuardActive)
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
        }

        /// <summary> This system bypasses the trees safety system. Check out <see cref="TryGetGuardedRef(UnityEngine.Transform,out VLib.VirtualValueTransform)"/> </summary>
        [BurstDiscard]
        public bool TryGetTransformUNSAFE(Transform t, out VirtualValueTransform transform) => TryGetTransformUNSAFE(t.GetInstanceID(), out transform);

        public bool TryGetTransformUNSAFE(int id, out VirtualValueTransform transform)
        {
            ref var dataRef = ref DataRef;
            if (!dataRef.idToIndexMap.TryGetValue(id, out var transformIndex))
            {
                transform = default;
                return false;
            }
            transform = dataRef.transforms[transformIndex];
            return true;
        }
        
        #region Access Guard - Secure SAFE access to low-level virtual transforms
        
        /// <summary> Incredibly dangerous if used wrong! Must call <see cref="DisableAccessGuard"/> after before trying to dispose the tree! </summary>
        public void EnableAccessGuard()
        {
            ref var dataRef = ref DataRef;
            if (dataRef.accessGuardActive && dataRef.hyperAccessIDsTaken.IsCreated)
                return;
                
            dataRef.hyperAccessIDsTaken = new UnsafeParallelHashMap<int, byte>(dataRef.transforms.Length, Allocator.Persistent);
            dataRef.accessGuardActive = true;
        }

        public void DisableAccessGuard()
        {
            ref var dataRef = ref DataRef;
            dataRef.accessGuardActive = false;
            
            // Log any IDs that were not returned
            foreach (var id in dataRef.hyperAccessIDsTaken)
                Debug.LogError($"Hyper access ID {id.Key} was not returned, held {id.Value} times!");
            
            dataRef.hyperAccessIDsTaken.Dispose();
        }

        /// <summary> Must enable access guard with <see cref="EnableAccessGuard"/> to use. Must also call <see cref="ReturnGuardedRef"/> when done! </summary>
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
            ref var dataRef = ref DataRef;
            if (!dataRef.accessGuardActive)
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
            ref var dataRef = ref DataRef;
            if (!dataRef.accessGuardActive)
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
        void AutoConstructTreeFrom(List<Transform> inputTransforms)
        {
            if (!data.IsCreated)
            {
                Debug.LogError("VirtualValueTransformTree not initialized!");
                return;
            }
            
            ref var dataRef = ref DataRef;
            
            dataRef.transforms.Clear();
            dataRef.idToIndexMap.Clear();
            
            AutoExpandTransformListWithAllRequired(ref inputTransforms);

            // Create virtual transforms
            for (var i = 0; i < inputTransforms.Count; i++)
            {
                var transform = inputTransforms[i];
                var instanceID = transform.GetInstanceID();
                if (instanceID == 0)
                    continue;

                dataRef.idToIndexMap.Add(instanceID, i);
                // Create virtual transforms without parents or children, these will be established later as a given parent could not be created before its children or vice-versa
                dataRef.transforms.Add(new VirtualValueTransform(accessKey, transform));
            }

            // Establish relationships
            foreach (var transform in inputTransforms)
            {
                var instanceID = transform.GetInstanceID();
                if (instanceID == 0)
                    continue;

                var virtualTransformIndex = dataRef.idToIndexMap[instanceID];
                var virtualTransformPtr = dataRef.transforms.GetListElementPtr(virtualTransformIndex);
                
                if (transform.parent != null)
                {
                    var parentInstanceID = transform.parent.GetInstanceID();
                    var parentVirtualTransformIndex = dataRef.idToIndexMap[parentInstanceID];
                    var parentVirtualTransform = dataRef.transforms[parentVirtualTransformIndex];
                    
                    virtualTransformPtr->parent = parentVirtualTransform;
                }
            }
            
            // Clear any non-initialized virtual transforms
            for (int i = dataRef.transforms.Length - 1; i >= 0; i--)
            {
                if (dataRef.transforms[i].TransformID == 0)
                    dataRef.transforms.RemoveAt(i);
            }
        }

        public static void AutoExpandTransformListWithAllRequired(ref List<Transform> transforms)
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
    }
}