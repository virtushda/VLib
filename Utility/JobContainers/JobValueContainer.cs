#if UNITY_EDITOR
#define SAFETY
#endif

using Unity.Jobs;
using VLib;

/// <summary> A job handle with a framework for alloc-free OnComplete </summary>
public struct JobValueContainer<T>
    where T : IJobValueContainerElement
{
    JobHandle handle;
    T element;
#if SAFETY
    VSafetyHandle safetyHandle;
#endif
    
    public JobHandle Handle => handle;
    public bool IsCompleted => handle.IsCompleted;
    public T Element => element;

    public JobValueContainer(JobHandle handle, T element)
    {
        this.handle = handle;
        this.element = element;
#if SAFETY
        safetyHandle = VSafetyHandle.Create();
#endif
    }

    public void Complete()
    {
#if SAFETY
        safetyHandle.ConditionalCheckValid();
#endif
        handle.Complete();
        element.OnComplete();
    }

    public void CombineHandleIntoContainer(in JobHandle jobHandle)
    {
#if SAFETY
        safetyHandle.ConditionalCheckValid();
#endif
        handle = JobHandle.CombineDependencies(handle, jobHandle);
    }
}