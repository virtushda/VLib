using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Assertions;

namespace VLib
{
    public readonly struct SemaphoreSlimHold : IDisposable
    {
        /// <summary> If not null, lock is held. </summary>
        readonly SemaphoreSlim semaphoreSlim;
        readonly VManagedSafetyHandle.User safetyHandle;
        
        public bool IsEntered => safetyHandle.IsValid;
        public static implicit operator bool(SemaphoreSlimHold hold) => hold.IsEntered;
        
        SemaphoreSlimHold(SemaphoreSlim semaphoreSlim)
        {
            Assert.IsTrue(semaphoreSlim != null, "SemaphoreSlim is null!");
            this.semaphoreSlim = semaphoreSlim;
            safetyHandle = VManagedSafetyHandle.AllocateUser();
        }

        /// <summary> Releases the semaphore lock if it was entered. </summary>
        public void Dispose()
        {
            if (!IsEntered)
                return;
            
            safetyHandle.Dispose();
            semaphoreSlim.Release();
        }

        /// <summary>Attempts to acquire the semaphore lock synchronously with an optional timeout.</summary>
        /// <param name="semaphoreSlim">The semaphore to acquire.</param>
        /// <param name="timeoutMS">Optional timeout in milliseconds; if null, waits indefinitely.</param>
        /// <param name="logOnError">Whether to log an error if acquisition fails.</param>
        /// <returns>A <see cref="SemaphoreSlimHold"/> representing the acquired lock, or default if failed.</returns>
        internal static SemaphoreSlimHold TryAcquire(SemaphoreSlim semaphoreSlim, int? timeoutMS = null, bool logOnError = true)
        {
            if (timeoutMS.TryGetValueOrDefault(out var timeoutValue))
            {
                if (semaphoreSlim.Wait(timeoutValue))
                    return new SemaphoreSlimHold(semaphoreSlim);
                if (logOnError)
                    UnityEngine.Debug.LogError("Failed to acquire semaphore slim lock!");
                return default;
            }

            semaphoreSlim.Wait();
            return new SemaphoreSlimHold(semaphoreSlim);
        }

        /// <summary>Attempts to acquire the semaphore lock asynchronously with an optional timeout.</summary>
        /// <param name="semaphoreSlim">The semaphore to acquire.</param>
        /// <param name="timeout">Optional timeout; if null, waits indefinitely.</param>
        /// <param name="logOnError">Whether to log an error if acquisition fails.</param>
        /// <returns>A <see cref="ValueTask{SemaphoreSlimHold}"/> representing the acquired lock, or default if failed.</returns>
        internal static async ValueTask<SemaphoreSlimHold> TryAcquireAsync(SemaphoreSlim semaphoreSlim, TimeSpan? timeout = null, bool logOnError = true)
        {
            bool entered;
            if (timeout.TryGetValueOrDefault(out var timeoutValue))
                entered = await semaphoreSlim.WaitAsync(timeoutValue);
            else
            {
                await semaphoreSlim.WaitAsync();
                entered = true;
            }

            if (!entered && logOnError)
                UnityEngine.Debug.LogError("Failed to acquire semaphore slim lock!");

            return entered ? new SemaphoreSlimHold(semaphoreSlim) : default;
        }
    }
}

namespace VLib.SyncPrimitives
{
    public static class SemaphoreSlimExt
    {
        /// <inheritdoc cref="SemaphoreSlimHold.TryAcquire"/>
        public static SemaphoreSlimHold TryAcquire(this SemaphoreSlim semaphoreSlim, int? timeoutMS = null, bool logOnError = true)
        {
            return SemaphoreSlimHold.TryAcquire(semaphoreSlim, timeoutMS, logOnError);
        }

        /// <inheritdoc cref="SemaphoreSlimHold.TryAcquireAsync"/>
        public static async ValueTask<SemaphoreSlimHold> TryAcquireAsync(this SemaphoreSlim semaphoreSlim, TimeSpan? timeout = null, bool logOnError = true)
        {
            return await SemaphoreSlimHold.TryAcquireAsync(semaphoreSlim, timeout, logOnError);
        }
    }
}