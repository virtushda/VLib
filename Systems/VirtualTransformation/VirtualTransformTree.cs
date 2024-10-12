using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace VLib
{
    public class VirtualTransformTree
    {
        List<VirtualTransform> transforms;
        Dictionary<int, VirtualTransform> idToTransformMap;
        
        public List<VirtualTransform> Transforms => transforms;
        public Dictionary<int, VirtualTransform> IDToTransformMap => idToTransformMap;

        public VirtualTransformTree()
        {
            transforms = new();
            idToTransformMap = new();
        }

        public VirtualTransformTree(List<Transform> transforms) : this() => AutoConstructTreeFrom(transforms);

        public bool TryAdd(int id, VirtualTransform transform)
        {
            if (!idToTransformMap.TryAdd(id, transform))
                return false;
            transforms.Add(transform);
            return true;
        }
        
        public bool TryGetTransform(Transform t, out VirtualTransform transform) => idToTransformMap.TryGetValue(t.GetInstanceID(), out transform);
        
        public bool TryGetTransform(int id, out VirtualTransform transform) => idToTransformMap.TryGetValue(id, out transform);

        public VirtualTransform GetTransformOrLogError(int transformID)
        {
            if (transformID == 0)
            {
                Debug.LogError("Transform ID is 0!");
                return null;
            }
            if (!idToTransformMap.TryGetValue(transformID, out var transform))
            {
                Debug.LogError($"Transform ID {transformID} is not in the transform ID to virtual transforms map!");
                return null;
            }

            return transform;
        }
        
        public VirtualTransform GetTransformOrLogError(Transform transform)
        {
            if (transform == null)
            {
                Debug.LogError("Transform is null!");
                return null;
            }
            return GetTransformOrLogError(transform.GetInstanceID());
        }

        /// <summary> Will auto expand inputTransforms to include all necessary transforms </summary>
        void AutoConstructTreeFrom(List<Transform> inputTransforms)
        {
            transforms.Clear();
            idToTransformMap.Clear();
            
            AutoExpandTransformListWithAllRequired(ref inputTransforms);

            // Create blank virtual transforms
            foreach (var transform in inputTransforms)
            {
                var instanceID = transform.GetInstanceID();
                if (instanceID == 0)
                    continue;
                var virtualTransform = new VirtualTransform();
                idToTransformMap.Add(instanceID, virtualTransform);
                transforms.Add(virtualTransform);
                
                // Extend input
            }
            
            // Establish relationships
            foreach (var transform in inputTransforms)
            {
                var instanceID = transform.GetInstanceID();
                if (instanceID == 0)
                    continue;

                var virtualTransform = idToTransformMap[instanceID];

                if (transform.parent == null)
                {
                    virtualTransform.SetData(transform);
                }
                else
                {
                    var parentInstanceID = transform.parent.GetInstanceID();
                    var parentVirtualTransform = idToTransformMap[parentInstanceID];
                    
                    virtualTransform.SetData(transform, parentVirtualTransform);
                }
            }
            
            // Clear any non-initialized virtual transforms
            for (int i = transforms.Count - 1; i >= 0; i--)
            {
                if (transforms[i].TransformID == 0)
                    transforms.RemoveAt(i);
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
    }
}