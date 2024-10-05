#if UNITY_EDITOR
#define CLAIM_TRACKING
#endif

using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace VLib
{
    /// <summary> This structure allocates a <see cref="VUnsafeRef"/> and uses the <see cref="VSafetyHandle"/> to ensure that disposal is recognized by all copies. <br/>
    /// This can only be used in play mode! </summary>
    public unsafe struct RefStructV2<T> : IDisposable, IEquatable<RefStructV2<T>>, IComparable<RefStructV2<T>>
        where T : unmanaged
    {
        // Data
        VUnsafeRef<T> refData;
        // Security
        readonly VSafetyHandle safetyHandle;

        public bool IsCreated => safetyHandle.IsValid && refData.IsCreated;
        
        public T ValueCopy
        {
            get
            {
                safetyHandle.ConditionalCheckValid();
                return refData.ValueRef;
            }
            set
            {
                safetyHandle.ConditionalCheckValid();
                refData.ValueRef = value;
            }
        }

        public ref T ValueRef
        {
            get
            {
                safetyHandle.ConditionalCheckValid();
                return ref refData.ValueRef;
            }
        }

        public T* ValuePtr
        {
            get
            {
                safetyHandle.ConditionalCheckValid();
                return refData.TPtr;
            }
        }

        /*uniqueID > 0 && // Valid ID
        refData != null && // Non-null data ptr
        dataPtrCopy != null && // Data ptr copy is pointing at something
        refData->TPtr == dataPtrCopy && // Actual data ptr is the same as when this struct was created
        refData->ValueRef.uniqueID == uniqueID; // IDs match in both locations*/

        public RefStructV2(Allocator allocator, T value = default
#if CLAIM_TRACKING
            , [CallerLineNumber] int callerLine = -1
#endif
            )
        {
            refData = new VUnsafeRef<T>(value, allocator);
            safetyHandle = VSafetyHandle.Create();
            
#if CLAIM_TRACKING
            RefStructV2Tracker.Track(safetyHandle.safetyIDCopy, callerLine);
#endif
        }

        public void Dispose()
        {
            if (VSafetyHandle.TryDispose(safetyHandle))
            {
                refData.Dispose();
#if CLAIM_TRACKING
                RefStructV2Tracker.Untrack(safetyHandle.safetyIDCopy);
#endif
            }
        }
        
        public bool TryGetValue(out T value)
        {
            if (!IsCreated)
            {
                value = default;
                return false;
            }

            value = refData.ValueRef;
            return true;
        }
        
        public ref T TryGetRef(out bool success)
        {
            if (!IsCreated)
            {
                success = false;
                return ref UnsafeUtility.AsRef<T>(null);
            }

            success = true;
            return ref refData.ValueRef;
        }
        
        public bool TryGetPtr(out T* ptr)
        {
            if (!IsCreated)
            {
                ptr = null;
                return false;
            }

            ptr = refData.TPtr;
            return true;
        }

        public override string ToString() => $"RefStructV2: {refData} - {safetyHandle}";

        public bool Equals(RefStructV2<T> other) => refData.Equals(other.refData) && safetyHandle.Equals(other.safetyHandle);
        public override bool Equals(object obj) => obj is RefStructV2<T> other && Equals(other);

        public override int GetHashCode() => safetyHandle.GetHashCode();

        public int CompareTo(RefStructV2<T> other)
        {
            bool isCreated = IsCreated;
            bool otherIsCreated = other.IsCreated;
            if (!isCreated || !otherIsCreated)
            {
                if (isCreated)
                    return 1;
                if (otherIsCreated)
                    return -1;
                return 0;
            }
            return safetyHandle.CompareTo(other.safetyHandle);
        }
    }

#if CLAIM_TRACKING
    public class RefStructV2Tracker
    {
        internal static readonly SharedStatic<Internal> InternalStatic = SharedStatic<Internal>.GetOrCreate<RefStructV2Tracker, Internal>();

        internal struct Internal
        {
            UnsafeParallelHashMap<ulong, int> handleIDToLineNumber;
            VUnsafeRef<int> locker;

            public bool IsCreated => handleIDToLineNumber.IsCreated;
            
            internal void Initialize()
            {
                Dispose();
                handleIDToLineNumber = new UnsafeParallelHashMap<ulong, int>(64, Allocator.Persistent);
                locker = new VUnsafeRef<int>(0, Allocator.Persistent);
            }
            
            internal void Dispose()
            {
                if (!IsCreated)
                    return;
                using (locker.ScopedAtomicLock())
                {
                    YellAboutLeaks();
                    handleIDToLineNumber.Dispose();
                }
                locker.Dispose();
            }
            
            internal void AddHandle(ulong handleID, int lineNumber)
            {
                using (locker.ScopedAtomicLock())
                {
                    if (!handleIDToLineNumber.TryAdd(handleID, lineNumber))
                        Debug.LogError($"Failed to add handle ID {handleID} at line {lineNumber}!");
                }
            }
            
            internal void RemoveHandle(ulong handleID)
            {
                using (locker.ScopedAtomicLock())
                {
                    if (!handleIDToLineNumber.Remove(handleID))
                        Debug.LogError($"Failed to remove handle ID {handleID}!");
                }
            }

            void YellAboutLeaks()
            {
                if (!IsCreated)
                {
                    Debug.LogError("Cannot yell about leaks... RefStructV2Tracker is not created!");
                    return;
                }

                if (handleIDToLineNumber.IsEmpty)
                    return;
                
                foreach (var kvp in handleIDToLineNumber)
                    Debug.LogError($"RefStructV2 with handle ID {kvp.Key} was not disposed! Created at line {kvp.Value}");
            }
        }
            
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        static unsafe void StaticInit()
        {
            Assert.IsTrue(InternalStatic.UnsafeDataPointer != null);
            InternalStatic.Data.Dispose();
            InternalStatic.Data.Initialize();
            
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= StaticDispose;
            EditorApplication.playModeStateChanged += StaticDispose;
#endif
        }

#if UNITY_EDITOR
        static void StaticDispose(PlayModeStateChange playModeStateChange)
        {
            if (playModeStateChange == PlayModeStateChange.EnteredEditMode)
                InternalStatic.Data.Dispose();
        }
#endif
        
        public static void Track(ulong handleID, int lineNumber) => InternalStatic.Data.AddHandle(handleID, lineNumber);
        
        public static void Untrack(ulong handleID) => InternalStatic.Data.RemoveHandle(handleID);
    }
#endif
}