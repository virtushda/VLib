using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Assertions;

namespace VLib.Physics
{
    /// <summary> A low level structure to facilitate setting up a batch of raycast commands, potentially from multiple parallel jobs. </summary>
    public unsafe struct BatchRaycastCommands
    {
        NativeList<RaycastCommand> commands;
        UnsafeList<RaycastCommand>* unsafeCommands;
        int hitsPerRay;
        NativeList<RaycastHit> hits;

        [MarshalAs(UnmanagedType.U1)]
        bool jobActive;
        JobHandle jobHandle;
        
        // Editor safety
        int unsafeSetterLock;

        /// <summary> Setting this property is not concurrent-safe </summary>
        public int Capacity
        {
            get => commands.Capacity;
            set
            {
                CheckJobInactive();
                MarkUnsafeSetBegin();
                CheckUnsafeSetSingleOp();
                commands.Capacity = value;
                hits.Capacity = value * hitsPerRay;
                MarkUnsafeSetEnd();
            }
        }

        /// <summary> Setting this property is not concurrent-safe </summary>
        public int HitsPerRay
        {
            get => hitsPerRay;
            set
            {
                CheckJobInactive();
                MarkUnsafeSetBegin();
                CheckUnsafeSetSingleOp();
                hitsPerRay = value;
                hits.Capacity = Capacity * hitsPerRay;
                MarkUnsafeSetEnd();
            }
        }
        
        public int Length => commands.Length;
        
        public bool JobActive => jobActive;

        public BatchRaycastCommands(int maxCommands, int maxHitsPerRay, Allocator allocator = Allocator.Persistent)
        {
            hitsPerRay = maxHitsPerRay;
            commands = new NativeList<RaycastCommand>(maxCommands, allocator);
            hits = new NativeList<RaycastHit>(maxCommands * maxHitsPerRay, allocator);

            unsafeCommands = commands.GetUnsafeList();
      
            jobActive = false;
            jobHandle = default;
            
            unsafeSetterLock = 0;
        }

        public void Dispose()
        {
            CheckJobInactive();
            MarkUnsafeSetBegin();
            unsafeCommands = null;
            commands.Dispose();
            hits.Dispose();
            MarkUnsafeSetEnd();
        }
        
        /// <summary> Concurrent-safe </summary>
        public int AddCommandNoResize(in RaycastCommand command)
        {
            CheckJobInactive();
            MarkUnsafeSetBegin();
            var commandIndex = Interlocked.Increment(ref unsafeCommands->m_length) - 1;
            // Editor-only check for pushing the length past the capacity. Bumping the length directly is very fast, but it must not exceed the capacity.
            CheckCommandLengthNoResize();
            (*unsafeCommands)[commandIndex] = command;
            MarkUnsafeSetEnd();
            return commandIndex;
        }

        /// <summary> Non-concurrent add, faster in single-threaded scenarios. </summary>
        public int AddCommandNoResizeNoConcurrency(in RaycastCommand command)
        {
            CheckJobInactive();
            MarkUnsafeSetBegin();
            var commandIndex = unsafeCommands->Length;
            commands.AddNoResize(command);
            MarkUnsafeSetEnd();
            return commandIndex;
        }
        
        /// <summary> Thread-safe </summary>
        public void Clear()
        {
            CheckJobInactive();
            MarkUnsafeSetBegin();
            commands.Clear();
            hits.Clear();
            MarkUnsafeSetEnd();
        }

        /// <summary> Schedules the job, this method may only be called on the main thread. <br/>
        /// The output jobhandle is NOT to be completed, it is only passed out for dependency purposes. <br/>
        /// You must call <see cref="CompleteJob"/>. </summary>
        public void ScheduleJob(JobHandle inDeps, out JobHandle outDeps)
        {
            CheckJobInactive();
            
            // Prep data
            HitsPerRay = hitsPerRay;
            
            MarkUnsafeSetBegin();
            CheckUnsafeSetSingleOp();
            
            jobActive = true;
            outDeps = jobHandle = RaycastCommand.ScheduleBatch(commands, hits, 16, HitsPerRay, inDeps);
            
            // If appreciable amount of commands, shove the work off immediately.
            if (commands.Length > 64)
                JobHandle.ScheduleBatchedJobs();
            
            MarkUnsafeSetEnd();
        }

        /// <summary> Work must be completed with this method to satisfy the internal safety system for reuse purposes. </summary>
        public void CompleteJob()
        {
            if (!jobActive)
                return;

            MarkUnsafeSetBegin();
            CheckUnsafeSetSingleOp();
            
            jobHandle.Complete();
            jobHandle = default;
            jobActive = false;
            
            MarkUnsafeSetEnd();
        }
        
        public ref RaycastHit GetFirstHit(int commandIndex) => ref hits.ElementAt(commandIndex * hitsPerRay);

        /// <summary> Allows you to walk all relevant hits for the given command index without fussing with the math. </summary>
        public HitIterator GetHitIterator(int commandIndex) => new HitIterator(hits, commandIndex * hitsPerRay, hitsPerRay);

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckCommandLengthNoResize()
        {
            if (unsafeCommands->Length > unsafeCommands->Capacity)
                throw new System.Exception("Command list is full. Use AddCommandResize instead.");
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void MarkUnsafeSetBegin() => Interlocked.Increment(ref unsafeSetterLock);
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void MarkUnsafeSetEnd() => Interlocked.Decrement(ref unsafeSetterLock);
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckUnsafeSetSingleOp()
        {
            if (unsafeSetterLock > 1)
                throw new System.Exception("Multiple unsafe sets in progress at same time!");
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckJobInactive()
        {
            if (jobActive)
                throw new System.Exception("Job is active, cannot modify commands.");
        }

        public struct HitIterator
        {
            NativeList<RaycastHit> hitsBuffer;
            int hitEnd;
            int currentHitIndex;

            public HitIterator(NativeList<RaycastHit> hitsBuffer, int hitStart, int hitCount)
            {
                if (!hitsBuffer.IsCreated)
                    throw new System.ArgumentException("Hits buffer is not created.");
                
                this.hitsBuffer = hitsBuffer;
                hitEnd = hitStart + hitCount;
                currentHitIndex = hitStart;
            }

            public bool MoveNext() => currentHitIndex < hitEnd;
            
            public ref RaycastHit CurrentRef => ref hitsBuffer.ElementAt(currentHitIndex);
        }
    }
}