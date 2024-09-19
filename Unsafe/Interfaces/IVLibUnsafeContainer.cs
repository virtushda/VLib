namespace VLib
{
    public interface IVLibUnsafeContainer : IVLibUnsafeContainerReadOnly
    {
        new int Length { get; set; }
        new int Capacity { get; set; }
    }
}