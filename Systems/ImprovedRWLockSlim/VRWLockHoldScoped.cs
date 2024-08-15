//#define EXPOSE_MULTIPLE_DISPOSE

using System;
using System.Diagnostics;
using UnityEngine;

namespace VLib
{
    [DebuggerDisplay("Valid: {IsValidLock}, IsWrite: {isWrite}, ID: {wrapperID}, Index: {idArrayIndex}")]
    public readonly struct VRWLockHoldScoped : IDisposable
    {
        readonly VReaderWriterLockSlim rwLock;
        internal readonly long wrapperID;
        internal readonly ushort idArrayIndex;
        public readonly bool isWrite;
        
        public bool IsValidLock => rwLock != null && rwLock.IsValid(this);

        public VRWLockHoldScoped(VReaderWriterLockSlim rwLock, long wrapperID, ushort idArrayIndex, bool isWrite)
        {
            this.rwLock = rwLock;
            this.wrapperID = wrapperID;
            this.idArrayIndex = idArrayIndex;
            this.isWrite = isWrite;
        }

        public void Dispose()
        {
#if EXPOSE_MULTIPLE_DISPOSE
            if (!rwLock.InvalidateScoped(this))
                UnityEngine.Debug.LogError("Trying to dispose a VRWLockSlimScoped that has already been disposed!");
#else
            rwLock.InvalidateScoped(this);
#endif
        }
    }
}