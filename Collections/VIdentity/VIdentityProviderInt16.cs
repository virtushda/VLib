using Unity.Mathematics;

namespace VLib
{
    /// <summary> Provides uint IDs starting from 1 </summary>
    public class VIdentityProviderInt16 : VIdentityProviderBase<short>
    {
        public override short MinValue => 0;
        
        public VIdentityProviderInt16() { nextValue = MinValue; }
        protected override short MaxPossibleValue() => short.MaxValue;
        
        protected override short OffsetValue(short value, int offset) => (short) math.clamp(value + offset, MinValue, MaxPossibleValue());
        protected override void ResetNextValue() => nextValue = MinValue;
    }
}