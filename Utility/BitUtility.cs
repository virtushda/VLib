using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace VLib
{
    public static class BitUtility
    {
        public const byte ByteOne = 1;
        public const int ByteBitCount = 8;
        public const byte FourBitMaxValue = 15;
        public const float FourBitMaxValueF = FourBitMaxValue;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe long BitSizeOf<T>() where T : unmanaged => sizeof(T) * 8;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadBitUnsafe(this byte b, byte index) => (b & (ByteOne << index)) == 1;
        
        public static void WriteBitUnsafe(ref this byte b, byte index, bool value) => b = (byte) (value ? b | (ByteOne << index) : b & ~(ByteOne << index));

        ///<summary> Up to 64 bits. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadBit<T>(ref T readTarget, byte bitIndex)
            where T : unmanaged
        {
            // Must fit type and ulong limit of 64 bits
            Assert.IsTrue(bitIndex < BitSizeOf<T>() && bitIndex < 64); //, $"Bit index {bitIndex} out of range");
            var dataAtAddress = UnsafeUtility.As<T, ulong>(ref readTarget);
            return (dataAtAddress & (1UL << bitIndex)) != 0;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBit<T>(ref T writeTarget, byte bitIndex, bool valueToWrite)
            where T : unmanaged
        {
            // Must fit type and ulong limit of 64 bits
            Assert.IsTrue(bitIndex < BitSizeOf<T>() && bitIndex < 64); //, $"Bit index {bitIndex} out of range");
            ref var dataAtAddress = ref UnsafeUtility.As<T, ulong>(ref writeTarget);
            if (valueToWrite)
                dataAtAddress |= 1UL << bitIndex;
            else
                dataAtAddress &= ~(1UL << bitIndex);
        }

        ///<summary> Read from any type a certain number of bits, up to 64 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ulong ReadNBitsFromRef<T>(ref T readTarget, byte bitIndex, byte bitCount) 
            where T : unmanaged
        {
            Assert.IsTrue(bitIndex + bitCount <= BitSizeOf<T>()); //, $"Bit index {bitIndex} + bit count {bitCount} out of range");
            Assert.IsTrue(bitCount <= 64); //, $"Bit count {bitCount} out of range, this method only supports reading up to 64 bits");
            
            var dataAtAddress = UnsafeUtility.As<T, ulong>(ref readTarget);
            var dataAtAddressShifted = dataAtAddress >> bitIndex;
            
            // Can't create a dynamic mask for 64 bits without needing 65 bits, handle this case explicitly
            if (bitCount == 64)
                return dataAtAddressShifted;
            var maskBits = (ulong) ((1 << bitCount) - 1);
            return dataAtAddressShifted & maskBits;
        }

        ///<summary> Write to any type a certain number of bits, up to 64 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteNBitsToRef<T>(ref T writeTarget, byte bitIndex, byte bitCount, ulong valueToWrite)
            where T : unmanaged
        {
            // Must fit type and ulong limit of 64 bits, don't put string in assert, makes this method vastly slower
            Assert.IsTrue(bitIndex + bitCount <= BitSizeOf<T>()); //, $"Bit index {bitIndex} + bit count {bitCount} out of range");
            Assert.IsTrue(bitCount <= 64); //, $"Bit count {bitCount} out of range, this method only supports writing up to 64 bits");
    
            // Get existing data
            ref var dataAtAddress = ref UnsafeUtility.As<T, ulong>(ref writeTarget);
            
            // Create a mask to zero out the bits that will be written
            var maskBits = (1UL << bitCount) - 1;
            // Slide mask to correct bit range and flip it to keep existing data outside write range
            var targetedMask = ~(maskBits << bitIndex);
            
            // Mask the target so the bits that will be written are zeroed out
            dataAtAddress &= targetedMask;
            
            // Shift the value to the left, so it's in the 'right' position (HAHAHAHA)
            var shiftedValue = valueToWrite << bitIndex;
            dataAtAddress |= shiftedValue;
        }

        public static byte Read8BitsFromInt(int readTarget, byte byteIndex)
        {
            Assert.IsTrue(byteIndex < 4); //, $"Byte index {byteIndex} out of range");
            var bitIndex = byteIndex * ByteBitCount;
            return (byte) ((readTarget >> bitIndex) & 0xFF);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Read8BitsFromInt(uint readTarget, byte byteIndex)
        {
            Assert.IsTrue(byteIndex < 4); //, $"Byte index {byteIndex} out of range");
            var bitIndex = byteIndex * ByteBitCount;
            return (byte) ((readTarget >> bitIndex) & 0xFF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write8BitsToInt(ref int writeTarget, byte byteIndex, byte valueToWrite)
        {
            Assert.IsTrue(byteIndex < 4); //, $"Byte index {byteIndex} out of range");
            var bitIndex = byteIndex * ByteBitCount;
            writeTarget = (writeTarget & ~(0xFF << bitIndex)) | (valueToWrite << bitIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write8BitsToInt(ref uint writeTarget, byte byteIndex, byte valueToWrite)
        {
            Assert.IsTrue(byteIndex < 4); //, $"Byte index {byteIndex} out of range");
            var bitIndex = byteIndex * ByteBitCount;
            writeTarget = (uint) ((writeTarget & ~(0xFF << bitIndex)) | (uint) (valueToWrite << bitIndex));
        }

        /*public static unsafe byte ReadFromValue<T>(ref T readTarget, byte byteIndex) 
            where T : unmanaged
        {
            Assert.IsTrue(byteIndex < sizeof(T), $"Byte index {byteIndex} out of range");
            return UnsafeUtility.ReadArrayElement<byte>(UnsafeUtility.AddressOf(ref readTarget), byteIndex);
        }
        
        public static unsafe void WriteToValue<T>(ref T writeTarget, byte byteIndex, byte valueToWrite) 
            where T : unmanaged
        {
            Assert.IsTrue(byteIndex < sizeof(T), $"Byte index {byteIndex} out of range");
            UnsafeUtility.WriteArrayElement(UnsafeUtility.AddressOf(ref writeTarget), byteIndex, valueToWrite);
        }*/

        /*public static void WriteToFloat(ref float writeTarget, byte byteIndex, byte valueToWrite)
        {
            Assert.IsTrue(byteIndex < 4, $"Byte index {byteIndex} out of range");
            
            ref var writeTargetAsInt = ref UnsafeUtility.As<float, uint>(ref writeTarget);
            WriteToInt(ref writeTargetAsInt, byteIndex, valueToWrite);
        }*/

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort Read16bitsFromInt(int readTarget, byte bitIndex)
        {
            Assert.IsTrue(bitIndex < 16); //, $"Bit index {bitIndex} out of range");
            // Shift the target to the right so the desired bits are in the rightmost position
            readTarget >>= bitIndex;
            // Mask the target so only the desired bits are left
            return (ushort) (readTarget & 0xFFFF);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write16bitsToInt(ref int writeTarget, byte bitIndex, ushort valueToWrite)
        {
            Assert.IsTrue(bitIndex < 16); //, $"Bit index {bitIndex} out of range");
            // Shift the value to the left so it's in the 'right' position (HAHAHAHA)
            int valueAsShiftedInt = valueToWrite << bitIndex;
            // Mask the target so the bits that will be written are zeroed out
            writeTarget &= ~(0xFFFF << bitIndex);
            // Write the value to the target
            writeTarget |= valueAsShiftedInt;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Convert_2bytes_To_4BitValues_AsByte(byte byte1, byte byte2)
        {
            // Pre round the integer values so the 4 bit representation is more accurate
            byte byte1PreRounded = (byte) math.min(byte1 + ByteBitCount, byte.MaxValue);
            byte byte2PreRounded = (byte) math.min(byte2 + ByteBitCount, byte.MaxValue);
                
            // Divide 8bit range into 4bit range
            byte byte1_4Bit = (byte) (byte1PreRounded / FourBitMaxValue);
            byte byte2_4Bit = (byte) (byte2PreRounded / FourBitMaxValue);
                
            // Pack both 4 bit values into a single byte
            return (byte) ((byte1_4Bit << 4) | byte2_4Bit);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (byte byte1, byte byte2) Convert_4BitValues_ToByte2_FromByte(byte packedByte)
        {
            byte byte1_4Bit = (byte) (packedByte >> 4);
            byte byte2_4Bit = (byte) (packedByte & 0x0F);
            
            // Multiply 4bit range into 8bit range
            byte byte1 = (byte) (byte1_4Bit * FourBitMaxValue);
            byte byte2 = (byte) (byte2_4Bit * FourBitMaxValue);
            
            return (byte1, byte2);
        }

        ///<summary> Convert a value made up of any number of bits (up to 32) to a floating point value between 0 and 1. <br/>
        /// Has fewer options to make it faster, use 64bits version for more control. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Convert_UpTo32BitsValue_To_Float01(uint value, byte bitCount)
        {
            var maximumPossibleValue = uint.MaxValue >> (32 - bitCount);
            return value / (float) maximumPossibleValue;
        }

        ///<summary> Convert a value made up of any number of bits (up to 64) to a floating point value between 0 and 1. </summary>
        ///<param name="maxValue"> Specify the maximum value range. Will be auto clamped by max possible value derived from bitCount. Min value range always 0. </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Convert_UpTo64BitsValue_To_Float01(ulong value, byte bitCount, ulong maxValue = ulong.MaxValue)
        {
            var maximumPossibleValue = ulong.MaxValue >> (64 - bitCount);
            var clampedMaxValue = math.min(maxValue, maximumPossibleValue);
            var valueClamped = math.clamp(value, 0, clampedMaxValue);
            
            return valueClamped / (double) clampedMaxValue;
        }

        ///<summary> Convert a floating point value between 0 and 1 to a value made up of any number of bits (up to 32). <br/>
        /// Has fewer options to make it faster, use 64bits version for more control. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Convert_Float01_To_UpTo32BitsValue(float value, byte bitCount)
        {
            var maximumPossibleValue = uint.MaxValue >> (32 - bitCount);
            return (uint) math.round(value * maximumPossibleValue);
        }
        
        ///<summary> Convert a floating point value between 0 and 1 to a value made up of any number of bits (up to 64). </summary>
        ///<param name="maxValue"> Specify the maximum value range. Will be auto clamped by max possible value derived from bitCount. Min value range always 0. </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Convert_Float01_To_UpTo64BitsValue(double value, byte bitCount, ulong maxValue = ulong.MaxValue)
        {
            var maximumPossibleValue = ulong.MaxValue >> (64 - bitCount);
            var clampedMaxValue = math.min(maxValue, maximumPossibleValue);
            var valueClamped = math.clamp(value, 0, 1);
            
            return (ulong) (valueClamped * clampedMaxValue + .5f);
        }
    }
}