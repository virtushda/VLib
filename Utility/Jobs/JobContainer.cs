using System;
using System.Collections.Generic;
using Unity.Jobs;
using VLib;

/// <summary> An extension of JobHandle that will automatically perform an action upon completion </summary>
public struct JobContainer : IDisposable
{
    RefStruct<JobHandle> handleHolder;
    List<Action> onComplete; // TODO: These could be pooled.

    public bool IsValid => handleHolder.IsCreated;
    public JobHandle Handle => handleHolder.TryGetValue(out var handle) ? handle : default;
    public bool IsCompleted => handleHolder.TryGetValue(out var handle) && handle.IsCompleted;

    /// <summary>
    /// Creates a JobContainer that can be waited on or completed immediately, and will invoke additional code after the job is complete
    /// </summary>
    /// <param name="handle">The JobHandle of the job being performed</param>
    /// <param name="onComplete">Additional code to run once the job is complete. Use this to dispose native collections or perform post-processing</param>
    public JobContainer(JobHandle handle, Action onComplete)
    {
        handleHolder = RefStruct<JobHandle>.Create(handle);
        
        // Always create the list, so that if the struct is copied, every copy is guaranteed to point at the right collection.
        this.onComplete = new List<Action>();
        if (onComplete != null)
            this.onComplete.Add(onComplete);
    }

    public void Dispose()
    {
        if (handleHolder.TryDispose())
        {
            onComplete?.Clear();
            onComplete = null;
        }
    }

    /// <summary> Call this method to see if you need to await completion of the job handle. Invokes OnComplete as soon as the job is finished. </summary>
    /// <returns>True if the job is still running, false if the job is complete and the wait is over</returns>
    public bool WaitToComplete()
    {
        if (!IsValid) 
            return false;
        if (!IsCompleted)
            return true;
        Complete();
        return false;
    }

    /// <summary>
    /// Forces the job to complete and invokes the OnComplete action.
    /// </summary>
    public void Complete()
    {
        if (handleHolder.TryGetValue(out var handle))
        {
            handle.Complete();
            onComplete?.Invoke();
        }
        Dispose();
    }

    /// <summary> Combines handles and onComplete actions, disposing the other handle. </summary>
    public JobContainer Consume(JobContainer other)
    {
        // Try to tend toward returning a valid handle, if possible
        if (!IsValid)
            return other;
        if (!other.IsValid)
            return this;

        // Merge handles
        handleHolder.ValueCopy = JobHandle.CombineDependencies(Handle, other.Handle);
        // Merge onComplete actions
        if (other.onComplete is {Count: > 0})
            onComplete.AddRange(other.onComplete);
        
        // Dispose the other handle
        other.Dispose();
        return this;
    }
}