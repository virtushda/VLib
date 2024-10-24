﻿#if UNITY_EDITOR
#define CLAIM_TRACKING // Burst-compatible, lightweight linenumber hints about leaking refstructs
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;

namespace VLib
{
    /// <summary> This structure allocates a <see cref="VUnsafeRef"/> and uses the <see cref="VSafetyHandle"/> to ensure that disposal is recognized by all copies. <br/>
    /// This can only be used in play mode, but is able to act like a "native class object". </summary>
    public unsafe struct RefStruct<T> : IDisposable, IEquatable<RefStruct<T>>, IComparable<RefStruct<T>>
        where T : unmanaged
    {
        // Data
        VUnsafeRef<T> refData;
        // Security
        readonly VSafetyHandle safetyHandle;

        public ulong SafetyID => safetyHandle.safetyIDCopy;
        public bool IsCreated => safetyHandle.IsValid;
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void ConditionalCheckValid() => safetyHandle.ConditionalCheckValid();
        
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
        
        RefStruct(T value, Allocator allocator = Allocator.Persistent)
        {
            refData = new VUnsafeRef<T>(value, allocator);
            safetyHandle = VSafetyHandle.Create();
        }

        /// <summary> Dispose with the instance's <see cref="Dispose"/> method. </summary>
        public static RefStruct<T> Create(T value = default, Allocator allocator = Allocator.Persistent
#if CLAIM_TRACKING
            , [CallerLineNumber] int callerLine = -1
#endif
            )
        {
            RefStruct<T> refStruct = new RefStruct<T>(value, allocator);
#if CLAIM_TRACKING
            RefStructTracker.Track(refStruct.safetyHandle.safetyIDCopy, callerLine);
#endif
            return refStruct;
        }

        public void Dispose()
        {
            if (VSafetyHandle.TryDispose(safetyHandle))
            {
                refData.Dispose();
#if CLAIM_TRACKING
                RefStructTracker.Untrack(safetyHandle.safetyIDCopy);
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

        public override string ToString() => $"RefStruct|{safetyHandle} of type {typeof(T)}";

        public bool Equals(RefStruct<T> other) => /*refData.Equals(other.refData) && */safetyHandle.Equals(other.safetyHandle);
        public override bool Equals(object obj) => obj is RefStruct<T> other && Equals(other);
        
        public static bool operator ==(RefStruct<T> left, RefStruct<T> right) => left.Equals(right);
        public static bool operator !=(RefStruct<T> left, RefStruct<T> right) => !left.Equals(right);

        public override int GetHashCode() => safetyHandle.GetHashCode();

        public int CompareTo(RefStruct<T> other)
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

    public static class RefStructExt
    {
        /// <summary> Disposes internal IDisposable, sets internal value reference to 'default', disposes the refstruct. Does not set the refstruct reference itself to default, no access on a copy. </summary>
        public static void DisposeSelfAndDefaultInternalRef<T>(this RefStruct<T> refStruct) 
            where T : unmanaged, IDisposable
        {
            if (refStruct.IsCreated)
            {
                // INTERNAL VALUE
                ref var internalValueRef = ref refStruct.TryGetRef(out var success);
                if (success)
                {
                    try
                    {
                        internalValueRef.Dispose();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to dispose internal value! {e}");
                    }
                    internalValueRef = default;
                }
                else
                    Debug.LogError("Failed to get value from ref struct!");
                
                // REFSTRUCT ITSELF
                refStruct.Dispose();
            }
        }

        /// <summary> Disposes internal IDisposable, sets internal value reference to 'default', disposes the refstruct, sets the reference to the refstruct itself to 'default'. </summary>
        public static void DisposeFullToDefault<T>(ref this RefStruct<T> refStruct) 
            where T : unmanaged, IDisposable
        {
            refStruct.DisposeSelfAndDefaultInternalRef();
            refStruct = default;
        }
    }

#if CLAIM_TRACKING
    public class RefStructTracker
    {
        internal static readonly SharedStatic<Internal> InternalStatic = SharedStatic<Internal>.GetOrCreate<RefStructTracker, Internal>();

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
                    Debug.LogError($"RefStruct with handle ID {kvp.Key} was not disposed! Created at line {kvp.Value}. Enable 'STACKTRACE_CLAIM_TRACKING' in VSafetyHandleManager.cs for a deeper trace.");
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