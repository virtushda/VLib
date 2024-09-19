using Unity.Mathematics;

namespace VLib.Libraries.VLib.Unsafe.Utility
{
    public static unsafe class Vectorized
    {
        /// <summary> Can be used to check literal equality (no IEquatable) for any unmanaged type that is 4 bytes, sizeof(int). </summary>
        public static bool Contains_4ByteType(int* ptr, int length, int value)
        {
            var length4 = length / 4;
            var lengthRemainder = length % 4;

            // SIMD loop
            var workingPtr = ptr;
            for (int i = 0; i < length4; i++)
            {
                // Take next 4 values in one move
                var ptrValue4 = *(int4*)workingPtr;
                if (math.any(ptrValue4 == value))
                    return true;
                
                workingPtr += 4;
            }
            
            // Remainder loop
            for (int i = 0; i < lengthRemainder; i++)
            {
                if (*workingPtr == value)
                    return true;
                workingPtr++;
            }
            
            return false;
        }
    }
}