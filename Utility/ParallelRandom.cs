using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Random = Unity.Mathematics.Random;

namespace VLib
{
    /// <summary>
    /// Thread-safe random source with lock-free call paths. <br/>
    /// Each call uses an atomically incremented index hashed through <see cref="Random.CreateFromIndex(uint)"/>. <br/>
    /// To use advanced Unity.Mathematics.Random features, use <see cref="NextRandom"/> directly.
    /// </summary>
    public class ParallelRandom
    {
        // Per-instance salt so separate ParallelRandom instances do not produce identical sequences.
        readonly uint instanceSalt;

        // Incremented for every random request. Interlocked wraparound is intentional.
        int requestCounter;

        static int globalInstanceCounter = Environment.TickCount;

        public ParallelRandom()
        {
            // Hash the instance id once. Request indices are XOR'd with this salt.
            var instanceId = unchecked((uint)Interlocked.Increment(ref globalInstanceCounter));
            instanceSalt = WangHash(instanceId + 0x9E3779B9u);
        }

        /// <summary> Creates a new random instance with a fresh seed. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Random NextRandom()
        {
            var rawIndex = unchecked((uint)Interlocked.Increment(ref requestCounter)) ^ instanceSalt;
            // Random.CreateFromIndex requires index != uint.MaxValue.
            if (rawIndex == uint.MaxValue)
                rawIndex = 0;
            return Random.CreateFromIndex(rawIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint WangHash(uint value)
        {
            // Same hash family used by Unity.Mathematics.Random.CreateFromIndex internals.
            value = (value ^ 61u) ^ (value >> 16);
            value *= 9u;
            value ^= value >> 4;
            value *= 0x27d4eb2du;
            value ^= value >> 15;
            return value;
        }

        ///<summary> Min is 0, max is 1. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float NextFloat01() => NextFloat(0, 1);

        ///<summary> Min is 0, define max. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float NextFloat(float max) => NextFloat(0, max);

        ///<summary> Min is inclusive, max is exclusive. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float NextFloat(float min, float max) => NextRandom().NextFloat(min, max);

        ///<summary> Min is inclusive, max is exclusive. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double NextDouble(double min, double max) => NextRandom().NextDouble(min, max);

        ///<summary> Randomly returns 0 or 1. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int NextInt01() => NextInt(0, 2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int NextInt(int maxExclusive) => NextInt(0, maxExclusive);

        ///<summary> Min is inclusive, max is exclusive. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int NextInt(int min, int maxExclusive) => NextRandom().NextInt(min, maxExclusive);

        /// <summary> Returns a random byte value. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte NextByte() => (byte)NextInt(0, 256);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool NextBool() => NextInt01() > 0;

        /// <summary> Evenly selects between two options. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Next<T>(T optionA, T optionB) => NextBool() ? optionA : optionB;
    }
}
