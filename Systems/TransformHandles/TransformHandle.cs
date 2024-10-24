// A potential system for working with wrapped transform IDs.

/*using System.Collections.Generic;
using System.Diagnostics;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using VLib.Threading;

namespace VLib.Systems.TransformHandles
{
    /// <summary> A burst-compatible wrapper for transforms </summary>
    public struct TransformHandle
    {
        PinnedMemoryElement<TransformHandleInternal> internalMemory;
        int transformID;

        public bool IsValid => internalMemory.IsCreated && internalMemory.Ref.transformID == transformID;
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void CheckValid()
        {
            if (!IsValid)
                throw new System.InvalidOperationException("TransformHandle is not valid!");
        }
        
        internal TransformHandle(PinnedMemoryElement<TransformHandleInternal> internalMemory, int transformID)
        {
            this.internalMemory = internalMemory;
            this.transformID = transformID;
        }
    }

    internal struct TransformHandleInternal
    {
        internal readonly VUnsafeKeyedRef<TransformSystemInternal> native;
        internal readonly int transformID;
        
        internal bool outOfDate;
        internal AffineTransform localTransformation; // TODO: Support transform chains??
    }

    /// <summary> Core system required to manage <see cref="TransformHandle"/>s. </summary>
    public class TransformHandleSystem
    {
        static TransformHandleSystem instance;
        static TransformHandleSystem Instance => instance ?? throw new System.InvalidOperationException("TransformHandleSystem is not initialized!");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        static void StaticInit()
        {
            #if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= EditorCleanup;
            EditorApplication.playModeStateChanged += EditorCleanup;
            #endif
        }

#if UNITY_EDITOR
        static void EditorCleanup(PlayModeStateChange stateChange)
        {
            if (stateChange == PlayModeStateChange.EnteredEditMode)
            {
                if (instance.IsCreated)
                    instance.Dispose();
                instance = null;
            }
        }
#endif
        
        const int MaxBaseListSize = 8192;
        const int MaxSubListSize = 8192;
        const int MaxTransforms = MaxBaseListSize * MaxSubListSize;

        IMonoBehaviourUpdatesHook updater;
        VUnsafeParallelPinnedMemory<TransformHandleInternal> internalMemorySlots;

        VUnsafeRef<int> transformMapLock;
        Dictionary<int, Transform> registeredTransforms = new();

        public bool IsCreated => updater != null && internalMemorySlots.IsCreated;
        
        public TransformHandleSystem(IMonoBehaviourUpdatesHook updater)
        {
            Assert.IsNull(instance);
            Assert.IsNotNull(updater);
            
            instance = this;
            this.updater = updater;
            internalMemorySlots = new VUnsafeParallelPinnedMemory<TransformHandleInternal>(MaxBaseListSize, MaxSubListSize);
        }

        public void Dispose()
        {
            
        }
        
        public static void OnUpdate()
        {
            
        }

        public static void OnLateUpdate()
        {
            
        }
        
        /// <summary> Concurrent safe </summary>
        public static bool TryGetTransformHandle(int transformID, out TransformHandle handle)
        {
            
        }

        /// <summary> Main thread only! </summary>
        public static TransformHandle GetCreateTransformHandle(int transformID)
        {
            MainThread.AssertMainThreadConditional();
        }
    }

    internal struct TransformSystemInternal
    {
        
    }
}*/