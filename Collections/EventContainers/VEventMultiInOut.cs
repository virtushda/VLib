using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.Profiling;
using VLib;

/// <summary> A typed event container that aggregates every invocation result into a disposable pooled output collection. </summary>
public class VEventMultiInOut<TIn, TOut>
{
    static readonly ConcurrentVPoolParameterless<OutputState> OutputPool = new(repoolPreProcess: state => state.Reset());

    static long nextOutputID;

    readonly object invocationListLock = new();
    volatile int invokingDepth;
    List<Func<TIn, TOut>> InvocationList = new();

    internal sealed class OutputState
    {
        public readonly List<TOut> outputs = new();
        public long id;

        public void Reset()
        {
            outputs.Clear();
            id = 0;
        }
    }

    /// <summary> Disposable pooled outputs for a single invocation. Treat as single-owner and do not read while disposing from another thread. </summary>
    public readonly struct Outputs : IDisposable, IReadOnlyList<TOut>
    {
        internal readonly OutputState state;
        readonly long id;

        public bool IsValid => state != null && state.id == id;
        public static implicit operator bool(in Outputs outputs) => outputs.IsValid;

        internal Outputs(OutputState state)
        {
            this.state = state;
            id = state?.id ?? 0;
        }

        public int Count => GetValidatedOutputs().Count;

        public int CountSafe => IsValid ? state.outputs.Count : 0;

        public TOut this[int index] => GetValidatedOutputs()[index];

        public void Dispose()
        {
            if (state == null)
                return;
            if (Interlocked.CompareExchange(ref state.id, 0, id) != id)
            {
                if (id != 0)
                    UnityEngine.Debug.LogError($"Trying to dispose invalid {nameof(VEventMultiInOut<TIn, TOut>)}.{nameof(Outputs)}!");
                return;
            }

            OutputPool.Repool(state);
        }

        public Enumerator GetEnumerator() => new(GetValidatedOutputs());

        IEnumerator<TOut> IEnumerable<TOut>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IReadOnlyList<TOut> GetValidatedOutputs()
        {
            if (state == null)
                throw new InvalidOperationException($"{nameof(VEventMultiInOut<TIn, TOut>)}.{nameof(Outputs)} is not initialized!");
            if (state.id == 0 || state.id != id)
                throw new InvalidOperationException($"{nameof(VEventMultiInOut<TIn, TOut>)}.{nameof(Outputs)} has been disposed!");
            return state.outputs;
        }

        public struct Enumerator : IEnumerator<TOut>
        {
            readonly IReadOnlyList<TOut> outputs;
            readonly int count;
            int index;

            internal Enumerator(IReadOnlyList<TOut> outputs)
            {
                this.outputs = outputs;
                count = outputs.Count;
                index = -1;
            }

            public void Dispose() { }

            public TOut Current => outputs[index];

            object IEnumerator.Current => Current;

            public bool MoveNext() => ++index < count;

            public void Reset() => index = -1;
        }
    }

    public static VEventMultiInOut<TIn, TOut> operator +(VEventMultiInOut<TIn, TOut> eventContainer, Func<TIn, TOut> action)
    {
        lock (eventContainer.invocationListLock)
        {
            eventContainer.ThrowIfInvoking();
            eventContainer.InvocationList.Add(action);
        }
        return eventContainer;
    }

    public static VEventMultiInOut<TIn, TOut> operator -(VEventMultiInOut<TIn, TOut> eventContainer, Func<TIn, TOut> action)
    {
        lock (eventContainer.invocationListLock)
        {
            eventContainer.ThrowIfInvoking();
            eventContainer.InvocationList.Remove(action);
        }
        return eventContainer;
    }

    /// <summary> Invokes all listeners and returns a pooled output lease that must be disposed by the caller. </summary>
    public Outputs Invoke(TIn arg)
    {
        Profiler.BeginSample($"VEventMultiOutput<{typeof(TIn).Name}, {typeof(TOut).Name}>.Invoke");
        var outputs = RentOutputs();
        try
        {
            lock (invocationListLock)
            {
                invokingDepth++;
                try
                {
                    var outputList = outputs.state.outputs;
                    foreach (var action in InvocationList)
                    {
                        if (action == null)
                        {
                            UnityEngine.Debug.LogError($"Null action in VEventMultiOutput<{typeof(TIn).Name}, {typeof(TOut).Name}>, skipping...");
                            continue;
                        }

                        Profiler.BeginSample($"VEventMultiOutput-{action.Method.Name}");
                        try
                        {
                            outputList.Add(action.Invoke(arg));
                        }
                        catch (Exception exception)
                        {
                            UnityEngine.Debug.LogError($"Exception in VEventMultiOutput<{typeof(TIn).Name}, {typeof(TOut).Name}>, logging and continuing...");
                            UnityEngine.Debug.LogException(exception);
                        }
                        finally
                        {
                            Profiler.EndSample();
                        }
                    }
                }
                finally
                {
                    invokingDepth--;
                }
            }

            return outputs;
        }
        catch
        {
            outputs.Dispose();
            throw;
        }
        finally
        {
            Profiler.EndSample();
        }
    }

    public bool Clear()
    {
        lock (invocationListLock)
        {
            ThrowIfInvoking();
            var hadListeners = InvocationList.Count > 0;
            if (InvocationList.Capacity > 32)
                InvocationList = new();
            else
                InvocationList.Clear();
            return hadListeners;
        }
    }

    public bool CopyTo(VEventMultiInOut<TIn, TOut> other)
    {
        if (other == null)
            return false;
        if (ReferenceEquals(this, other))
            return true;

        List<Func<TIn, TOut>> invocationSnapshot;
        lock (invocationListLock)
        {
            if (InvocationList.Count == 0)
                return false;
            invocationSnapshot = new List<Func<TIn, TOut>>(InvocationList);
        }

        lock (other.invocationListLock)
        {
            other.ThrowIfInvoking();
            other.InvocationList.Clear();
            other.InvocationList.AddRange(invocationSnapshot);
        }

        return true;
    }

    public bool CopyFrom(VEventMultiInOut<TIn, TOut> other) => other?.CopyTo(this) ?? false;

    Outputs RentOutputs()
    {
        var outputState = OutputPool.Depool();
        outputState.id = Interlocked.Increment(ref nextOutputID);
        return new Outputs(outputState);
    }

    void ThrowIfInvoking()
    {
        if (invokingDepth > 0)
            throw new InvalidOperationException($"Cannot modify {nameof(VEventMultiInOut<TIn, TOut>)} while invoking.");
    }
}