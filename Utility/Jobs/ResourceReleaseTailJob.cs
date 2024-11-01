using Unity.Jobs;

namespace VLib.Jobs
{
    /// <summary> A resource which needs to be released. </summary>
    public interface IReleasableJobResource
    {
        public void ReleaseJobResource();
    }

    /// <summary> A tail job to release a resource such as a lock or a reader structure. </summary>
    public struct ResourceReleaseTailJob<T> : IJob
        where T : unmanaged, IReleasableJobResource
    {
        T resource;

        public ResourceReleaseTailJob(T resource) => this.resource = resource;

        public void Execute()
        {
            try
            {
                resource.ReleaseJobResource();
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Error releasing job resource, next line...");
                UnityEngine.Debug.LogException(e);
            }
        }
    }

    public struct ManagedResourceReleaseTailJob<T> : IJob
        where T : class, IReleasableJobResource
    {
        JobObjectRef<T> resourceRef;
        
        public ManagedResourceReleaseTailJob(JobObjectRef<T> resourceReference) => resourceRef = resourceReference;
        
        public void Execute()
        {
            try
            {
                if (resourceRef.IsValid && resourceRef.TryGet(out var resource))
                    resource.ReleaseJobResource();
                else
                    UnityEngine.Debug.LogError($"Resource release job failed to get resource.");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Error releasing job resource, next line...");
                UnityEngine.Debug.LogException(e);
            }
            finally
            {
                resourceRef.Dispose();
            }
        }
    }

    /*public struct UnmanagedResourceReleaseTailJob<T> : IJob
        where T : unmanaged, IReleasableJobResource
    {
        RefStruct<T> resourceRef;
        
        public UnmanagedResourceReleaseTailJob(RefStruct<T> resourceReference) => resourceRef = resourceReference;
        
        public void Execute()
        {
            try
            {
                if (resourceRef.IsCreated)
                {
                    ref var resource = ref resourceRef.TryGetRef(out var hasResource);
                    if (hasResource)
                        resource.ReleaseJobResource();
                    else
                        UnityEngine.Debug.LogError($"Resource release job failed to get resource.");
                }
                else
                    UnityEngine.Debug.LogError($"Resource release job failed to get resource. Container not created.");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Error releasing job resource, next line...");
                UnityEngine.Debug.LogException(e);
            }
            finally
            {
                resourceRef.Dispose();
            }
        }
    }*/
    
    public static class ResourceReleaseTailJobExtensions
    {
        public static JobHandle ScheduleResourceRelease<T>(this T resource, JobHandle dependency)
            where T : unmanaged, IReleasableJobResource
        {
            return new ResourceReleaseTailJob<T>(resource).Schedule(dependency);
        }
        
        public static JobHandle ScheduleManagedResourceRelease<T>(this JobObjectRef<T> resourceRef, JobHandle dependency)
            where T : class, IReleasableJobResource
        {
            return new ManagedResourceReleaseTailJob<T>(resourceRef).Schedule(dependency);
        }
    }
}