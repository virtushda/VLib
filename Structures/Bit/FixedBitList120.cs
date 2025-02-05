using System;
using UnityEngine;

namespace VLib.Structures
{
    /// <summary> A struct type can arbitrarily store 120 bits, using 8 bits to store the used count. </summary>
    [Serializable]
    public struct FixedBitList120
    {
        public ulong bits0;
        /// <summary> First 56 bits are used for bit storage, the last 8 bits are used for the count. </summary>
        public ulong bits1;

        public byte Count
        {
            get => (byte) (bits1 >> 56);
            set
            {
                if (value > MaxCapacity)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Count cannot exceed 120.");
                // Mask
                bits1 &= 0x00FFFFFFFFFFFFFF;
                // Set
                bits1 |= (ulong) value << 56;
            }
        }
        
        public const int MaxCapacity = 120;
        public int Capacity => MaxCapacity;
        
        public bool this[int index]
        {
            get
            {
                if ((uint)index >= Count)
                {
                    Debug.LogError($"Index {index} out of range.");
                    return false;
                }
                return index < 64 ? BitUtility.ReadBit(bits0, index) : BitUtility.ReadBit(bits1, (byte)(index - 64));
            }
            set
            {
                if ((uint)index >= Count)
                {
                    Debug.LogError($"Index {index} out of range.");
                    return;
                }

                if (index < 64)
                    BitUtility.WriteBit(ref bits0, (byte)index, value);
                else
                    BitUtility.WriteBit(ref bits1, (byte)(index - 64), value);
            }
        }
        
        public void Clear()
        {
            bits0 = 0;
            bits1 = 0;
        }

        public void AddNoResize(bool bit)
        {
            var count = Count;
            switch (count)
            {
                case < 64: 
                    BitUtility.WriteBit(ref bits0, count, bit);
                    break;
                case < MaxCapacity:
                    BitUtility.WriteBit(ref bits1, (byte)(count - 64), bit);
                    break;
                default:
                    Debug.LogError("Too many transits recorded. Hit limit of 120.");
                    return;
            }
            ++count;
        }
        
        public void InsertBitAtStart(bool bit)
        {
            var count = Count;
            switch (count)
            {
                case < 64: 
                    BitUtility.InsertBitAtLowest(ref bits0, bit); 
                    break;
                case < MaxCapacity:
                    // Take the last bit that's about to be pushed off the end of bits0
                    var lastBit = BitUtility.ReadBit(bits0, 63);
                    // Push it to the next chunk
                    BitUtility.InsertBitAtLowest(ref bits1, lastBit);
                    // Push the new value to the first bit
                    BitUtility.InsertBitAtLowest(ref bits0, bit);
                    break;
                default:
                    Debug.LogError("Too many transits recorded. Hit limit of 120.");
                    return;
            }
            Count = (byte) (count + 1); // Set count in this way to restore it when the second memory chunk is edited as it ruins the count information
        }
    }
}