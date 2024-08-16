#if UNITY_EDITOR
//#define PROFILING
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
            
            // Release the lock
            if (isWrite)
                rwLock.internalLock.ExitWriteLock();
            else
                rwLock.internalLock.ExitReadLock();
        }
    }
}