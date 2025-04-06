using System;
using Unity.Mathematics;
using Drawing;
using UnityEngine;

namespace VLib.PluginExtensions
{
    public static class AlineExt
    {
        /*/// <summary> Draws a traditional octahedron shape for a bone. </summary>
        /// <param name="start"> The (world) start of the bone. </param>
        /// <param name="end"> The (world) end of the bone. </param>
        /// <param name="up"> The (world) up direction of the bone. </param>
        /// <param name="widthMultiplier"> The width of the octahedron as a multiplier of the bone length. </param>
        /// <param name="middle01"> The middle of the bone is defined as the point at this 0-1 (start:0, end:1) value along the bone. </param>
        public static void DrawOctahedron(float3 start, float3 end, float3 up, float widthMultiplier = 0.15f, float middle01 = 0.15f)
        {
            Span<float3> middleDiamondPoints = stackalloc float3[4];
            if (!CalculateOctahedronPoints(start, end, up, widthMultiplier, middle01, ref middleDiamondPoints, out var width))
                return;
            
            // Diamond
            for (int i = 0; i < middleDiamondPoints.Length; i++)
            {
                var nextIndex = (i + 1) % middleDiamondPoints.Length;
                Draw.Line(middleDiamondPoints[i], middleDiamondPoints[nextIndex]);
            }
            
            // Start to diamond
            for (int i = 0; i < middleDiamondPoints.Length; i++)
                Draw.Line(start, middleDiamondPoints[i]);
            
            // Diamond to end
            for (int i = 0; i < middleDiamondPoints.Length; i++)
                Draw.Line(middleDiamondPoints[i], end);
            
            // Draw spheres at the ends
            Draw.WireSphere(start, width);
            Draw.WireSphere(end, width);
        }*/

        /// <summary> Draws a traditional octahedron shape for a bone. </summary>
        /// <param name="start"> The (world) start of the bone. </param>
        /// <param name="end"> The (world) end of the bone. </param>
        /// <param name="up"> The (world) up direction of the bone. </param>
        /// <param name="widthMultiplier"> The width of the octahedron as a multiplier of the bone length. </param>
        /// <param name="middle01"> The middle of the bone is defined as the point at this 0-1 (start:0, end:1) value along the bone. </param>
        public static void Octahedron(this CommandBuilder drawer, float3 start, float3 end, float3 up, float widthMultiplier = 0.15f, float middle01 = 0.15f)
        {
            Span<float3> middleDiamondPoints = stackalloc float3[4];
            if (!CalculateOctahedronPoints(start, end, up, widthMultiplier, middle01, ref middleDiamondPoints, out var width))
                return;
            
            // Diamond
            for (int i = 0; i < middleDiamondPoints.Length; i++)
            {
                var nextIndex = (i + 1) % middleDiamondPoints.Length;
                drawer.Line(middleDiamondPoints[i], middleDiamondPoints[nextIndex]);
            }
            
            // Start to diamond
            for (int i = 0; i < middleDiamondPoints.Length; i++)
                drawer.Line(start, middleDiamondPoints[i]);
            
            // Diamond to end
            for (int i = 0; i < middleDiamondPoints.Length; i++)
                drawer.Line(middleDiamondPoints[i], end);
            
            // Draw spheres at the ends
            drawer.WireSphere(start, width);
            drawer.WireSphere(end, width);
        }

        static bool CalculateOctahedronPoints(float3 start, float3 end, float3 up, float widthMultiplier, float middle01, ref Span<float3> middleDiamondPoints, out float width)
        {
            width = 1f;
            
            var startToEnd = end - start;
            var startToEndLength = math.length(startToEnd);
            if (startToEndLength < 0.0001f)
                return false;
            var startToEndDir = startToEnd / startToEndLength;
            
            // Calculate the middle point
            var middle = start + startToEnd * middle01;
            
            // Create coordinate system using provided up vector
            up = math.normalize(up);
            if (up.Equals(startToEndDir))
                Debug.LogError("DrawOctahedron: Up vector is collinear with bone direction!");
            var right = math.cross(startToEndDir, up);
            
            width = startToEndLength * widthMultiplier;
            
            // Calculate diamond points
            var widthOffset = right * width;
            var heightOffset = up * width;
            
            BurstAssert.True(middleDiamondPoints.Length == 4);
            middleDiamondPoints[0] = middle + widthOffset;
            middleDiamondPoints[1] = middle - widthOffset;
            middleDiamondPoints[2] = middle + heightOffset;
            middleDiamondPoints[3] = middle - heightOffset;
            
            return true;
        }
    }
}