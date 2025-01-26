using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace VLib
{
    /// <summary> A separate class to handle managed events for the burst-compatible <see cref="VirtualValueTransformTree"/> </summary>
    public static class VirtualValueTransformTreeEvents
    {
        // Editor maintenance
#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void OnInitInEditor() => handlers.Clear();
#endif
        
        static ConcurrentDictionary<ulong, Handler> handlers = new();
        
        public static Handler GetHandler(in VirtualValueTransformTree tree)
        {
            var treeID = tree.InternalData.SafetyID;
            if (!handlers.TryGetValue(treeID, out var handler))
            {
                handler = new Handler(treeID);
                handlers.TryAdd(treeID, handler);
            }
            return handler;
        }
        
        public static void InvokePreDispose(in VirtualValueTransformTree tree)
        {
            if (handlers.TryGetValue(tree.InternalData.SafetyID, out var handler))
                handler.Invoke_OnPreDispose(tree);
        }

        public static void DisposeHandler(in VirtualValueTransformTree tree) => handlers.TryRemove(tree.InternalData.SafetyID, out _);

        public class Handler
        {
            /// <summary> Derived from the <see cref="VirtualValueTransformTree.data"/> type. </summary>
            public readonly ulong treeID;

            public Handler(ulong id) => treeID = id;

            /// <summary> Leverage this event to return guarded access or to do disposal work before the tree comes down. </summary>
            public event Action<VirtualValueTransformTree> OnPreDispose;
            
            public void Subscribe_OnPreDispose(Action<VirtualValueTransformTree> action) => OnPreDispose += action;
            public void Unsubscribe_OnPreDispose(Action<VirtualValueTransformTree> action) => OnPreDispose -= action;
            
            public void Invoke_OnPreDispose(in VirtualValueTransformTree tree)
            {
                BurstAssert.True(tree.InternalData.SafetyID == treeID);
                OnPreDispose?.Invoke(tree);
            }
        }
    }
}