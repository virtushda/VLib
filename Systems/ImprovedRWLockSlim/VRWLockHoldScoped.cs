#if UNITY_EDITOR
//#define SAFETY_TRACKING
#endif

using System;
using System.Diagnostics;

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

        public bool IsLockedForRead => rwLock.internalLock.IsReadLockHeld;
        public bool IsLockedForWrite => rwLock.internalLock.IsWriteLockHeld;
        public bool IsLockedAny => IsLockedForRead || IsLockedForWrite;

        public VRWLockHoldScoped(VReaderWriterLockSlim rwLock, bool isWrite
#if SAFETY_TRACKING
            , long wrapperID
#endif
        )
        {
            this.rwLock = rwLock;
            this.isWrite = isWrite;
#if SAFETY_TRACKING
            this.wrapperID = wrapperID;
#endif
        }

        public void Dispose()
        {
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