using System.Runtime.CompilerServices;
using Drawing;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using VLib.Aline.Drawers;
using VLib.Systems;
using VLib.Threading;
using VLib.Utility;

namespace VLib.Aline
{
    /// <summary> A central burst-compatible system to just fricken draw stuff without boilerplate! </summary>
    [BurstCompile]
    public abstract class AlineBurst
    {
        static readonly SharedStatic<AlineBurstNative> Shared = SharedStatic<AlineBurstNative>.GetOrCreate<AlineBurst>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        static void Init()
        {
            Shared.Data.InitNative();
            VApplicationMonitor.AddQuitEvent(new SortedAction(Dispose, 1000));
        }

        static void Dispose() => Shared.Data.DisposeNative();

        struct AlineBurstNative
        {
            const byte CommandListCapacity = 8;
            
            UnsafeList<AlineBurstCommandList> commandLists;
            int listIndex;

            internal bool IsCreated => commandLists.IsCreated;

            internal void InitNative()
            {
                if (commandLists.IsCreated)
                {
                    for (int i = 0; i < commandLists.Length; i++)
                        commandLists.ElementAt(i).Dispose();
                    commandLists.Dispose();
                }
                
                commandLists = new UnsafeList<AlineBurstCommandList>(CommandListCapacity, Allocator.Persistent);
                for (int i = 0; i < CommandListCapacity; i++)
                    commandLists.AddNoResize(new AlineBurstCommandList(64));
                
                listIndex = 0;
            }
            
            internal void DisposeNative()
            {
                if (!commandLists.IsCreated)
                    return;
                for (int i = 0; i < commandLists.Length; i++)
                    commandLists.ElementAt(i).Dispose();
                commandLists.Dispose();
            }

            internal void Add(in AlineBurstDrawCommand command)
            {
                if (!IsInitialized)
                {
                    Debug.LogError("AlineBurstNative not initialized! (play mode only!)");
                    return;
                }
                var writeIndex = InterlockedUtil.IncrementModuloSpinning(ref listIndex, commandLists.Length);
                ref var writeList = ref commandLists.ElementAt(writeIndex);
                writeList.Add(command);
            }

            internal void DrawAllInternal(ref CommandBuilder draw)
            {
                for (int i = 0; i < commandLists.Length; i++)
                    commandLists.ElementAt(i).DrawAll(ref draw);
            }
        }
        
        public static bool IsInitialized => Shared.Data.IsCreated;
        
        public static void EnqueueRay(in Ray ray, float length, Color color = default, float duration = default, byte lineThickness = default, bool allowDurationWhilePaused = false)
        {
            if (!IsInitialized)
            {
                // Redirect to editor main-thread draw if possible
                bool didDraw = false;
                DrawRayFallback(ref didDraw, ray, length, color, duration, lineThickness);
                
                if (!didDraw)
                    Debug.LogError("AlineBurstNative not initialized! (play mode only!)");
                return;
            }
            
            HandleDurationWhilePaused(ref duration, allowDurationWhilePaused);
            var rayCommand = AlineBurstCommands.Rays.Create(ray, length, color, duration, lineThickness);
            Shared.Data.Add(rayCommand);
        }

        [BurstDiscard]
        static void DrawRayFallback(ref bool didDraw, in Ray ray, float length, Color color = default, float duration = default, byte lineThickness = default)
        {
            if (duration > 0.01f)
                Draw.editor.PushDuration(duration);
            if (lineThickness > 1)
                Draw.editor.PushLineWidth(lineThickness);
            
            Draw.editor.Ray(ray, length, color);
            didDraw = true;
            
            if (lineThickness > 1)
                Draw.editor.PopLineWidth();
            if (duration > 0.01f)
                Draw.editor.PopDuration();
        }
        
        public static void EnqueueLine(in float3 start, in float3 end, Color color = default, float duration = default, byte lineThickness = default, bool allowDurationWhilePaused = false)
        {
            var startToEnd = end - start;
            var length = math.length(startToEnd);
            // Prevent division by zero
            if (length < 0.0001f)
                return;
            HandleDurationWhilePaused(ref duration, allowDurationWhilePaused);
            EnqueueRay(new Ray(start, startToEnd / length), length, color, duration, lineThickness);
        }
        
        public static void EnqueueDashedLine(in float3 start, in float3 end, float dash, float gap, Color color = default, float duration = default, byte lineThickness = default, bool allowDurationWhilePaused = false)
        {
            if (!IsInitialized)
            {
                // Redirect to editor main-thread draw if possible
                bool didDraw = false;
                DrawDashedLineFallback(ref didDraw, start, end, dash, gap, color, duration, lineThickness);
                
                if (!didDraw)
                    Debug.LogError("AlineBurstNative not initialized! (play mode only!)");
                return;
            }
            var lengthSqr = math.distancesq(start, end);
            if (lengthSqr < 0.00001f)
                return;
            HandleDurationWhilePaused(ref duration, allowDurationWhilePaused);
            var lineCommand = AlineBurstCommands.DashedLines.Create(start, end, dash, gap, color, duration, lineThickness);
            Shared.Data.Add(lineCommand);
        }
        
        [BurstDiscard]
        static void DrawDashedLineFallback(ref bool didDraw, in float3 start, in float3 end, float dash, float gap, Color color = default, float duration = default, byte lineThickness = default)
        {
            if (duration > 0.01f)
                Draw.editor.PushDuration(duration);
            if (lineThickness > 1)
                Draw.editor.PushLineWidth(lineThickness);
            
            Draw.editor.DashedLine(start, end, dash, gap, color);
            didDraw = true;
            
            if (lineThickness > 1)
                Draw.editor.PopLineWidth();
            if (duration > 0.01f)
                Draw.editor.PopDuration();
        }
        
        public static void EnqueueSphere(in float3 position, float radius, Color color = default, float duration = default, byte lineThickness = default, bool allowDurationWhilePaused = false)
        {
            if (!IsInitialized)
            {
                // Redirect to editor main-thread draw if possible
                bool didDraw = false;
                DrawSphereFallback(ref didDraw, position, radius, color, duration, lineThickness);
                
                if (!didDraw)
                    Debug.LogError("AlineBurstNative not initialized! (play mode only!)");
                return;
            }
            HandleDurationWhilePaused(ref duration, allowDurationWhilePaused);
            var sphereCommand = AlineBurstCommands.Spheres.Create(new SphereNative(position, radius), color, duration, lineThickness);
            Shared.Data.Add(sphereCommand);
        }
        
        [BurstDiscard]
        static void DrawSphereFallback(ref bool didDraw, in float3 position, float radius, Color color = default, float duration = default, byte lineThickness = default)
        {
            if (duration > 0.01f)
                Draw.editor.PushDuration(duration);
            if (lineThickness > 1)
                Draw.editor.PushLineWidth(lineThickness);
            
            Draw.editor.WireSphere(position, radius, color);
            didDraw = true;
            
            if (lineThickness > 1)
                Draw.editor.PopLineWidth();
            if (duration > 0.01f)
                Draw.editor.PopDuration();
        }
        
        public static void EnqueueBox(in float3 position, in quaternion rotation, in float3 size, Color color = default, float duration = default, byte lineThickness = default, bool allowDurationWhilePaused = false)
        {
            if (!IsInitialized)
            {
                // Redirect to editor main-thread draw if possible
                bool didDraw = false;
                DrawBoxFallback(ref didDraw, position, rotation, size, color, duration, lineThickness);
                
                if (!didDraw)
                    Debug.LogError("AlineBurstNative not initialized! (play mode only!)");
                return;
            }
            HandleDurationWhilePaused(ref duration, allowDurationWhilePaused);
            var cubeCommand = AlineBurstCommands.Boxes.Create(position, size, rotation, color, duration, lineThickness);
            Shared.Data.Add(cubeCommand);
        }
        
        [BurstDiscard]
        static void DrawBoxFallback(ref bool didDraw, in float3 position, in quaternion rotation, in float3 size, Color color = default, float duration = default, byte lineThickness = default)
        {
            if (duration > 0.01f)
                Draw.editor.PushDuration(duration);
            if (lineThickness > 1)
                Draw.editor.PushLineWidth(lineThickness);
            
            Draw.editor.WireBox(position, rotation, size, color);
            didDraw = true;
            
            if (lineThickness > 1)
                Draw.editor.PopLineWidth();
            if (duration > 0.01f)
                Draw.editor.PopDuration();
        }
        
        public static void EnqueueCapsule(in CapsuleNative capsule, Color color = default, float duration = default, byte lineThickness = default, bool allowDurationWhilePaused = false)
        {
            if (!IsInitialized)
            {
                // Redirect to editor main-thread draw if possible
                bool didDraw = false;
                DrawCapsuleFallback(ref didDraw, capsule, color, duration, lineThickness);
                
                if (!didDraw)
                    Debug.LogError("AlineBurstNative not initialized! (play mode only!)");
                return;
            }
            HandleDurationWhilePaused(ref duration, allowDurationWhilePaused);
            var capsuleCommand = AlineBurstCommands.Capsules.Create(capsule, color, duration, lineThickness);
            Shared.Data.Add(capsuleCommand);
        }
        
        [BurstDiscard]
        static void DrawCapsuleFallback(ref bool didDraw, in CapsuleNative capsule, Color color = default, float duration = default, byte lineThickness = default)
        {
            if (duration > 0.01f)
                Draw.editor.PushDuration(duration);
            if (lineThickness > 1)
                Draw.editor.PushLineWidth(lineThickness);
            Draw.editor.PushColor(color);
            
            capsule.DrawAline(ref Draw.editor);
            didDraw = true;
            
            Draw.editor.PopColor();
            if (lineThickness > 1)
                Draw.editor.PopLineWidth();
            if (duration > 0.01f)
                Draw.editor.PopDuration();
        }
        
        public static void EnqueueArc(in float3 center, in float3 start, in float3 end, bool solid, Color color = default, float duration = default, byte lineThickness = default, bool allowDurationWhilePaused = false)
        {
            if (!IsInitialized)
            {
                // Redirect to editor main-thread draw if possible
                bool didDraw = false;
                DrawArcFallback(ref didDraw, center, start, end, color, duration, lineThickness);
                
                if (!didDraw)
                    Debug.LogError("AlineBurstNative not initialized! (play mode only!)");
                return;
            }
            HandleDurationWhilePaused(ref duration, allowDurationWhilePaused);
            var arcCommand = AlineBurstCommands.Arcs.Create(center, start, end, solid, color, duration, lineThickness);
            Shared.Data.Add(arcCommand);
        }
        
        [BurstDiscard]
        static void DrawArcFallback(ref bool didDraw, in float3 center, in float3 start, in float3 end, Color color = default, float duration = default, byte lineThickness = default)
        {
            if (duration > 0.01f)
                Draw.editor.PushDuration(duration);
            if (lineThickness > 1)
                Draw.editor.PushLineWidth(lineThickness);
            Draw.editor.PushColor(color);
            
            Draw.editor.Arc(center, start, end);
            didDraw = true;
            
            Draw.editor.PopColor();
            if (lineThickness > 1)
                Draw.editor.PopLineWidth();
            if (duration > 0.01f)
                Draw.editor.PopDuration();
        }

        /// <summary> Draws a transform's axis in world space. </summary>
        public static void EnqueueTransformAxis(in float4x4 matrix, float scale = 1f, float duration = default, byte lineThickness = 1, in Color? colorX = null, in Color? colorY = null, in Color? colorZ = null)
        {
            var colorXFinal = colorX ?? Color.red;
            var colorYFinal = colorY ?? Color.green;
            var colorZFinal = colorZ ?? Color.blue;
            var position = matrix.GetPositionDelta();
            var transformedRight = math.transform(matrix, VMath.Right3 * scale);
            var transformedUp = math.transform(matrix, VMath.Up3 * scale);
            var transformedForward = math.transform(matrix, VMath.Forward3 * scale);
            
            HandleDurationWhilePaused(ref duration, false);
            
            EnqueueLine(position, transformedRight, colorXFinal, duration, lineThickness);
            EnqueueLine(position, transformedUp, colorYFinal, duration, lineThickness);
            EnqueueLine(position, transformedForward, colorZFinal, duration, lineThickness);
        }

        /// <summary> Draws a transform's axis in world space. This version uses a drawer directly to support edit mode. </summary>
        public static void DrawTransformAxis(ref CommandBuilder draw, in float4x4 matrix, float scale = 1f, float duration = default, byte lineThickness = 1, in Color? colorX = null, in Color? colorY = null, in Color? colorZ = null)
        {
            var colorXFinal = colorX ?? Color.red;
            var colorYFinal = colorY ?? Color.green;
            var colorZFinal = colorZ ?? Color.blue;
            var position = matrix.GetPositionDelta();
            var transformedRight = math.transform(matrix, VMath.Right3 * scale);
            var transformedUp = math.transform(matrix, VMath.Up3 * scale);
            var transformedForward = math.transform(matrix, VMath.Forward3 * scale);
            
            HandleDurationWhilePaused(ref duration, false);
            
            if (duration > 0.01f)
                draw.PushDuration(duration);
            if (lineThickness > 1)
                draw.PushLineWidth(lineThickness);
            
            draw.Line(position, transformedRight, colorXFinal);
            draw.Line(position, transformedUp, colorYFinal);
            draw.Line(position, transformedForward, colorZFinal);
            
            if (lineThickness > 1)
                draw.PopLineWidth();
            if (duration > 0.01f)
                draw.PopDuration();
        }

        static readonly ProfilerMarker DrawAllProfileMarker = new("AlineBurst.DrawAll");

        /// <summary> This must be called to actually push enqueued draws to aline! <br/>
        /// Don't forget to dispose the command builder as well... </summary>
        public static void DrawAll_MainThread(ref CommandBuilder draw)
        {
            MainThread.AssertMainThreadConditional();
            DrawAllBurst(ref draw);
        }
        
        [BurstCompile]
        static void DrawAllBurst(ref CommandBuilder draw)
        {
            using var _ = DrawAllProfileMarker.Auto();
            Shared.Data.DrawAllInternal(ref draw);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void HandleDurationWhilePaused(ref float duration, bool allowDurationWhilePaused)
        {
            if (duration > 0)
            {
                // If time is paused, it's incredibly dangerous to continuously build up commands with duration
                if (VTime.deltaTime == 0 && !allowDurationWhilePaused)
                    duration = 0;
            }
        }
    }
}