using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace VLib.Structures
{
    /// <summary> Description of a child's position in a hierarchical structure. </summary>
    [Serializable]
    public struct HierarchicalChildPosition
    {
        public enum TransitDir : byte { Sibling, Child }

        /// <summary> Represents a series of steps to reach a child relative to a starting point. <br/>
        /// False: Next Sibling, True: First Child </summary>
        [SerializeField] FixedBitList120 transitBits;

        /// <summary> An optional metric to help verify the final destination. </summary>
        [SerializeField] uint finalDestinationHash;

        /// <summary> <inheritdoc cref="finalDestinationHash"/> </summary>
        public uint FinalDestinationHash
        {
            get => finalDestinationHash;
            set => finalDestinationHash = value;
        }

        /// <summary> Inserts a transit step at the beginning of the list. </summary>
        public void Record(bool wasChild) => transitBits.InsertBitAtStart(wasChild);

        /// <summary> Steps in-order from first-move to last-move to reach the destination. </summary>
        public bool TryGetTransit(int transitIndex, out TransitDir transitDir)
        {
            if ((uint)transitIndex >= transitBits.Count)
            {
                transitDir = default;
                return false;
            }
            transitDir = transitBits[transitIndex] ? TransitDir.Child : TransitDir.Sibling;
            return true;
        }

        [Button]
        void Log() => Debug.Log(ToString());
        
        public override string ToString()
        {
            string str = "Parent";
            for (int i = 0; i < transitBits.Count; i++)
                str += transitBits[i] ? "->Child" : "->Sib";
            str += "->Dest";
            return str;
        }
    }

    public static class HierarchicalChildPositionExt
    {
        /// <summary> Attempts to compute the hierarchical position of a child relative to a starting transform. </summary>
        /// <param name="startingTransform"> The starting point of the hierarchy. </param>
        /// <param name="childTransform"> The child transform to compute the position of. </param>
        /// <param name="position"> The computed serializable hierarchical position of the child relative to the starting transform. </param>
        /// <param name="logging"> Whether to log errors if the computation fails. </param>
        public static bool TryComputeChildHierarchicalPosition(this Transform startingTransform, Transform childTransform, out HierarchicalChildPosition position, bool logging = true)
        {
            position = new HierarchicalChildPosition();
            Transform currentTransform = childTransform;
            // Iterate up parents until we reach the starting transform, or we hit the root
            while (currentTransform.parent != null)
            {
                var parent = currentTransform.parent;
                var siblingIndex = currentTransform.GetSiblingIndex();
                
                // Sibling steps
                while (siblingIndex > 0)
                {
                    position.Record(false);
                    --siblingIndex;
                }
                
                // Child steps
                currentTransform = parent;
                position.Record(true);
                
                // Reached the starting transform
                if (currentTransform == startingTransform)
                {
                    // Store the hashcode of the name, which is very unlikely to randomly collider
                    position.FinalDestinationHash = (uint)childTransform.name.GetHashCode();
                    return true;
                }
            }

            if (logging)
                Debug.LogError($"Failed to compute hierarchical position from {startingTransform} to {childTransform}.");
            return false;
        }
        
        /// <summary> Attempts to get the child transform at a hierarchical position relative to a starting transform. </summary>
        /// <param name="startingTransform"> The starting point of the hierarchy. </param>
        /// <param name="position"> The hierarchical position of the child relative to the starting transform. </param>
        /// <param name="child"> The child transform found at the hierarchical position. </param>
        /// <returns> Whether the child was found. </returns>
        public static bool TryGetChildHierarchical(this Transform startingTransform, HierarchicalChildPosition position, out Transform child, bool logging = true)
        {
            int transitIndex = 0;
            Transform currentTransform = startingTransform;
            child = null;
            
            // TODO: Optimize multiple sibling steps if needed
            
            while (position.TryGetTransit(transitIndex, out var transitDir))
            {
                if (transitDir == HierarchicalChildPosition.TransitDir.Sibling)
                {
                    var parent = currentTransform.parent;
                    if (parent == null)
                    {
                        if (logging)
                            Debug.LogError("Could not take sibling step, no parent...");
                        child = null;
                        return false;
                    }
                    var nextSiblingIndex = currentTransform.GetSiblingIndex() + 1;
                    if (parent.TryGetChild(nextSiblingIndex, out child))
                    {
                        currentTransform = child;
                    }
                    else
                    {
                        if (logging)
                            Debug.LogError("Failed to get sibling.");
                        return false;
                    }
                }
                else
                {
                    if (currentTransform.childCount == 0)
                    {
                        if (logging)
                            Debug.LogError("Attempt to step down to child, but no children...");
                        child = null;
                        return false;
                    }
                    // Move down to first child
                    currentTransform = currentTransform.GetChild(0);
                }

                ++transitIndex;
            }

            if (child == null)
            {
                if (logging)
                    Debug.LogError("Failed to find child.");
                return false;
            }
            // Verify hash if present
            if (position.FinalDestinationHash != 0 && child.name.GetHashCode() != position.FinalDestinationHash)
            {
                if (logging)
                    Debug.LogError("Final destination hash mismatch.");
                return false;
            }
            return true;
        }
    }
}