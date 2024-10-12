#if UNITY_EDITOR
#define UNSAFE_SET_TRACKING
//#define ALWAYS_REPORT_HITBUFFER_SIZE
#endif

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using VLib.Safety;
using Debug = UnityEngine.Debug;

namespace VLib.Physics
{
    /// <summary> A low level structure to facilitate setting up a batch of raycast commands, potentially from multiple parallel jobs. </summary>
    public unsafe struct BatchRaycastCommands
    {
        const int MaxHitsPerRayBufferSize = 999999;
        
        NativeList<RaycastCommand> commands;
        UnsafeList<RaycastCommand>* unsafeCommands;
        int hitsPerRay;
        NativeList<RaycastHit> hits;
        UnsafeList<RaycastHit>* unsafeHits;

        [MarshalAs(UnmanagedType.U1)]
        bool jobActive;
        JobHandle jobHandle;
        
        // Editor safety
        BurstSingleOpEnforcer opEnforcer;

        /// <summary> Setting this property is not concurrent-safe </summary>
        public int CommandCapacity
        {
            get => commands.Capacity;
            /*set
            {
                CheckJobInactive();
                MarkUnsafeSetBegin();
                CheckUnsafeSetSingleOp();
                commands.Capacity = value;
                hits.Capacity = value * hitsPerRay;
                MarkUnsafeSetEnd();
            }*/
        }

        /// <summary> Setting this property is not concurrent-safe </summary>
        public int HitsPerRay
        {
            get => hitsPerRay;
            set
            {
                CheckJobInactive();
                opEnforcer.StartOp();
                hitsPerRay = value;
                opEnforcer.CompleteOp();
            }
        }
        
        public int HitCapacity => unsafeHits != null ? unsafeHits->Capacity : 0;
        
        public int Length => commands.Length;
        
        public bool JobActive => jobActive;

        public BatchRaycastCommands(int maxCommands, int maxHitsPerRay, Allocator allocator = Allocator.Persistent)
        {
            hitsPerRay = maxHitsPerRay;
            commands = new NativeList<RaycastCommand>(maxCommands, allocator);
            hits = new NativeList<RaycastHit>(4, allocator);

            unsafeCommands = commands.GetUnsafeList();
            unsafeHits = hits.GetUnsafeList();
      
            jobActive = false;
            jobHandle = default;
            
            opEnforcer = BurstSingleOpEnforcer.Create();
        }

        public void Dispose()
        {
            CheckJobInactive();
            opEnforcer.StartOp();
            unsafeCommands = null;
            commands.Dispose();
            hits.Dispose();
            opEnforcer.CompleteOp();
        }

        /// <summary> Non-concurrent (faster) add, can resize (slower) </summary>
        public int AddCommand(in RaycastCommand command)
        {
            CheckJobInactive();
            opEnforcer.StartOp();
            var lengthBeforeAdd = commands.Length;
            commands.Add(command);
            opEnforcer.CompleteOp();
            return lengthBeforeAdd;
        }

        /// <summary> Non-concurrent (faster) add, non-resizing (faster). </summary>
        public int AddCommandNoResize(in RaycastCommand command)
        {
            CheckJobInactive();
            opEnforcer.StartOp();
            var commandIndex = unsafeCommands->Length;
            commands.AddNoResize(command);
            opEnforcer.CompleteOp();
            return commandIndex;
        }

        /// <summary> Concurrent-safe (slower), but cannot resize (faster). </summary>
        public int AddCommandNoResizeConcurrent(in RaycastCommand command)
        {
            CheckJobInactive();
            opEnforcer.StartOp();
            var commandIndex = Interlocked.Increment(ref unsafeCommands->m_length) - 1;
            // Editor-only check for pushing the length past the capacity. Bumping the length directly is very fast, but it must not exceed the capacity.
            CheckCommandLengthNoResize();
            (*unsafeCommands)[commandIndex] = command;
            opEnforcer.CompleteOp();
            return commandIndex;
        }

        /// <summary> Thread-safe </summary>
        public void Clear()
        {
            CheckJobInactive();
            opEnforcer.StartOp();
            commands.Clear();
            hits.Clear();
            opEnforcer.CompleteOp();
        }

        /// <summary> Schedules the job, this method may only be called on the main thread. <br/>
        /// The output jobhandle is NOT to be completed, it is only passed out for dependency purposes. <br/>
        /// You must call <see cref="CompleteJob"/>. </summary>
        public void ScheduleJob(JobHandle inDeps, out JobHandle outDeps)
        {
            CheckJobInactive();
            
            // Prep data
            // Ensure hits buffer is big enough
            
            EnsureHitBufferLength();
            
            opEnforcer.StartOp();
            
            jobActive = true;
            outDeps = jobHandle = RaycastCommand.ScheduleBatch(commands.AsArray(), hits.AsArray(), 16, HitsPerRay, inDeps);
            
            // If appreciable amount of commands, shove the work off immediately.
            if (commands.Length > 64)
                JobHandle.ScheduleBatchedJobs();
            
            opEnforcer.CompleteOp();
        }

        /// <summary> Work must be completed with this method to satisfy the internal safety system for reuse purposes. </summary>
        public void CompleteJob()
        {
            if (!jobActive)
                return;

            opEnforcer.StartOp();
            
            jobHandle.Complete();
            jobHandle = default;
            jobActive = false;
            
            opEnforcer.CompleteOp();
        }

        public void EnsureHitBufferLength()
        {
            CheckJobInactive();
            opEnforcer.StartOp();
            
            ReportHitBufferSize(commands.Length);
            
            if (hits.IsCreated)
                hits.Length = commands.Length * hitsPerRay; // Use only the amount of memory needed
            opEnforcer.CompleteOp();
        }
        
        public ref RaycastHit GetFirstHit(int commandIndex) => ref hits.ElementAt(commandIndex * hitsPerRay);

        /// <summary> Allows you to walk all relevant hits for the given command index without fussing with the math. Call MoveNext before trying to call current. </summary>
        public HitIterator GetHitIterator(int commandIndex)
        {
            CheckHitsAllocated();
            return new HitIterator(hits, commandIndex * hitsPerRay, hitsPerRay);
        }

        public bool TryGetHitIterator(int commandIndex, out HitIterator iterator)
        {
            CheckHitsAllocated();
            var hitStartIndex = commandIndex * hitsPerRay;
            var hitEndIndex = hitStartIndex + hitsPerRay;
            if (hitStartIndex < 0 || hitEndIndex > unsafeHits->Length)
            {
                iterator = default;
                return false;
            }
            iterator = new HitIterator(hits, commandIndex * hitsPerRay, hitsPerRay);
            return true;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckCommandLengthNoResize()
        {
            if (unsafeCommands->Length > unsafeCommands->Capacity)
                throw new System.Exception("Command list is full. Use AddCommandResize instead.");
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckJobInactive()
        {
            if (jobActive)
                throw new System.Exception("Job is active, cannot modify commands.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckHitsAllocated()
        {
            if (!unsafeHits->IsCreated)
                throw new System.Exception("Hits buffer is not created.");
        }
        
        void ReportHitBufferSize(int commandCount)
        {
#if ALWAYS_REPORT_HITBUFFER_SIZE
            Debug.Log($"Hits buffer size: {commandCount * hitsPerRay}");
#else
            if (commandCount * hitsPerRay > MaxHitsPerRayBufferSize)
                Debug.LogError($"Hits buffer size: {commandCount * hitsPerRay}");
#endif
        }

        public struct HitIterator
        {
            UnsafeList<RaycastHit>* hitsBuffer;
            int hitStart;
            int hitEnd;
            int currentHitIndex;

            public HitIterator(NativeList<RaycastHit> hitsBuffer, int hitStart, int hitCount)
            {
                if (!hitsBuffer.IsCreated)
                    throw new System.ArgumentException("Hits buffer is not created.");
                
                this.hitsBuffer = hitsBuffer.GetUnsafeList();
                this.hitStart = hitStart;
                hitEnd = hitStart + hitCount;
                currentHitIndex = hitStart - 1;
                
                CheckRangeAgainstCollection(hitStart, hitCount);
            }

            public bool MoveNext() => ++currentHitIndex < hitEnd;
            
            public ref RaycastHit CurrentRef
            {
                get
                {
                    CheckCurrentHitIndex();
                    return ref hitsBuffer->ElementAt(currentHitIndex);
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            public void CheckCurrentHitIndex()
            {
                if (currentHitIndex < hitStart || currentHitIndex >= hitEnd)
                    throw new System.InvalidOperationException($"Index out of range: {currentHitIndex}, Range: {hitStart} - {hitEnd - 1}");
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            public void CheckIndexAgainstCollection(int index)
            {
                if (index < 0 || index >= hitsBuffer->Length)
                    throw new System.InvalidOperationException($"Index out of range: {index}, Range: {hitStart} - {hitEnd - 1}");
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            public void CheckRangeAgainstCollection(int start, int count)
            {
                if (start < 0)
                    throw new System.InvalidOperationException($"Start index out of range: {start}");
                if (start + count > hitsBuffer->Length)
                    throw new System.InvalidOperationException($"End index '{start + count}' above buffer length '{hitsBuffer->Length}'");
            }
        }
    }
}