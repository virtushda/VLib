#if UNITY_EDITOR
//#define SAFETY_TRACKING // Enable this for each lock scope to be tracked, catching any double-disposal issues.
#if SAFETY_TRACKING
#define STACKTRACE_CLAIM_TRACKING
#endif
//#define LOCK_TIMING
#endif

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Profiling;

namespace VLib
{
    /// <summary> Contains a <see cref="ReaderWriterLockSlim"/> but is capable of handing out safely scoped structs that lock and are protected against double disposal.
    /// Scoping structs support up to <see cref="ThreadSafeArrayLength"/> simultaneous locks, if you need something that can breach this, use a different lock. </summary>
    public class VReaderWriterLockSlim
    {
        const int DefaultTimeoutMS = 2000;
        const int LOCK_TIMING_LOGTHRESHOLD = 100;

        public ReaderWriterLockSlim internalLock;

#if SAFETY_TRACKING
        long wrapperIDSource;
        internal ConcurrentDictionary<long, string> activeIDTracking = new();
#endif

        public VReaderWriterLockSlim(LockRecursionPolicy supportsRecursion = LockRecursionPolicy.SupportsRecursion)
        {
#if SAFETY_TRACKING
            wrapperIDSource = 0;
#endif

            // Create Objects
            internalLock = new(supportsRecursion);
        }

        public void Dispose()
        {
            internalLock?.Dispose();
            internalLock = null;
        }

        public VRWLockHoldScoped ScopedReadLock(int timeoutMS = DefaultTimeoutMS)
        {
#if LOCK_TIMING
            var watch = ValueStopwatch.StartNew();
#endif
            // Get lock
            if (!internalLock.TryEnterReadLock(timeoutMS))
                throw new TimeoutException($"Failed to acquire read lock within {timeoutMS}ms!");
#if LOCK_TIMING
            var timeMS = watch.ElapsedMillisecondsF;
            if (timeMS > LOCK_TIMING_LOGTHRESHOLD)
                UnityEngine.Debug.LogError($"Read lock took {(timeMS / 1000f).AsTimeToPrint()} to acquire!");
#endif

#if SAFETY_TRACKING
            var wrapperID = CreateValidation();
            return new VRWLockHoldScoped(this, false, wrapperID);
#else
            return new VRWLockHoldScoped(this, false);
#endif
        }

        public VRWLockHoldScoped ScopedExclusiveLock(int timeoutMS = DefaultTimeoutMS)
        {
#if LOCK_TIMING
            var watch = ValueStopwatch.StartNew();
#endif
            // Get lock
            if (!internalLock.TryEnterWriteLock(timeoutMS))
                throw new TimeoutException($"Failed to acquire write lock within {timeoutMS}ms!");
#if LOCK_TIMING
            var timeMS = watch.ElapsedMillisecondsF;
            if (timeMS > LOCK_TIMING_LOGTHRESHOLD)
                UnityEngine.Debug.LogError($"Write lock took {(timeMS / 1000f).AsTimeToPrint()} to acquire!");
#endif

#if SAFETY_TRACKING
            var wrapperID = CreateValidation();
            return new VRWLockHoldScoped(this, true, wrapperID);
#else
            return new VRWLockHoldScoped(this, true);
#endif
        }

        static readonly ProfilerMarker TryEnterReadWithWriteRequiredConditionMarker = new("TryEnterReadWithWriteRequiredCondition");

        /// <summary> In a fully concurrent-safe way, this method obtains the read-lock, while ensuring that a condition is true. <br/>
        /// This is to handle the complex case, where changing the condition must happen behind a write lock! </summary>
        /// <param name="conditionEvaluator"> The condition that must be true for the read lock to be obtained. (This MUST be safe to call inside the read-lock!) </param>
        /// <param name="conditionChanger"> The action that changes the condition, this will be called if the condition is false. <br/>
        ///     (This MUST be safe to call inside the write-lock!) </param>
        /// <param name="timeoutMS"> The timeout in milliseconds to wait for the lock. </param>
        /// <param name="attempts"> The amount of times this function will cycle over the lock attempting to change the condition. </param>
        /// <returns> If within timeout: A scoped lock that will release the read lock when disposed. <br/>
        /// Otherwise: A default scope lock struct that must be evaluated. </returns>
        public VRWLockHoldScoped TryEnterReadWithWriteLockCondition(Func<bool> conditionEvaluator, Action conditionChanger, int timeoutMS = DefaultTimeoutMS, int attempts = 64)
        {
            using var _ = TryEnterReadWithWriteRequiredConditionMarker.Auto();

            attempts = math.max(attempts, 1);
            while (--attempts >= 0)
            {
                // Enter read
                if (!internalLock.TryEnterReadLock(timeoutMS))
                    throw new TimeoutException($"Failed to acquire read lock within {timeoutMS}ms!");

                bool conditionPassed = false;
                try
                {
                    // Check Condition
                    conditionPassed = conditionEvaluator();
                }
                catch (Exception e)
                {
                    // Release read lock to avoid deadlock
                    internalLock.ExitReadLock();
                    throw new InvalidOperationException("Failed to evaluate condition!", e);
                }

                // If condition is true, return the lock
                if (conditionPassed)
                    // VALID EXIT POINT, construct a scope struct from the current state
                {
#if SAFETY_TRACKING
                    var wrapperID = CreateValidation();
#endif
                    return new VRWLockHoldScoped(this, false
#if SAFETY_TRACKING
                        , wrapperID
#endif
                    );
                }

                // Past this point we've failed to acquire a readlock together with a valid condition, we have to now try to change the condition. 
                // Release read lock
                internalLock.ExitReadLock();

                // Enter write lock
                if (!internalLock.TryEnterWriteLock(timeoutMS))
                    throw new TimeoutException($"Failed to acquire write lock within {timeoutMS}ms!");

                // Change the condition
                try
                {
                    conditionChanger();
                }
                catch (Exception e)
                {
                    // Release write lock to avoid deadlock
                    internalLock.ExitWriteLock();
                    throw new InvalidOperationException("Failed to change condition!", e);
                }

                // Release write lock
                internalLock.ExitWriteLock();
                // Loop back to try to acquire the read lock again
            }

            return default; // This failure case must be handled by the caller, 'IsCreated' will be false.
        }

#if SAFETY_TRACKING
        long CreateValidation()
        {
            var wrapperID = Interlocked.Increment(ref wrapperIDSource);
            string trace = null;
#if STACKTRACE_CLAIM_TRACKING
            trace = Environment.StackTrace;
#endif
            if (!activeIDTracking.TryAdd(wrapperID, trace))
                throw new InvalidOperationException($"Failed to add ID {wrapperID} to tracking dictionary!");
            return wrapperID;
        }
#endif
    }

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