namespace VLib
{
    /// <summary> Simple, no pooling, goes up to ulong.MaxValue values </summary>
    public struct UlongIDProviderNative
    {
        long nextValue;
        
        public static UlongIDProviderNative Create() => new UlongIDProviderNative { nextValue = -long.MaxValue };

        public ulong NextValue => nextValue.IncrementToUlong();
    }
}