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
            }

            internal VSafetyHandle Create()
            {
                CheckCreated();
                var pinnedMemory = safetyMemory.GetPinnedAddress();
                pinnedMemory.Value = GetUniqueID();
                Interlocked.Increment(ref takenHandles);
                return new VSafetyHandle(pinnedMemory);
            }
            
            internal bool TryDestroy(VSafetyHandle handle)
            {
                if (!handle.IsValid)
                    return false;
                safetyMemory.ReturnAddress(handle.truthLocation);
                Interlocked.Decrement(ref takenHandles);
                return true;
            }

            ulong GetUniqueID() => idBasis.IncrementToUlong();

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
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
    }
}