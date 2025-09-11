namespace VLib.Structures
{
    public struct FixedBitArray128
    {
        public const int CAPACITY = 128;
        
        public ulong bits0;
        public ulong bits1;
        
        public int Capacity => CAPACITY;
        
        public static FixedBitArray128 All => new(ulong.MaxValue, ulong.MaxValue);
        
        public bool IsEmpty => bits0 == 0 && bits1 == 0;
        
        public FixedBitArray128(ulong bits0, ulong bits1)
        {
            this.bits0 = bits0;
            this.bits1 = bits1;
        }

        public bool this[int index]
        {
            get
            {
                if ((uint)index >= CAPACITY)
                    throw new System.IndexOutOfRangeException($"Index {index} out of range [0-127]");
                
                if (index < 64)
                    return ((bits0 >> index) & 1UL) != 0UL;
                else
                    return ((bits1 >> (index - 64)) & 1UL) != 0UL;
            }
            set
            {
                if ((uint)index >= CAPACITY)
                    throw new System.IndexOutOfRangeException($"Index {index} out of range [0-127]");
                
                if (index < 64)
                {
                    if (value)
                        bits0 |= 1UL << index;
                    else
                        bits0 &= ~(1UL << index);
                }
                else
                {
                    int adjustedIndex = index - 64;
                    if (value)
                        bits1 |= 1UL << adjustedIndex;
                    else
                        bits1 &= ~(1UL << adjustedIndex);
                }
            }
        }

        public void Invert()
        {
            bits0 = ~bits0;
            bits1 = ~bits1;
        }
        
        public void SetAll()
        {
            bits0 = ulong.MaxValue;
            bits1 = ulong.MaxValue;
        }
        
        public void Clear()
        {
            bits0 = 0;
            bits1 = 0;
        }
    }
}