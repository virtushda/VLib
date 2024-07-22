using System.Collections.Generic;
using Unity.Mathematics;

namespace VLib
{
    /// <summary>
    /// Provides IDs from 1 to int.maxvalue
    /// </summary>
    public class VIdentityProviderInt32Positive : VIdentityProviderBase<int>
    {
        public override int MinValue => 1;
        
        public VIdentityProviderInt32Positive() { nextValue = MinValue; }
        
        protected override int MaxPossibleValue() => int.MaxValue;

        protected override int OffsetValue(int value, int offset) => math.clamp(value + offset, MinValue, MaxPossibleValue());

        protected override void ResetNextValue() => nextValue = MinValue;
    }
}