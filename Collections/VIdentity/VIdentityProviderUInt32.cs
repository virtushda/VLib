using Unity.Mathematics;

namespace VLib
{
    /// <summary> Provides uint IDs starting from 1 </summary>
    public class VIdentityProviderUInt32 : VIdentityProviderBase<uint>
    {
        public override uint MinValue => 1;
        
        public VIdentityProviderUInt32() { nextValue = MinValue; }

        protected override uint MaxPossibleValue() => uint.MaxValue;

        protected override uint OffsetValue(uint value, int offset) => (uint) math.clamp(value + offset, MinValue, MaxPossibleValue());
        
        protected override void ResetNextValue() => nextValue = MinValue;
    }
}