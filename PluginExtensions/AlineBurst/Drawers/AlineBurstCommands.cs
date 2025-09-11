using Drawing;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using VLib.Unsafe.Structures;
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
                UnmanagedData64.ConvertFrom(data, out var dataPacked);
                return new AlineBurstDrawCommand(AlineDrawShape.Ray, dataPacked, color, duration, lineThickness);
            }
           
            public static void DrawRay(ref CommandBuilder draw, ref AlineBurstDrawCommand command)
            {
                ref readonly var rayData = ref command.drawData.ConvertTo<Data>();
                command.PushParameters(ref draw);
                draw.Ray(rayData.ray, rayData.length);
                command.PopParameters(ref draw);
            } 
        }

        public static class DashedLines
        {
            struct Data
            {
                public float3 start;
                public float3 end;
                public float dash;
                public float gap;
            }

            public static AlineBurstDrawCommand Create(in float3 start, in float3 end, float dash, float gap, Color color = default, float duration = default, byte lineThickness = default)
            {
                var data = new Data {start = start, end = end, dash = dash, gap = gap};
                UnmanagedData64.ConvertFrom(data, out var dataPacked);
                return new AlineBurstDrawCommand(AlineDrawShape.DashedLine, dataPacked, color, duration, lineThickness);
            }

            public static void DrawDashedLine(ref CommandBuilder draw, ref AlineBurstDrawCommand command)
            {
                ref readonly var lineData = ref command.drawData.ConvertTo<Data>();
                command.PushParameters(ref draw);
                draw.DashedLine(lineData.start, lineData.end, lineData.dash, lineData.gap);
                command.PopParameters(ref draw);
            }
        }

        public static class Spheres
        {
            public static AlineBurstDrawCommand Create(in SphereNative sphere, in Color color = default, float duration = default, byte lineThickness = default)
            {
                UnmanagedData64.ConvertFrom(sphere, out var dataPacked);
                return new AlineBurstDrawCommand(AlineDrawShape.Sphere, dataPacked, color, duration, lineThickness);
            }
           
            public static void DrawWireSphere(ref CommandBuilder draw, ref AlineBurstDrawCommand command)
            {
                ref readonly var sphereData = ref command.drawData.ConvertTo<SphereNative>();
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
                UnmanagedData64.ConvertFrom(data, out var dataPacked);
                return new AlineBurstDrawCommand(AlineDrawShape.CubeSolid, dataPacked, color, duration, lineThickness);
            }

            internal static void DrawWireCube(ref CommandBuilder draw, ref AlineBurstDrawCommand command)
            {
                ref readonly var data = ref command.drawData.ConvertTo<Data>();
                command.PushParameters(ref draw);
                draw.WireBox(data.position, data.rotation, data.size);
                command.PopParameters(ref draw);
            }
        }

        public class Capsules
        {
            internal static AlineBurstDrawCommand Create(in CapsuleNative capsule, Color color, float duration, byte lineThickness)
            {
                UnmanagedData64.ConvertFrom(capsule, out var dataPacked);
                return new AlineBurstDrawCommand(AlineDrawShape.Capsule, dataPacked, color, duration, lineThickness);
            }

            internal static void DrawWireCapsule(ref CommandBuilder draw, ref AlineBurstDrawCommand command)
            {
                ref readonly var capsuleData = ref command.drawData.ConvertTo<CapsuleNative>();
                command.PushParameters(ref draw);
                capsuleData.DrawAline(ref draw);
                command.PopParameters(ref draw);
            }
        }

        public class Arcs
        {
            struct Data
            {
                public float3 center;
                public float3 start;
                public float3 end;
                public byte solid;
                
                public Data(in float3 center, in float3 start, in float3 end, bool solid)
                {
                    this.center = center;
                    this.start = start;
                    this.end = end;
                    this.solid = solid ? (byte)1 : (byte)0;
                }
            }
            
            internal static AlineBurstDrawCommand Create(in float3 center, in float3 start, in float3 end, bool solid, Color color, float duration, byte lineThickness)
            {
                var data = new Data(center, start, end, solid);
                UnmanagedData64.ConvertFrom(data, out var dataPacked);
                return new AlineBurstDrawCommand(AlineDrawShape.Arc, dataPacked, color, duration, lineThickness);
            }
            
            internal static void DrawArc(ref CommandBuilder draw, ref AlineBurstDrawCommand command)
            {
                ref readonly var arcData = ref command.drawData.ConvertTo<Data>();
                command.PushParameters(ref draw);
                draw.Arc(arcData.center, arcData.start, arcData.end);
                command.PopParameters(ref draw);
            }
        }
    }
}