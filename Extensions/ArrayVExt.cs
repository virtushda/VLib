using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace VLib
{
    public static class ArrayVExt
    {
        public static T Find<T>(this T[] array, Predicate<T> match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match), "Predicate is NULL!");
            for (int i = 0; i < array.Length; ++i)
            {
                if (match(array[i]))
                    return array[i];
            }

            return default(T);
        }

        /// <summary> List-like add behaviour for arrays. Will resize the array if necessary. </summary>
        /// <param name="array">The array to add to.</param>
        /// <param name="arrayCount">The current count of the array. Will be incremented.</param>
        /// <param name="element">The element to add.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add<T>(this T[] array, ref int arrayCount, T element)
        {
            int arrLength = array.Length;
            if (arrayCount >= arrLength)
                Array.Resize(ref array, arrLength * 2);

            array[arrayCount] = element;
            ++arrayCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add<T>(this T[] array, T element)
        {
            Array.Resize(ref array, array.Length + 1);

            array[array.Length - 1] = element;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddNoDuplicate<T>(this T[] array, T element)
        {
            if (element == null)
                return;
            foreach (var e in array)
            {
                if (e.Equals(element))
                    return;
            }

            array.Add(element);
        }

        /// <summary>Safe method to attempt reading from an array of unknown length, or that could be null!</summary>
        public static bool TryGetValue<T>(this T[] array, int index, out T value)
        {
            if (array == null || array.Length < 1)
            {
                value = default;
                return false;
            }

            int validIndex = math.clamp(index, 0, array.Length - 1);
            if (index == validIndex)
            {
                value = array[index];
                return true;
            }

            value = default;
            return false;
        }

        public static bool AllValuesNull<T>(this T[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] != null)
                    return false;
            }

            return true;
        }

        public static void SetAllValues<T>(this T[] array, T value)
        {
            if (array is not {Length: > 0})
                return;
            for (int i = 0; i < array.Length; i++)
                array[i] = value;
        }

        public static long MemoryFootprintBytes<T>(this T[] array)
            where T : struct
            => UnsafeUtility.SizeOf(typeof(T)) * array.LongLength;

        public static long MemoryFootprintBytesManaged<T>(this T[] arrayOfManaged, bool tryGetFootprintOfElements = false)
        {
            long footprint = 0;
            // Array footprint
            footprint += Marshal.SizeOf(IntPtr.Zero) * arrayOfManaged.Length;

            if (tryGetFootprintOfElements && arrayOfManaged.Length > 0 && arrayOfManaged[0] is IMemoryReporter)
            {
                for (int i = 0; i < arrayOfManaged.Length; i++)
                {
                    footprint += ((IMemoryReporter) arrayOfManaged[i]).ReportBytes();
                }
            }

            return footprint;
        }

        /// <summary> Do NOT use this on non-serializable objects! I would constrain this method, but I cannot. </summary> 
        public static T[] DeepCloneSerializableObjects<T>(this T[] array)
        {
            using var stream = new System.IO.MemoryStream();
            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            formatter.Serialize(stream, array);
            stream.Position = 0;
            return (T[]) formatter.Deserialize(stream);
        }

        public static ulong Sum(this ulong[] array)
        {
            var sum = 0ul;
            foreach (var value in array)
                sum += value;
            return sum;
        }

        /// <summary> Efficiently compute SHA512 hash of array of unmanaged types. Unmanaged allows zero allocations. </summary>
        /// <param name="array">Array of unmanaged types to hash.</param>
        /// <param name="hash">Output hash as a base64 string.</param>
        /// <returns>True if hash was computed successfully, false otherwise.</returns>
        public static bool TryComputeSHA512Hash<T>(this T[] array, out string hash)
            where T : unmanaged
        {
            return array.AsSpan().TryComputeSHA512Hash(out hash);
        }
    }
}