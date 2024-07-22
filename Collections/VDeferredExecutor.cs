using System;
using System.Collections.Concurrent;
using UnityEngine;

/// <summary>
/// Can be used to queue up deferred actions from ANY thread.
/// Initial purpose was to gather work that can only be done on the main thread without sacrificing other perf gains.
/// </summary>
/// <typeparam name="T">Type of value that can be gathered to then be fed to the specified actionT in the constructor.</typeparam>
public class VDeferredExecutor<T>
{
    ConcurrentBag<T> bag = new ConcurrentBag<T>();
    Action<T> deferredAction;

    public VDeferredExecutor(Action<T> deferredAction)
    {
        this.deferredAction = deferredAction;
    }

    public void Add(T value) => bag.Add(value);

    public void Clear() => bag.Clear();

    public void ExecuteDeferredOps()
    {
        //Skip checking bag.count constantly
        int count = bag.Count;
        for (int i = 0; i < count; i++) 
            ExecuteNext();

        //Final pass to avoid missing elements added during execution
        while (bag.Count > 0)
            ExecuteNext();

        bag.Clear();

        void ExecuteNext()
        {
            if (!bag.TryTake(out T value))
                throw new UnityException("Unable to take from deferred value bag...");
            deferredAction.Invoke(value);
        }
    }
}