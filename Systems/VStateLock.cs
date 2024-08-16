// Too much of a mixed bag, faster in some situations, slower in high contention, getting increasingly hard to optimize for little gain. :(

/*//#define PROFILING

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using UnityEngine.Profiling;

namespace VLib.Systems
{
    /// <summary> Conservative lock design that is somewhat less flexible, but maintains a lightweight presence by demanding the caller specify states. </summary>
    public class VStateLock
    {
        public enum UserEffect : byte { None, Add, Remove }
        
        // Implement your own states using const int fields
        int state;
        /// <summary> User count that can block state changes </summary>
        internal int stateUsers;
        
        /// <summary> How long the spinlocks can be a'spinnin </summary>
        int spinMS;
        /// <summary> Main spin lock </summary>
        int stateChangeSpinLockValue;
        ThreadLocal<SpinWait> threadSpinWaiters = new(() => new SpinWait());
        
        /// <summary> If a spinlock breaks, the entire lock becomes unusable immediately to make debugging less of a PITA. </summary>
        bool lockFunctional;

        /// <summary> For debugging output, preferrably pass in a single cached reference. Index is raw state value. </summary>
        string[] stateNames;
        
        public int State => state;
        
        public VStateLock(int initialState = 0, int spinMS = 1000, string[] stateNames = null)
        {
            state = initialState;
            this.spinMS = spinMS;
            stateChangeSpinLockValue = 0; //new SpinLock(Application.isEditor);
            this.stateNames = stateNames;
            lockFunctional = true;
        }

        const string StateLockSetStateSPINLOCK = "StateLock.SetState SPINLOCK";
        /// <summary> If a state is already expected, call <see cref="DemandState"/> instead, which is super super cheap. </summary>
        public void SetState(int newState, out int previousState)
        {
            DemandFunctional();
            
            // Use spin lock to ensure that the state is set atomically
#if PROFILING
            Profiler.BeginSample(StateLockSetStateSPINLOCK);
#endif
            // ENTER MAIN SPINLOCK
            if (!StateChangeSpinLockRaw(128))
            {
                // Spin lock until we get in
                var outerSpinWatch = ValueStopwatch.StartNew();
                while (!StateChangeSpinLockRaw(128))
                {
                    // Timeout
                    if (outerSpinWatch.ElapsedMilliseconds > spinMS)
                    {
                        Debug.LogError("StateLock is being set while it is already being set. Lock is now broken so that it can be debugged without respinning constantly.");
                        lockFunctional = false;
                        previousState = default;
                        Interlocked.Exchange(ref stateChangeSpinLockValue, 0);
                        ApplyUserChange();
#if PROFILING
                        Profiler.EndSample();
#endif
                        return;
                    }
                }
            }
#if PROFILING
            Profiler.EndSample();
#endif
            
            previousState = state;
            // This allows the lock to recurse as long as the requested state is the active state
            if (state == newState)
            {
                ApplyUserChange();
                // RELEASE OUTER SPINLOCK
                Interlocked.Exchange(ref stateChangeSpinLockValue, 0);
                return;
            }

            // Spin again manually if there are current many users
            if (stateUsers > 0)
            {
                ValueStopwatch spinWatch = ValueStopwatch.StartNew();
                while (stateUsers > 0)
                {
                    if (spinWatch.ElapsedMilliseconds > spinMS)
                    {
                        try
                        {
                            Debug.LogError($"StateLock is being set while it has '{stateUsers}' active users. Lock is now broken so that it can be debugged without respinning constantly.");
                            Debug.LogError($"Current lock state: {stateNames?[state] ?? state.ToString()} -> {stateNames?[newState] ?? newState.ToString()}");
                            lockFunctional = false;
                        }
                        catch (Exception e)
                        {
                            Debug.LogError("Couldn't log error nicely, logging exception and then raw lock state messages.");
                            Debug.LogException(e);
                            Debug.LogError($"StateLock is being set while it has '{stateUsers}' active users. Lock is now broken so that it can be debugged without respinning constantly.");
                            Debug.LogError($"Current lock state: {state} -> {newState}");
                        }
                        break;
                    }
                }
            }

            state = newState;
            
            ApplyUserChange();
            
            // Release outer spin lock
            Interlocked.Exchange(ref stateChangeSpinLockValue, 0);

            return;

            void ApplyUserChange() => AddUserAtomic();

            bool StateChangeSpinLockRaw(int iterations)
            {
                while (Interlocked.CompareExchange(ref stateChangeSpinLockValue, 1, 0) != 0)
                {
                    --iterations;
                    if (iterations <= 0)
                        return false;
                }
                return true;
            }

            bool StateUsersSpinLockRaw(int iterations)
            {
                while (stateUsers > 0)
                {
                    --iterations;
                    if (iterations <= 0)
                        return false;
                }
                return true;
            }
        }

        /// <summary> Changes the state if necessary and registers a user with this state. Can recurse.
        /// More efficient not to have it revert the state, but that is an option. <br/> </summary>
        public VStateLockScopeState ChangeInScope(int newState) => new(this, newState);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DemandState(int demandedState)
        {
            DemandFunctional();
            if (state != demandedState)
                throw new InvalidOperationException($"StateLock is in state: {state}, expected state: {demandedState}.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void DemandFunctional()
        {
            if (!lockFunctional)
                throw new InvalidOperationException("StateLock is not functional.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddUserAtomic() => Interlocked.Increment(ref stateUsers);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveUserAtomic()
        {
            if (Interlocked.Decrement(ref stateUsers) < 0)
                Debug.LogError("StateLock is being removed more times than it was added.");
        }
    }

    /// <summary> Set the lock to a state, and set a user for the duration of the scope. (Cheaper if you don't revert the state after) </summary>
    public struct VStateLockScopeState : IDisposable
    {
        VStateLock stateLock;
        
        public VStateLockScopeState(VStateLock stateLock, int newState)
        {
            this.stateLock = stateLock;
            stateLock.SetState(newState, out _);
        }

        public void Dispose() => stateLock.RemoveUserAtomic();
    }
}*/