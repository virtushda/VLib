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

        [Flags]
        public enum Flags : byte
        {
            None = 0,
            /// <summary> If set, we can ignore the start hash and instead enforce starting points are always an absolute root. <br/>
            /// This allows for transform name changes. </summary>
            AbsoluteRoot = 1 << 0
        }

        /// <summary> Represents a series of steps to reach a child relative to a starting point. <br/>
        /// False: Next Sibling, True: First Child </summary>
        [SerializeField] FixedBitList120 transitBits;

        /// <summary> An optional hash to help verify the starting point. </summary>
        [SerializeField] int startHash;
        /// <summary> An optional hash to help verify the final destination. </summary>
        [SerializeField] int finalDestinationHash;
        [SerializeField] Flags flags;

        /// <summary> <inheritdoc cref="startHash"/> </summary>
        public int StartHash
        {
            get => startHash;
            set => startHash = value;
        }

        /// <summary> <inheritdoc cref="finalDestinationHash"/> </summary>
        public int FinalDestinationHash
        {
            get => finalDestinationHash;
            set => finalDestinationHash = value;
        }
        public int TransitCount => transitBits.Count;

        public bool UseAbsoluteRoot
        {
            get => flags.HasFlagFast(Flags.AbsoluteRoot);
            set
            {
                if (value)
                    flags |= Flags.AbsoluteRoot;
                else
                    flags &= ~Flags.AbsoluteRoot;
            }
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
        public static bool HasFlagFast(this HierarchicalChildPosition.Flags flags, HierarchicalChildPosition.Flags flag) => (flags & flag) != 0;

        /// <summary> Attempts to compute the hierarchical position of a child relative to a starting transform. </summary>
        /// <param name="startingParentTransform"> The starting point of the hierarchy. </param>
        /// <param name="childTransform"> The child transform to compute the position of. </param>
        /// <param name="position"> The computed serializable hierarchical position of the child relative to the starting transform. </param>
        /// <param name="logging"> Whether to log errors if the computation fails. </param>
        /// <param name="autoAbsoluteRoot"> If true, and the starting point is an absolute root, the output position will also be flagged as an absolute root. </param>
        public static bool TryComputeChildHierarchicalPosition(this Transform startingParentTransform, Transform childTransform, out HierarchicalChildPosition position, 
            bool logging = true, bool autoAbsoluteRoot = true)
        {
            position = new HierarchicalChildPosition();
            position.StartHash = startingParentTransform.name.GetStableHashCode();
            if (autoAbsoluteRoot && startingParentTransform.parent == null)
                position.UseAbsoluteRoot = true;
            
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
                if (currentTransform == startingParentTransform)
                {
                    ComputeHash(ref position);
                    return true;
                }
            }
            
            // Reached the starting transform (supports root)
            if (currentTransform == startingParentTransform)
            {
                ComputeHash(ref position);
                return true;
            }

            if (logging)
                Debug.LogError($"Failed to compute hierarchical position from {startingParentTransform} to {childTransform}.");
            return false;

            // Store the hashcode of the name, which is very unlikely to randomly collide
            void ComputeHash(ref HierarchicalChildPosition position) => position.FinalDestinationHash = childTransform.name.GetStableHashCode();
        }
        
        /// <summary> Attempts to get the child transform at a hierarchical position relative to a starting transform. </summary>
        /// <param name="startingTransform"> The starting point of the hierarchy. </param>
        /// <param name="position"> The hierarchical position of the child relative to the starting transform. </param>
        /// <param name="child"> The child transform found at the hierarchical position. </param>
        /// <param name="logging"> Whether to log errors if the child is not found. </param>
        /// <param name="ignoreWrongStartHash"> If true, the starting transform hash will be ignored in all cases. <br/>
        /// (typically only ignored if <see cref="startingTransform"/> and <see cref="position"/> are both absolute roots) </param>
        /// <returns> Whether the child was found. </returns>
        public static bool TryGetChildHierarchical(this Transform startingTransform, HierarchicalChildPosition position, out Transform child, bool logging = true, bool ignoreWrongStartHash = false)
        {
            bool isAbsoluteRoot = position.UseAbsoluteRoot && startingTransform.parent == null;
            
            if (!isAbsoluteRoot && !ignoreWrongStartHash && position.StartHash != 0 && position.StartHash != startingTransform.name.GetStableHashCode())
            {
                // Find the probable correct starting transform by checking hashes
                Transform matchingTransform = null;
                var candidates = startingTransform.root.GetComponentsInChildren<Transform>();
                foreach (var candidate in candidates)
                {
                    if (candidate.name.GetStableHashCode() == position.StartHash)
                    {
                        matchingTransform = candidate;
                        break;
                    }
                }
                
                Debug.LogError($"Starting hash mismatch. \n Input StartingTransform: {startingTransform.name}, \n" +
                               $"Expected stable hash: {position.StartHash}, \n First transform matching hash: {matchingTransform?.name ?? "None"}");
                child = null;
                return false;
            }
            
            int transitIndex = 0;
            Transform currentTransform = startingTransform;
            child = startingTransform; // Support self-referencing
            
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

                    // Do all sibling steps in one go
                    var steps = 1;
                    // If future steps are also siblings, keep going in a tight loop to skip unnecessary transform lookups
                    while (position.TryGetTransit(transitIndex + 1, out var nextTransitDir) && nextTransitDir == HierarchicalChildPosition.TransitDir.Sibling)
                    {
                        ++steps;
                        ++transitIndex;
                    }
                    
                    var nextSiblingIndex = currentTransform.GetSiblingIndex() + steps;
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
                    currentTransform = child = currentTransform.GetChild(0);
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
            if (position.FinalDestinationHash != 0 && child.name.GetStableHashCode() != position.FinalDestinationHash)
            {
                if (logging)
                    Debug.LogError("Final destination hash mismatch.");
                return false;
            }
            return true;
        }
    }
}