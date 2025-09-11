using Unity.Mathematics;
using UnityEngine;

namespace VLib.Testers
{
    [ExecuteAlways]
    public class VMathRollBetweenRotationsTester : MonoBehaviour
    {
        public Transform other;

        public float rollDegrees;
        
        void Update()
        {
            if (other == null)
            {
                // Get child or create new
                other = transform.childCount > 0 ? transform.GetChild(0) : new GameObject("Other Roll Tester").transform;
            }
            
            rollDegrees = math.degrees(VMath.SignedRollDeltaBetween(transform.rotation, other.rotation, VMath.Forward3, VMath.Up3));
        }
    }
}