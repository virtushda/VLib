using System;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace VLib
{
    /// <summary> A typed event container with a callback that is invoked per-invocation. </summary>
    public class VCallbackEvent<TIn, TOut>
    {
        private static readonly ThreadSafePoolParameterless<List<Func<TIn, TOut>>> listPool = new();
        
        public static VCallbackEvent<TIn, TOut> operator +(VCallbackEvent<TIn, TOut> eventContainer, Func<TIn, TOut> action)
        {
            eventContainer.EnsureList();
            eventContainer.InvocationList.Add(action);
            return eventContainer;
        }
        public static VCallbackEvent<TIn, TOut> operator -(VCallbackEvent<TIn, TOut> eventContainer, Func<TIn, TOut> action)
        {
            eventContainer.EnsureList();
            eventContainer.InvocationList.Remove(action);
            return eventContainer;
        }
        
        public List<Func<TIn, TOut>> InvocationList;
        Action<TIn, TOut> callback;
        
        public VCallbackEvent(Action<TIn, TOut> callback = null) => this.callback = callback;

        public void Invoke(TIn arg)
        {
            Profiler.BeginSample($"RelayEvent<{typeof(TIn).Name}, {typeof(TOut).Name}>.Invoke");
            if (InvocationList == null)
            {
                Profiler.EndSample();
                return;
            }

            foreach (var action in InvocationList)
            {
                if (action == null)
                {
                    UnityEngine.Debug.LogError($"Null action in RelayEvent<{typeof(TIn).Name}, {typeof(TOut).Name}>, skipping...");
                    continue;
                }

                // Contain each invocation
                Profiler.BeginSample($"RelayEvent-{action.Method.Name}");
                try
                {
                    var output = action.Invoke(arg);
                    callback?.Invoke(arg, output);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"Exception in RelayEvent<{typeof(TIn).Name}, {typeof(TOut).Name}>, logging and continuing...");
                    UnityEngine.Debug.LogException(e);
                }
                finally
                {
                    Profiler.EndSample();
                }
            }
            Profiler.EndSample();
        }

        public bool Clear()
        {
            if (InvocationList == null)
                return false;
            
            InvocationList.Clear();
            listPool.Repool(InvocationList);
            InvocationList = null;
            return true;
        }

        public bool CopyTo(VCallbackEvent<TIn, TOut> other)
        {
            if (InvocationList == null)
                return false;

            other.EnsureList();
            other.InvocationList.Clear();
            foreach (var action in InvocationList)
                other.InvocationList.Add(action);
            
            return true;
        }
        public bool CopyFrom(VCallbackEvent<TIn, TOut> other)
        {
            if (other.InvocationList == null)
                return false;

            EnsureList();
            InvocationList.Clear();
            foreach (var action in other.InvocationList)
                InvocationList.Add(action);
            
            return true;
        }

        private void EnsureList() => InvocationList ??= listPool.Fetch();
    }
}