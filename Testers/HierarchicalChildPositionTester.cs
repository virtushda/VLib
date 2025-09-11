using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using VLib.Structures;

namespace VLib.Testers
{
    public class HierarchicalChildPositionTester : MonoBehaviour
    {
        public Transform parent;
        public HierarchicalChildPosition position;

        [SerializeField, FoldoutGroup("Position From String"), TextArea]
        string positionString;
        
        [PropertySpace, FoldoutGroup("Position From String"), Button]
        void PositionFromString()
        {
            // Starts with parent
            int parentStartIndex = positionString.IndexOf("Parent", StringComparison.Ordinal);
            if (parentStartIndex == -1)
            {
                Debug.LogError("Failed to find parent in position string.");
                return;
            }
            
            var positionStringCopy = positionString.Substring(parentStartIndex + "Parent".Length);
            position = new HierarchicalChildPosition();
            
            // Consume 'Child', 'Sib' and 'Dest' tokens to build a position
            string[] transitTokens = { "Child", "Sib", "Dest" };
            Span<int> transitTokenIndices = stackalloc int[transitTokens.Length];
            while (true)
            {
                // Get next token
                transitTokenIndices.Clear();
                for (int i = 0; i < transitTokens.Length; i++)
                {
                    var transitToken = transitTokens[i];
                    int transitTokenIndex = positionStringCopy.IndexOf(transitToken, StringComparison.Ordinal);
                    if (transitTokenIndex >= 0)
                        transitTokenIndices[i] = transitTokenIndex;
                    else
                        transitTokenIndices[i] = int.MaxValue;
                }
                
                // Find minimum
                var minimumIndex = -1;
                var minimumIndexValue = int.MaxValue;
                for (int i = 0; i < transitTokenIndices.Length; i++)
                {
                    if (transitTokenIndices[i] < minimumIndexValue)
                    {
                        minimumIndexValue = transitTokenIndices[i];
                        minimumIndex = i;
                    }
                }
                
                // No more tokens
                if (minimumIndexValue == int.MaxValue)
                    break;
                
                // Consume token
                positionStringCopy = positionStringCopy.Substring(minimumIndexValue + transitTokens[minimumIndex].Length);
                
                // Record transit
                if (minimumIndex == 0) // Child
                    position.RecordAtEnd(true);
                else if (minimumIndex == 1) // Sibling
                    position.RecordAtEnd(false);
                else // Destination, we are done
                    break;
            }
        }
        
        [PropertySpace, Button]
        void TestGenerate()
        {
            if (!parent.TryComputeChildHierarchicalPosition(this.transform, out position))
                Debug.LogError("Failed to compute hierarchical position.");
        }

        [PropertySpace, Button]
        void TestTraverse()
        {
            if (parent.TryGetChildHierarchical(position, out var child))
                Debug.Log($"Found child: {child}");
            else
                Debug.LogError("Failed to find child.");
        }
        
        [PropertySpace, Button]
        void TestRoundTripAllDescendants() => HierarchicalChildPosition.RoundTripTestAllDescendants(parent);
        
        [PropertySpace, Button]
        void CompareTwoHierarchies(Transform hierarchy1, Transform hierarchy2)
        {
            var children1 = hierarchy1.GetComponentsInChildren<Transform>();
            var children2 = hierarchy2.GetComponentsInChildren<Transform>();
            
            // Check count
            if (children1.Length != children2.Length)
                Debug.LogError($"Hierarchy count mismatch. {children1.Length} != {children2.Length}");

            // Check precise
            for (int i = 0; i < math.min(children1.Length, children2.Length); i++)
            {
                var child1 = children1[i];
                var child2 = children2[i];
                if (child1.name != child2.name)
                    Debug.LogError($"Mismatch at index {i}. {child1.name} != {child2.name}");
            }
            
            // Find transforms only in one and not the other
            var onlyIn1 = FindTransformsMissingInOther(children1, children2);
            var onlyIn2 = FindTransformsMissingInOther(children2, children1);
            if (onlyIn1 is {Count: > 0})
            {
                foreach (var missingTransform in onlyIn1)
                    Debug.LogError($"Only in 1: {missingTransform}");
            }
            if (onlyIn2 is {Count: > 0})
            {
                foreach (var missingTransform in onlyIn2)
                    Debug.LogError($"Only in 2: {missingTransform}");
            }
            
            
            Debug.Log("Hierarchies match.");
            return;
            
            List<string> FindTransformsMissingInOther(Transform[] transforms1, Transform[] transforms2)
            {
                var missing = new List<string>();
                foreach (var transform1 in transforms1)
                {
                    if (Array.IndexOf(transforms2, transform1) == -1)
                        missing.Add(transform1.ToString());
                }
                return missing;
            }
        }
    }
}