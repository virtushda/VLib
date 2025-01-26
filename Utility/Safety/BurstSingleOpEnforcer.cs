#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define BURST_SINGLE_OP
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace VLib.Safety
{
    /// <summary> A utility struct that simplifies enforcing single operations. <br/>
    /// The idea is that you can put a number of restrictions in place that get conditionally compiled out in builds. </summary>
    public struct BurstSingleOpEnforcer
    {
        int locker;
        int lastOpLine;
        
        public static BurstSingleOpEnforcer Create() => new() {lastOpLine = -1, locker = 0};

        [Conditional("BURST_SINGLE_OP")]
        public void StartOp([CallerLineNumber] int callerLine = -1) => StartOpCustomLine(callerLine);

        void StartOpCustomLine(int callerLine)
        {
            Lock();
            if (lastOpLine != -1)
                UnityEngine.Debug.LogError($"BurstSingleOpEnforcer: Operation started at line {lastOpLine} not ended.");
            lastOpLine = callerLine;
            Unlock();
        }

        [Conditional("BURST_SINGLE_OP")]
        public void CompleteOp()
        {
            Lock();
            if (lastOpLine == -1)
                UnityEngine.Debug.LogError("BurstSingleOpEnforcer: Operation completed without starting.");
            lastOpLine = -1;
            Unlock();
        }

        void Lock()
        {
            while (Interlocked.CompareExchange(ref locker, 1, 0) != 0)
            {
            }
        }

        void Unlock() => Interlocked.Exchange(ref locker, 0);
        
        public struct ScopedOp : IDisposable
        {
            readonly RefStruct<BurstSingleOpEnforcer> enforcer;

            public ScopedOp(RefStruct<BurstSingleOpEnforcer> enforcer, int callerLine)
            {
#if BURST_SINGLE_OP
                BurstAssert.True(enforcer.IsCreated);
                this.enforcer = enforcer;
                enforcer.ValueRef.StartOpCustomLine(callerLine);
                return;
#endif
                this.enforcer = default;
            }

            public void Dispose()
            {
#if BURST_SINGLE_OP
                if (enforcer.TryGetValue(out var enforcerValue))
                    enforcerValue.CompleteOp();
#endif
            }
        }

        /*static long nextID;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void StaticInit()
        {
            nextID = 0;
            lockCounter = 0;
            activeIDTracking = new(16, Allocator.Persistent);

            EditorApplication.playModeStateChanged += StaticDispose;
        }

        static void StaticDispose(PlayModeStateChange playModeStateChange)
        {
            EditorApplication.playModeStateChanged -= StaticDispose;
            if (playModeStateChange == PlayModeStateChange.ExitingPlayMode)
            {
                activeIDTracking.Dispose();
                activeIDTracking = default;
            }
        }

        public static long GetNextID() => Interlocked.Increment(ref nextID);*/
    }

    public static class BurstSingleOpEnforcerExt
    {
        public static BurstSingleOpEnforcer.ScopedOp ScopedOp(this RefStruct<BurstSingleOpEnforcer> enforcer, [CallerLineNumber] int callerLine = -1) => new(enforcer, callerLine);
    }
    
}