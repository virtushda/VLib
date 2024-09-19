namespace VLib
{
    public unsafe interface IVLibUnsafeContainerReadOnly : IAllocating
    {
        int Length { get; }
        int Capacity { get; }
        
        void* GetUnsafePtr();
    }
}