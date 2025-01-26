#if UNITY_EDITOR
//#define SAFETY_TRACKING
#endif

using System;
using System.Diagnostics;
using Unity.Burst;

namespace VLib
{
    [DebuggerDisplay("IsWrite: {isWrite}, ID: {wrapperID}")]
    public readonly struct VRWLockHoldScoped : IDisposable
    {
        readonly VReaderWriterLockSlim rwLock;
        public readonly bool isWrite;
#if SAFETY_TRACKING
        internal readonly long wrapperID;
#endif
        
        public bool IsCreated => rwLock != null;
        public static implicit operator bool(VRWLockHoldScoped lockScope) => lockScope.IsCreated;

        public bool IsLockedForRead => IsCreated && rwLock.internalLock.IsReadLockHeld;
        public bool IsLockedForWrite => IsCreated && rwLock.internalLock.IsWriteLockHeld;
        public bool IsLockedAny => IsLockedForRead || IsLockedForWrite;

        internal VRWLockHoldScoped(VReaderWriterLockSlim rwLock, bool isWrite
#if SAFETY_TRACKING
            , long wrapperID
#endif
        )
        {
            this.rwLock = rwLock;
            this.isWrite = isWrite;
            
            // Is it valid for recursive lock to enter read lock while write lock is held
            //BurstAssert.True(this.isWrite == rwLock.internalLock.IsWriteLockHeld); // Lock state mismatch check
            
#if SAFETY_TRACKING
            this.wrapperID = wrapperID;
#endif
        }

        [BurstDiscard]
        public void Dispose()
        {
            if (rwLock == null)
                return;
#if SAFETY_TRACKING
            if (!rwLock.activeIDTracking.TryRemove(wrapperID, out _))
                throw new InvalidOperationException($"{(isWrite ? "Write" : "Read")} scope lock is not active and cannot be invalidated twice! (This is an editor-only safety feature)");
#endif
            try
            {
                // Release the lock
                if (isWrite)
                    rwLock.internalLock.ExitWriteLock();
                else
                    rwLock.internalLock.ExitReadLock();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to release {(isWrite ? "write" : "read")} lock! Logging exception...");
                UnityEngine.Debug.LogException(e);
            }
        }
    }
}