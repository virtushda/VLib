using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using Plane = Unity.Mathematics.Geometry.Plane;

namespace VLib
{
    public static class TransformExt
    {
        public static float3x3 GetDirsXYZ(this Transform t) => new float3x3(t.right, t.up, t.forward);

        public static bool TryGetChild(this Transform t, int childIndex, out Transform child)
        {
            BurstAssert.True(childIndex >= 0); // Negative child index is invalid.
            if (t.childCount <= childIndex)
            {
                child = default;
                return false;
            }

            child = t.GetChild(childIndex);
            return true;
        }

        public static bool TryGetComponentInParents<T>(this Transform transform, out T component) 
            where T : Component
        {
            component = null;
            
            do
            {
                if (transform.TryGetComponent(out component))
                    return true;
                transform = transform.parent;
            } 
            while (transform != null);
            
            return false;
        }

        public static Transform[] GetChildTransforms(this Transform t)
        {
            if (t.childCount == 0)
                return Array.Empty<Transform>();

            var array = new Transform[t.childCount];

            for (int i = 0; i < array.Length; i++)
                array[i] = t.GetChild(i);

            return array;
        }

        public static List<Transform> GetAncestors(this Transform t, in List<Transform> ancestors)
        {
            var transformWalker = new TransformAncestryIterator(t);
            while (transformWalker.MoveNext())
                ancestors.Add(transformWalker.current);
            return ancestors;
        }

        struct TransformAncestryIterator
        {
            public Transform start { get; }
            public Transform current { get; private set; }
            
            public TransformAncestryIterator(Transform start)
            {
                this.start = start;
                current = start;
            }
            
            public bool MoveNext()
            {
                if (current == null)
                    return false;

                current = current.parent;
                return current != null;
            }
        }

        public static List<Transform> FindChain(this Transform root, string name)
        {
            var nodes = new List<Transform>();
            var s = FindRobust(root, name);
            if (s == null)
                s = root;
            else
                nodes.Add(s);
            int idx = 1;
            for (int i = 0; i < 20; i++)
            {
                var ns = FindRobust(s, string.Format("{0}.{1}", name, idx.ToString().PadLeft(3, '0')));
                if (ns != null)
                {
                    s = ns;
                    nodes.Add(s);
                }

                idx++;
            }

            return nodes;
        }

        public static Transform FindRobust(this Transform n, string name)
        {
            if (n == null)
                return null;
            var ret = n.Find(name);
            if (ret == null)
                ret = n.Find(name.Replace(".", "_"));
            return ret;
        }

        public static Transform FindPartial(this Transform n, string name)
        {
            if (n == null)
                return null;
            for (int i = 0; i < n.childCount; i++)
            {
                if (n.GetChild(i).name.ToLower().Contains(name.ToLower()))
                    return n.GetChild(i);
            }

            var ret = n.Find(name);
            if (ret == null)
                ret = n.Find(name.Replace(".", "_"));
            return ret;
        }

        public static Transform FindInChain(this Transform[] nodes, string name)
        {
            if (nodes == null || nodes.Length <= 0)
                return null;
            foreach (var n in nodes)
            {
                var ret = n.Find(name);
                if (ret == null)
                    ret = n.Find(name.Replace(".", "_"));
                if (ret != null)
                    return ret;
            }

            return null;
        }

        /// <summary> Creates a plane which intersects this transform position, taking the desired transform axis as the plane normal. </summary>
        public static Plane GetPlane(this Transform t, Axis normalAxis, bool flippedNormal = false)
        {
            var pos = t.position;
            var forward = normalAxis switch
            {
                Axis.X => t.right,
                Axis.Y => t.up,
                Axis.Z => t.forward,
                _ => throw new ArgumentOutOfRangeException()
            };
            if (flippedNormal)
                forward = -forward;
            return new Plane(forward, pos);
        }

        /// <summary> A depth and child index to record splits in a tree. </summary>
        struct HierarchicalSplit
        {
            public ushort depth;
            public ushort childIndex;
        }

        /// <summary> <inheritdoc cref="RecordChildrenDepthOrderedRecursive"/> </summary>
        [BurstDiscard]
        public static void AutoSortTransformListDepthFirst(this List<Transform> transforms)
        {
#if ENABLE_PROFILER
            using var profileMarker = ProfileScope.Auto();
#endif
            var rootTransformIDs = new UnsafeList<int>(transforms.Count, Allocator.Temp);
            var idMap = new UnsafeHashMap<int, int>(transforms.Count, Allocator.Temp);
            var orderedIDs = new UnsafeList<int>(transforms.Count, Allocator.Temp);
            
            // Track IDs
            for (int i = 0; i < transforms.Count; i++)
            {
                var t = transforms[i];
                idMap.Add(t.GetInstanceID(), i);
            }
            
            // Find roots (relative to the list, not necessarily scene roots)
            for (int i = 0; i < transforms.Count; i++)
            {
                var t = transforms[i];
                var tParent = t.parent;
                if (!tParent // Scene root
                    || !idMap.ContainsKey(tParent.GetInstanceID())) // Relative root
                {
                    rootTransformIDs.Add(t.GetInstanceID());
                }
            }
            // Remove nested roots
            for (int i = rootTransformIDs.Length - 1; i >= 0; i--)
            {
                var rootID = rootTransformIDs[i];
                var rootIndex = idMap[rootID];
                var rootTransform = transforms[rootIndex];
                
                for (int j = rootTransformIDs.Length - 1; j >= 0; j--)
                {
                    if (i == j)
                        continue;
                    var otherRootID = rootTransformIDs[j];
                    var otherRootIndex = idMap[otherRootID];
                    var otherRootTransform = transforms[otherRootIndex];
                    if (rootTransform.IsChildOf(otherRootTransform))
                    {
                        rootTransformIDs.RemoveAtSwapBack(i);
                        break;
                    }
                }
            }
            
            // Record correct order
            var recursiveSplitStack = new UnsafeList<HierarchicalSplit>(32, Allocator.Temp);
            for (int r = 0; r < rootTransformIDs.Length; r++)
            {
                var rootID = rootTransformIDs[r];
                var root = transforms[idMap[rootID]];

                var currentDepth = 0;
                var iteration = 0u;
                RecordChildrenDepthOrderedRecursive(root, ref currentDepth, ref recursiveSplitStack, ref orderedIDs, ref iteration);
            }
            recursiveSplitStack.Dispose();
            
            // Clean ordered list of any gaps (input list does not have to be complete)
            for (int i = orderedIDs.Length - 1; i >= 0; i--)
            {
                var id = orderedIDs[i];
                if (!idMap.ContainsKey(id))
                    orderedIDs.RemoveAt(i);
            }
            
            // Reorder
            for (int newIndex = 0; newIndex < orderedIDs.Length; newIndex++)
            {
                var correctID = orderedIDs[newIndex];
                var oldIndex = idMap[correctID];
                
                // Swap needed?
                if (oldIndex != newIndex)
                {
                    // Move transforms
                    var other = transforms[newIndex];
                    transforms[newIndex] = transforms[oldIndex];
                    transforms[oldIndex] = other;
                    
                    // Update ID map
                    idMap[correctID] = newIndex;
                    idMap[other.GetInstanceID()] = oldIndex;
                }
            }
            
            // Clean up
            rootTransformIDs.Dispose();
            orderedIDs.Dispose();
            idMap.Dispose();
        }

        /// <summary> Traverses a transform tree, preferring depth. Traverses child nodes, THEN siblings nodes, to record depth-based chains. <br/>
        /// Optimizes access and provides a deterministic order. </summary>
        static void RecordChildrenDepthOrderedRecursive(Transform current, ref int currentDepth, ref UnsafeList<HierarchicalSplit> recursiveSplitStack, ref UnsafeList<int> orderedIDs, 
            ref uint iteration)
        {
            const int iterationLimit = 10000;
            
            ++iteration;
            if (iteration > iterationLimit)
            {
                Debug.LogError("Infinite loop detected in AutoSortTransformListDepthFirst. Aborting.");
                return;
            }
            
            orderedIDs.Add(current.GetInstanceID());
            var childCount = current.childCount;
            
            // Hit end point
            if (childCount == 0)
            {
                // Is there another split to explore?
                if (recursiveSplitStack.TryPop(out var split))
                {
                    // Travel back to the split
                    while (currentDepth > split.depth)
                    {
                        current = current.parent;
                        --currentDepth;
                    }

                    // Travel to the sibling (parent is already explored)
                    current = current.GetChild(split.childIndex);
                    ++currentDepth;
                    RecordChildrenDepthOrderedRecursive(current, ref currentDepth, ref recursiveSplitStack, ref orderedIDs, ref iteration);
                }
            }
            else // Traverse down tree
            {
                // Record passed splits to return to
                // Record in reverse order to ensure popped order is correct
                for (int c = childCount - 1; c > 0; c--)
                    recursiveSplitStack.Add(new HierarchicalSplit {depth = (ushort)currentDepth, childIndex = (ushort)c});
                
                // Move to first child
                var child = current.GetChild(0);
                ++currentDepth;
                RecordChildrenDepthOrderedRecursive(child, ref currentDepth, ref recursiveSplitStack, ref orderedIDs, ref iteration);
            }
            
            // DONE!
        }
    }
}