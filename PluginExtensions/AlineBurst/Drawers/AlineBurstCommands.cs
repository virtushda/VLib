using Drawing;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using VLib.Unsafe.Utility;

namespace VLib.Aline.Drawers
{
    internal static class AlineBurstCommands
    {
        public static class Rays
        {
            struct Data
            {
                public Ray ray;
                public float length;
            }
            
            public static AlineBurstDrawCommand Create(Ray ray, float length, Color color = default, float duration = default, byte lineThickness = default)
            {
                var data = new Data { ray = ray, length = length };
                float4x4 dataPacked = default;
                VUnsafeUtil.StructCopyAIntoB(data, ref dataPacked);
                return new AlineBurstDrawCommand(AlineDrawShape.Ray, dataPacked, color, duration, lineThickness);
            }
           
            public static void DrawRay(ref CommandBuilder draw, ref AlineBurstDrawCommand command)
            {
                var rayData = UnsafeUtility.As<float4x4, Data>(ref command.drawData);
                command.PushParameters(ref draw);
                draw.Ray(rayData.ray, rayData.length);
                command.PopParameters(ref draw);
            } 
        }
        
        public static class Spheres
        {
            public static AlineBurstDrawCommand Create(in SphereNative sphere, in Color color = default, float duration = default, byte lineThickness = default)
            {
                float4x4 dataPacked = default;
                VUnsafeUtil.StructCopyAIntoB(sphere, ref dataPacked);
                return new AlineBurstDrawCommand(AlineDrawShape.Sphere, dataPacked, color, duration, lineThickness);
            }
           
            public static void DrawWireSphere(ref CommandBuilder draw, ref AlineBurstDrawCommand command)
            {
                var sphereData = UnsafeUtility.As<float4x4, SphereNative>(ref command.drawData);
                command.PushParameters(ref draw);
                draw.WireSphere(sphereData.Position, sphereData.Radius);
                command.PopParameters(ref draw);
            } 
        }

        public static class Boxes
        {
            struct Data
            {
                public float3 position;
                public quaternion rotation;
                public float3 size;
            }

            internal static AlineBurstDrawCommand Create(in float3 position, in float3 size, in quaternion rotation, Color color = default, float duration = default, byte lineThickness = default)
            {
                var data = new Data { position = position, rotation = rotation, size = size };
                float4x4 dataPacked = default;
                VUnsafeUtil.StructCopyAIntoB(data, ref dataPacked);
                return new AlineBurstDrawCommand(AlineDrawShape.CubeSolid, dataPacked, color, duration, lineThickness);
            }

            internal static void DrawWireCube(ref CommandBuilder draw, ref AlineBurstDrawCommand command)
            {
                var data = UnsafeUtility.As<float4x4, Data>(ref command.drawData);
                command.PushParameters(ref draw);
                draw.WireBox(data.position, data.rotation, data.size);
                command.PopParameters(ref draw);
            }
        }

        public class Capsules
        {
            internal static AlineBurstDrawCommand Create(in CapsuleNative capsule, Color color, float duration, byte lineThickness)
            {
                float4x4 dataPacked = default;
                VUnsafeUtil.StructCopyAIntoB(capsule, ref dataPacked);
                return new AlineBurstDrawCommand(AlineDrawShape.Capsule, dataPacked, color, duration, lineThickness);
            }

            internal static void DrawWireCapsule(ref CommandBuilder draw, ref AlineBurstDrawCommand command)
            {
                var capsuleData = UnsafeUtility.As<float4x4, CapsuleNative>(ref command.drawData);
                command.PushParameters(ref draw);
                capsuleData.DrawAline(ref draw);
                command.PopParameters(ref draw);
            }
        }
    }
}