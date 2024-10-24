using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Unity.Burst;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace VLib.Jobs
{
    /// <summary> To use this see: <see cref="JobObjectRef{T}"/> </summary>
    public static class JobObjectReferencesHolder
    {
        internal static ConcurrentDictionary<ulong, object> ReferenceMap { get; private set; }

        [RuntimeInitializeOnLoadMethod]
        static void Initialize()
        {
            ReferenceMap = new ConcurrentDictionary<ulong, object>();
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= OnReturnToEditorMode;
            EditorApplication.playModeStateChanged += OnReturnToEditorMode;
#endif
        }

#if UNITY_EDITOR
        static void OnReturnToEditorMode(PlayModeStateChange stateChange)
        {
            if (stateChange != PlayModeStateChange.EnteredEditMode)
                return;
            
            Debug.Log("Reentering edit mode... (from JobObjectReferencesHolder)");
            if (!ReferenceMap.IsEmpty)
            {
                Debug.LogError("JobObjectReferencesHolder has references that were not disposed from the last play session!");
                foreach (var kvp in ReferenceMap)
                    Debug.LogError($"Reference ID: {kvp.Key}, Object: {kvp.Value}");
            }
        }
#endif
    }

    /// <summary> A way to refer to a managed type that will pass through the job barrier. <br/>
    /// Is value type, but not designed to be used from inside burst. You can check validity and read the ID from burst though. </summary>
    public struct JobObjectRef<T> : IEquatable<JobObjectRef<T>>, IDisposable
        where T : class
    {
        VSafetyHandle nativeHandle;

        /// <summary> Burst compatible </summary>
        public bool ConstructedWithNull { get; }
        
        /// <summary> Burst compatible </summary>
        public bool IsValid => nativeHandle.IsValid;

        /// <summary> Burst compatible </summary>
        public ulong ID => nativeHandle.safetyIDCopy;

        /// <summary> Not burst compatible </summary>
        public JobObjectRef(T obj)
        {
            ConstructedWithNull = obj == null;
            nativeHandle = VSafetyHandle.Create();
            if (!ConstructedWithNull)
            {
                if (!JobObjectReferencesHolder.ReferenceMap.TryAdd(nativeHandle.safetyIDCopy, obj))
                    Debug.LogError($"Failed to add reference to JobObjectReferencesHolder for object of type {typeof(T)}");
            }
        }

        /// <summary> Not burst compatible </summary>
        public void Dispose()
        {
            if (nativeHandle.TryDispose())
            {
                if (!ConstructedWithNull)
                {
                    if (!JobObjectReferencesHolder.ReferenceMap.TryRemove(nativeHandle.safetyIDCopy, out _))
                        Debug.LogError($"Failed to remove reference from JobObjectReferencesHolder for object of type {typeof(T)}");
                }
            }
        }

        /// <summary> Not burst compatible </summary>
        public bool TryGet(out T obj)
        {
            if (IsValid)
            {
                // If we passed in a null object, we can validly get it out, there are certain cases where this is valid behaviour.
                // The only job of this struct is to ferry the input through to the job.
                if (ConstructedWithNull)
                {
                    obj = null;
                    return true;
                }
                if (!JobObjectReferencesHolder.ReferenceMap.TryGetValue(ID, out var objRef))
                {
                    Debug.LogError($"IsValid == true! BUT: failed to get reference from JobObjectReferencesHolder for object of type {typeof(T)} using ID: {ID}");
                    obj = null;
                    return false;
                }
                if (objRef is T objOfType)
                {
                    obj = objOfType;
                    return true;
                }
            }
            obj = null;
            return false;
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void ConditionalCheckFullyValidAndNotNull()
        {
            if (!IsValid)
                throw new InvalidOperationException("JobObjectRef is not created!");
            if (!TryGet(out var objRef) || objRef == null)
                throw new InvalidOperationException("JobObjectRef is not pointing to a valid object!");
        }

        public bool Equals(JobObjectRef<T> other) => nativeHandle.Equals(other.nativeHandle);
        public override bool Equals(object obj) => obj is JobObjectRef<T> other && Equals(other);

        public override int GetHashCode() => nativeHandle.GetHashCode();

        public static bool operator ==(JobObjectRef<T> left, JobObjectRef<T> right) => left.Equals(right);
        public static bool operator !=(JobObjectRef<T> left, JobObjectRef<T> right) => !left.Equals(right);
    }
}