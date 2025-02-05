using Sirenix.OdinInspector;
using UnityEngine;
using VLib.Structures;

namespace VLib.Testers
{
    public class HierarchicalChildPositionTester : MonoBehaviour
    {
        public Transform parent;
        public HierarchicalChildPosition position;

        [Button]
        void TestGenerate()
        {
            if (!parent.TryComputeChildHierarchicalPosition(this.transform, out position))
                Debug.LogError("Failed to compute hierarchical position.");
        }

        [Button]
        void TestTraverse()
        {
            if (parent.TryGetChildHierarchical(position, out var child))
                Debug.Log($"Found child: {child}");
            else
                Debug.LogError("Failed to find child.");
        }
    }
}