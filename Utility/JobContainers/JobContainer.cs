using System;
using Unity.Jobs;

// TODO: Due for upgrades, should use conditional safety handles instead of a bool that can be subverted by a copy

/// <summary>
/// An extension of JobHandle that will automatically perform an action upon completion
/// </summary>
public struct JobContainer
{
    bool isValid;
    JobHandle handle;
    Action onComplete;

    public bool IsValid => isValid;
    public JobHandle Handle => handle;
    public bool IsCompleted => handle.IsCompleted;

    /// <summary>
    /// Creates a JobContainer that can be waited on or completed immediately, and will invoke additional code after the job is complete
    /// </summary>
    /// <param name="handle">The JobHandle of the job being performed</param>
    /// <param name="onComplete">Additional code to run once the job is complete. Use this to dispose native collections or perform post-processing</param>
    public JobContainer(JobHandle handle, Action onComplete)
    {
        isValid = true;
        this.handle = handle;
        this.onComplete = onComplete;
    }

    /// <summary>
    /// Call this method to await completion of the job handle. Invokes OnComplete as soon as the job is finished.
    /// </summary>
    /// <returns>True if the job is still running, false if the job is complete and the wait is over</returns>
    public bool WaitToComplete()
    {
        if (!isValid) return false;
        if (!handle.IsCompleted) return true;

        handle.Complete();
        onComplete?.Invoke();
        isValid = false;
        return false;
    }

    /// <summary>
    /// Forces the job to complete and invokes the OnComplete action.
    /// </summary>
    public void Complete()
    {
        if (!isValid) return;
        handle.Complete();
        onComplete?.Invoke();
        isValid = false;
    }
}