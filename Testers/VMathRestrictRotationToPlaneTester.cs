/*using System;
using UnityEngine;
using Unity.Mathematics;
using VLib.Aline;

namespace VLib.Testers
{
    public class RestrictRotationToPlaneTester : MonoBehaviour
    {
        [Tooltip("Child transform whose localRotation provides the input quaternion.")]
        public Transform rotationChild;

        [Tooltip("Reference object to visualize plane normal (optional).")]
        public Transform planeNormalVisualizer;

        // When enabled, applies restriction and prints results to Console.
        public bool runTest = true;

        void OnEnable()
        {
            rotationChild = new GameObject("RotationChild").transform;
            rotationChild.parent = transform;
            rotationChild.localPosition = Vector3.zero;
            rotationChild.localRotation = Quaternion.identity;
            
            planeNormalVisualizer = new GameObject("PlaneNormalVisualizer").transform;
        }

        void Update()
        {
            if (!runTest || !rotationChild)
                return;

            // Define plane normal using this transform's .right
            float3 planeNormal = transform.right;

            // Input quaternion from child local rotation
            quaternion inputQuat = rotationChild.rotation;

            // Call the method to test
            quaternion restrictedQuat = VMath.RestrictRotationToPlane(inputQuat, planeNormal);

            // Show
            var matrix = float4x4.TRS(transform.position, restrictedQuat, new float3(1f));
            AlineBurst.EnqueueTransformAxis(matrix);
        }
    }
}*/