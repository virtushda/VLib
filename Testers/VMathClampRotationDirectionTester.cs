using Unity.Mathematics;
using UnityEngine;
using VLib.Aline;

namespace VLib.Testers
{
    public class VMathClampRotationDirectionTester : MonoBehaviour
    {
        [Range(0f, 180f)] public float angleLimit = 45f;
        Transform targetRotation;

        void OnEnable()
        {
            targetRotation = new GameObject("TargetRotation").transform;
            targetRotation.parent = transform;
            targetRotation.localPosition = Vector3.zero;
            targetRotation.localRotation = Quaternion.identity;
        }

        void Update()
        {
            if (!targetRotation)
                return;

            // Input quaternion from child local rotation
            quaternion guideRotation = transform.rotation;
            quaternion targetQuat = targetRotation.rotation;

            // Call the method to test
            quaternion clampedQuat = VMath.ClampRotationDirection(targetQuat, guideRotation, math.radians(angleLimit));

            // Draw guide
            AlineBurst.EnqueueRay(new Ray(transform.position, math.rotate(guideRotation, math.forward())), 10f, ColorExt.blueBright);
            
            // Draw clamped
            var drawMatrix = float4x4.TRS(transform.position, clampedQuat, new float3(1f));
            AlineBurst.EnqueueTransformAxis(drawMatrix);
            
            // Draw angle limit cone
            var vector = transform.forward;
            var angleRot = quaternion.AxisAngle(transform.right, math.radians(angleLimit));
            vector = math.rotate(angleRot, vector);
            // Spin around
            var steps = 16;
            var color = ColorExt.orange.WithAlpha(0.4f);
            var spinRot = quaternion.AxisAngle(transform.forward, math.radians(360f / steps));
            for (int i = 0; i < steps; i++)
            {
                vector = math.rotate(spinRot, vector);
                AlineBurst.EnqueueRay(new Ray(transform.position, vector), 10f, color);
            }
        }
    }
}