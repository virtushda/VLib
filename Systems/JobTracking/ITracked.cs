namespace VLib
{
    public interface ITracked/* : IUniqueID64*/
    {
        public ulong ID { get; }
        
        public bool IsCreated { get; }
        
        public void Dispose(bool logException = true);
        
        /*public JobHandle GetJobHandle();
        public JobHandle GetJobHandleWith(JobHandle inDeps);
        public void AddDependency(JobHandle job);*/
    }
}