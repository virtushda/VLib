#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define SINGLE_OP
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Debug = UnityEngine.Debug;

namespace VLib.Safety
{
    /// <summary> A utility struct that simplifies enforcing single operations. <br/>
    /// The idea is that you can put a number of restrictions in place that get conditionally compiled out in builds. </summary>
    public class SingleOpEnforcer
    {
        readonly object locker = new();
        
        int lastOpCallerLine;
        string lastOpCaller;
        
        public static SingleOpEnforcer Create() => new() {lastOpCallerLine = -1};

        /// <summary> Mark the start of an operation. </summary>
        [Conditional("SINGLE_OP")]
        public void StartOp([CallerLineNumber] int callerLine = -1, [CallerMemberName] string callerName = null) => StartOpCustomLine(callerLine, callerName);

        [Conditional("SINGLE_OP")]
        void StartOpCustomLine(int callerLine, string callerName)
        {
            BurstAssert.TrueCheap(callerLine >= 0);
            lock (locker)
            {
                if (lastOpCallerLine != -1)
                    Debug.LogError($"SingleOpEnforcer: Operation started at line '{lastOpCallerLine}', by '{callerName ?? "NULL"}', not ended.");
                lastOpCallerLine = callerLine;
                lastOpCaller = callerName;
            }
        }

        /// <summary> Mark the end of an operation. </summary>
        [Conditional("SINGLE_OP")]
        public void CompleteOp(bool logError = true)
        {
            lock (locker)
            {
                if (lastOpCallerLine == -1 && logError)
                    Debug.LogError("BurstSingleOpEnforcer: Operation completed without starting.");
                lastOpCallerLine = -1;
                lastOpCaller = null;
            }
        }
        
        public struct ScopedOp : IDisposable
        {
#if SINGLE_OP
            readonly SingleOpEnforcer enforcer;
#endif

            public ScopedOp(SingleOpEnforcer enforcer, int callerLine, string callerName)
            {
#if SINGLE_OP
                this.enforcer = enforcer;
                enforcer.StartOpCustomLine(callerLine, callerName);
                return;
#endif
            }

            public void Dispose()
            {
#if SINGLE_OP
                enforcer?.CompleteOp();
#endif
            }
        }
    }

    public static class SingleOpEnforcerExt
    {
        public static SingleOpEnforcer.ScopedOp ScopedOp(this SingleOpEnforcer enforcer, [CallerLineNumber] int callerLine = -1, [CallerMemberName] string callerName = null) 
            => new(enforcer, callerLine, callerName);
    }
}