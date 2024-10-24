#if UNITY_EDITOR
//#define STACKTRACE_CLAIM_TRACKING // REQUIRES BURST OFF
#endif

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Unity.Burst;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace VLib
{
    public class VSafetyHandleManager
    {
        /// <summary> The number of safety lists. The root list holding the other lists will be allocated up front. </summary>
        const int SafetyListCount = 8192;
        /// <summary> The size of each safety list. Allocations happen in large chunks for efficiency. New chunk allocations are done only as needed. </summary>
        const int SafetyListSize = 32768;
        const long MinimumMemoryCostBytes = SafetyListCount * 24 + SafetyListSize * sizeof(ulong);
        /// <summary> The absolute maximum that can be supported. This amount is not allocated up front! </summary>
        const int MaximumActiveSafetyHandles = SafetyListCount * SafetyListSize;

        public static readonly SharedStatic<Internal> InternalMemoryField = SharedStatic<Internal>.GetOrCreate<VSafetyHandleManager, Internal>();

        public struct Internal
        {
            long idBasis;
            VUnsafeParallelPinnedMemory<ulong> safetyMemory;
            long takenHandles;
            
            public bool IsCreated => safetyMemory.IsCreated;
            public long TakenHandles => takenHandles;

            internal void Initialize()
            {
                Dispose();
                safetyMemory = new VUnsafeParallelPinnedMemory<ulong>(SafetyListCount, SafetyListSize);
                idBasis = -long.MaxValue;
                takenHandles = 0;
            }

            internal void Dispose()
            {
                if (takenHandles != 0)
                    Debug.LogError($"VSafetyHandleManager is being disposed with {takenHandles} active handles!");
                if (safetyMemory.IsCreated)
                    safetyMemory.Dispose();
                takenHandles = 0;
                
#if STACKTRACE_CLAIM_TRACKING
                LogAllTraces();
                ClearAllTraces();
#endif
            }

            internal VSafetyHandle Create()
            {
                CheckCreated();
                var pinnedMemory = safetyMemory.GetPinnedAddress();
                pinnedMemory.Value = GetUniqueID();
                Interlocked.Increment(ref takenHandles);
                
#if STACKTRACE_CLAIM_TRACKING
                TrackStackTrace(pinnedMemory.Value);
#endif
                return new VSafetyHandle(pinnedMemory);
            }
            
            internal bool TryDestroy(VSafetyHandle handle)
            {
                if (!handle.IsValid)
                    return false;
                
#if STACKTRACE_CLAIM_TRACKING
                UntrackStackTrace(handle.safetyIDCopy);
#endif
                Interlocked.Decrement(ref takenHandles);
                safetyMemory.ReturnAddress(handle.truthLocation);
                return true;
            }

            ulong GetUniqueID() => idBasis.IncrementToUlong();

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
            public void CheckCreated()
            {
                if (!IsCreated)
                    throw new System.InvalidOperationException("VSafetyHandleManager is not created!");
            }
        }
        
        // In the editor and the start of the game, ensure we are setting everything up
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        static void Init()
        {
            StackTraceClaimTrackingInit();
            InternalMemoryField.Data.Initialize();
            
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= EditorCleanup;
            EditorApplication.playModeStateChanged += EditorCleanup;
#endif
        }

#if UNITY_EDITOR
        // Let the memory leak outside the editor, Unity should give all that memory back to the OS anyway.
        static void EditorCleanup(PlayModeStateChange playModeStateChange)
        {
            if (playModeStateChange is PlayModeStateChange.EnteredEditMode)
                InternalMemoryField.Data.Dispose();
        }
#endif
        

        [BurstDiscard]
        static void StackTraceClaimTrackingInit()
        {
#if STACKTRACE_CLAIM_TRACKING
            traceDict = new();
#endif
        }
        
#if STACKTRACE_CLAIM_TRACKING

        static readonly object tracerLock = new();
        static Dictionary<ulong, StackTrace> traceDict = new();

        [BurstDiscard]
        static void TrackStackTrace(ulong handleID)
        {
            var trace = new StackTrace(1, true);
            lock (tracerLock)
            {
                if (!traceDict.TryAdd(handleID, trace))
                    throw new UnityException($"Handle ID {handleID} already has a trace!");
            }
        }

        [BurstDiscard]
        static void UntrackStackTrace(ulong handleID)
        {
            lock (tracerLock)
            {
                if (!traceDict.Remove(handleID))
                    throw new UnityException($"Handle ID {handleID} does not have a trace!");
            }
        }
        
        static void LogAllTraces()
        {
            lock (tracerLock)
            {
                foreach (var kvp in traceDict)
                { 
                    Debug.LogError($"VSafetyHandle-{kvp.Key} still active...");
                    Debug.LogError($"VSafetyHandle (ID logged separately) still active, stack trace: \n {kvp.Value}");
                }
            }
        }

        static void ClearAllTraces()
        {
            lock (tracerLock)
                traceDict.Clear();
        }
#endif
    }
}