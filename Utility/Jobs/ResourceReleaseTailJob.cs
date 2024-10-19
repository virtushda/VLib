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
    
    public static class ResourceReleaseTailJobExtensions
    {
        public static JobHandle ScheduleResourceRelease<T>(this T resource, JobHandle dependency)
            where T : unmanaged, IReleasableJobResource
        {
            return new ResourceReleaseTailJob<T>(resource).Schedule(dependency);
        }
    }
}