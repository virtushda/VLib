using Drawing;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using VLib.Aline.Drawers;
using VLib.Threading;
using VLib.Utility;

namespace VLib.Aline
{
    /// <summary> A central burst-compatible system to just fricken draw stuff without boilerplate! </summary>
    [BurstCompile]
    public abstract class AlineBurst
    {
        static readonly SharedStatic<AlineBurstNative> Shared = SharedStatic<AlineBurstNative>.GetOrCreate<AlineBurst>();
        class AlineBurstNativeID {}
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
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
        
        public static void EnqueueRay(in Ray ray, float length, Color color = default, float duration = default, byte lineThickness = default)
        {
            if (!IsInitialized)
                return;
            var rayCommand = AlineBurstCommands.Rays.Create(ray, length, color, duration, lineThickness);
            Shared.Data.Add(rayCommand);
        }
        
        public static void EnqueueLine(in float3 start, in float3 end, Color color = default, float duration = default, byte lineThickness = default)
        {
            if (!IsInitialized)
                return;
            var startToEnd = end - start;
            var length = math.length(startToEnd);
            // Prevent division by zero
            if (length < 0.0001f)
                return;
            EnqueueRay(new Ray(start, startToEnd / length), length, color, duration, lineThickness);
        }
        
        public static void EnqueueSphere(in float3 position, float radius, Color color = default, float duration = default, byte lineThickness = default)
        {
            if (!IsInitialized)
                return;
            var sphereCommand = AlineBurstCommands.Spheres.Create(new SphereNative(position, radius), color, duration, lineThickness);
            Shared.Data.Add(sphereCommand);
        }
        
        public static void EnqueueBox(in float3 position, in quaternion rotation, in float3 size, Color color = default, float duration = default, byte lineThickness = default)
        {
            if (!IsInitialized)
                return;
            var cubeCommand = AlineBurstCommands.Boxes.Create(position, size, rotation, color, duration, lineThickness);
            Shared.Data.Add(cubeCommand);
        }
        
        public static void EnqueueCapsule(in CapsuleNative capsule, Color color = default, float duration = default, byte lineThickness = default)
        {
            if (!IsInitialized)
                return;
            var capsuleCommand = AlineBurstCommands.Capsules.Create(capsule, color, duration, lineThickness);
            Shared.Data.Add(capsuleCommand);
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
    }
}