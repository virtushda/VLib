namespace VLib
{
    /// <summary> SafeLists are burst compatible thread-safe UnsafeLists and they must be kept safe by relaying time to them so they can abort their own locks. </summary>
    public class SafeListManager : ITimeReceiver
    {
        public float Time { get; set; } = 0;
        
        
    }
}