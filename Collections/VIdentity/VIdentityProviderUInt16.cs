using Unity.Mathematics;

namespace VLib
{
    /// <summary> Provides uint IDs starting from 1 </summary>
    public class VIdentityProviderUInt16 : VIdentityProviderBase<ushort>
    {
        public override ushort MinValue => 1;
        
        public VIdentityProviderUInt16() { nextValue = MinValue; }
        protected override ushort MaxPossibleValue() => ushort.MaxValue;
        
        protected override ushort OffsetValue(ushort value, int offset) => (ushort) math.clamp(value + offset, MinValue, MaxPossibleValue());
        protected override void ResetNextValue() => nextValue = MinValue;
    }
}