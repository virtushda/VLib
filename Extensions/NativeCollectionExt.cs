#define DEBUGJAHBS

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Jobs;
using Debug = UnityEngine.Debug;

namespace VLib
{
    public static class NativeCollectionExt
    {
    #region Native Array

    public static bool TryGetValue<T>(this NativeArray<T> nativeArray, int index, out T value)
        where T : struct
    {
        var isValidIndex = nativeArray.IsValidIndex(index);
        value = isValidIndex ? nativeArray[index] : default;
        return isValidIndex;
    }

    public static unsafe bool TryGetValueReadOnly<T>(this NativeArray<T> nativeArray, int index, out T value)
        where T : struct
    {
        var isValidIndex = nativeArray.IsValidIndex(index);
        value = isValidIndex ? UnsafeUtility.ReadArrayElement<T>(nativeArray.GetUnsafeReadOnlyPtr(), index) : default;
        return isValidIndex;
    }

    public static unsafe bool TryGetValueBypassSafety<T>(this NativeArray<T> nativeArray, int index, out T value)
        where T : struct
    {
        var isValidIndex = nativeArray.IsValidIndex(index);
        value = isValidIndex ? UnsafeUtility.ReadArrayElement<T>(nativeArray.ForceGetUnsafePtrNOSAFETY(), index) : default;
        return isValidIndex;
    }

    public static bool IsValidIndex<T>(this NativeArray<T> nativeArray, int index)
        where T : struct =>
        index > -1 && index < nativeArray.Length;

    public static void DisposeSafe<T>(this ref NativeArray<T> nativeArray)
            where T : struct
        {
            if (nativeArray != null && nativeArray.IsCreated)
                nativeArray.Dispose();
        }

    public static void DisposeSafe<T>(this ref NativeReference<T> nativeRef)
        where T : unmanaged
    {
        if (nativeRef != null && nativeRef.IsCreated)
            nativeRef.Dispose();
    }
        /*public static bool ArraysEqualBurst<T>(ref this NativeArray<T> array, ref NativeArray<T> otherArray)
            where T : unmanaged, IEquatable<T>
        {
            if (array.IsCreated == false)
            {
                Debug.LogError($"NativeArray<{nameof(T)}>.Equals ERROR: First array is not created!");
                return false;
            }

            if (otherArray.IsCreated == false)
            {
                Debug.LogError($"NativeArray<{nameof(T)}>.Equals ERROR: Second array is not created!");
                return false;
            }

            if (array.Length != otherArray.Length)
                return false;

            //Otherwise, compare arrays directly in threaded burst jahb!
            //Assume true, job will try to prove it isn't true.
            var results = new NativeArray<bool>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            results[0] = true;

            EqualsQueryJob<T> jahb = new EqualsQueryJob<T>(array, otherArray, results);
            jahb.ScheduleBatch(array.Length, 1024).Complete();

            bool result = results[0];
            results.Dispose();
            return result;
        }

        [BurstCompile]
        public struct EqualsQueryJob<T> : IJobParallelForBatch
            where T : unmanaged, IEquatable<T>
        {
            [ReadOnly] NativeArray<T> array1;
            [ReadOnly] NativeArray<T> array2;
            [NativeDisableParallelForRestriction, NativeDisableContainerSafetyRestriction] NativeArray<bool> result;

            public EqualsQueryJob(NativeArray<T> array1, NativeArray<T> array2, NativeArray<bool> result)
            {
                this.array1 = array1;
                this.array2 = array2;
                this.result = result;
            }

            public void Execute(int startIndex, int count)
            {
                //Skip if already found to be unequal
                if (!result[0])
                    return;

                for (int i = startIndex; i < startIndex + count; i++)
                {
                    if (!array1[i].Equals(array2[i]))
                    {
                        result[0] = false;
                        return;
                    }
                }
            }
        }*/

        public interface IIterationAction<T>
            where T : unmanaged
        {
            void Execute(int index, T singleValue);
        }

        public interface IIterationActionReturn<T>
            where T : unmanaged
        {
            T Execute(int index, T singleValue);
        }

        public interface IVectIterationAction<T, V> : IIterationAction<T>
            where T : unmanaged
            where V : unmanaged
        {
            void ExecuteVectorized(int index, V vectorizedValue);
        }

        /// <summary> Be careful with type alignment! </summary>
        /// <param name="array"></param>
        /// <param name="action"></param>
        /// <typeparam name="TSingle"></typeparam>
        /// <typeparam name="TVec"></typeparam>
        /// <typeparam name="TAction"></typeparam>
        [GenerateTestsForBurstCompatibility]
        public static void IterateVectorized<TSingle, TVec, TAction>(this ref NativeArray<TSingle> array, ref TAction action)
            where TSingle : unmanaged
            where TVec : unmanaged
            where TAction : struct, IVectIterationAction<TSingle, TVec>
        {
            if (!array.IsCreated)
            {
                Debug.LogError("Attempted to Iterate an uncreated native array...");
                return;
            }

            int vecArrayLength4 = array.Length / 4;
            int vecArrayLengthSingle = vecArrayLength4 * 4;

            if (vecArrayLength4 > 0)
            {
                //Read Vectorized
                NativeArray<TVec> vectorizedView = array.GetSubArray(0, vecArrayLengthSingle).Reinterpret<TVec>(UnsafeUtility.SizeOf<TSingle>());
                for (int i = 0; i < vecArrayLength4; i++)
                    action.ExecuteVectorized(i, vectorizedView[i]);

                //Read Last Few
                for (int i = vecArrayLength4 * 4; i < array.Length; i++)
                    action.Execute(i, array[i]);
            }
            else
            {
                //Read Last Few
                for (int i = 0; i < array.Length; i++)
                    action.Execute(i, array[i]);
            }

            //Read Vectorized
            /*for (i = 0; i + 3 < array.Length; i += 4)
                action.ExecuteVectorized(i, array.ReinterpretLoad<TVec>(i));*/

            //Read Last Few
            /*for (; i < array.Length; i++)
                action.Execute(i, array[i]);*/
        }

    #endregion

    #region Native List

        public static void DisposeSafe<T>(this ref NativeList<T> nativeArray)
            where T : unmanaged
        {
            if (nativeArray.IsCreated)
                nativeArray.Dispose();
        }
        
        /// <summary> Resize, shift elements to the right of the insertion point, insert </summary>
        public static void Insert<T>(this NativeList<T> list, int startIndex, T valueToAdd)
            where T : unmanaged
        {
            if (startIndex < 0)
                startIndex = ~startIndex;

            int endIndex = startIndex + 1;

            //If at end, add
            if (list.Length <= startIndex)
                list.Add(valueToAdd);
            else
            {
                //Make roooooom
                list.InsertRangeWithBeginEnd(startIndex, endIndex);
                //Insert
                list[startIndex] = valueToAdd;
            }
        }

        public static void Insert<T>(this NativeList<T> list, int startIndex, NativeArray<T> valuesToAdd)
            where T : unmanaged
        {
            //Make Rooom
            list.InsertRangeWithBeginEnd(startIndex, startIndex + valuesToAdd.Length);
            //Copy Chunk
            NativeArray<T>.Copy(valuesToAdd, 0, list.AsArray(), startIndex, valuesToAdd.Length);
        }

        public static void RemoveElementsSwapback<T>(ref this NativeList<T> list, NativeArray<T> elementsToRemove)
            where T : unmanaged, IEquatable<T>
        {
            RemoveElementsSwapbackJob<T> jab = new RemoveElementsSwapbackJob<T>(ref list, ref elementsToRemove);
            jab.Run();
        }

        public static void FindAllMatchingElements<T>(this NativeList<T> list, T element, NativeList<int> matchingElements)
            where T : unmanaged, IEquatable<T>
        {
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i].Equals(element))
                    matchingElements.Add(i);
            }
        }

        /// <summary> WARNING - NO SAFETY: Does not check list size or state. </summary>
        public static T PopUnsafe<T>(this NativeList<T> list)
            where T : unmanaged, IEquatable<T>
        {
            int index = list.Length - 1;
            T element = list[index];
            list.RemoveAt(index);
            return element;
        }

        /// <summary> No range checks, will pop first list element. </summary>
        public static T PopFirstUnsafe<T>(this NativeList<T> list)
            where T : unmanaged, IEquatable<T>
        {
            T element = list[0];
            list.RemoveAt(0);
            return element;
        }

        public static JobHandle StartStripDefaultValuesJob<T>(this NativeList<T> list) 
            where T : unmanaged, IEquatable<T>
        {
            return new StripDefaultValuesJob<T> { list = list }.Schedule();
        }

        public static JobHandle StartRemoveIndicesFromHashJob<T>(this NativeList<T> list, UnsafeParallelHashSet<int> indicesHash)
            where T : unmanaged
        {
            return new RemoveHashIndicesFromListJob<T> { list = list, indicesHash = indicesHash }.Schedule();
        }

        public static JobHandle StartRemoveHashedValuesJob<T>(this NativeList<T> list, NativeParallelHashSet<T> toRemove)
            where T : unmanaged, IEquatable<T>
        {
            return new RemoveHashedElementsSwapbackJob<T>(list, toRemove).Schedule();
        }
        
        public static void Swap<T>(this NativeList<T> list, int indexA, int indexB) 
            where T : unmanaged
        {
            if (math.min(indexA, indexB) < 0)
            {
                Debug.LogError("NativeList.Swap error, at least one index was less than zero!");
                return;
            }

            var valueB = list[indexB];
            list[indexB] = list[indexA];
            list[indexA] = valueB;
        }
        
        /// <summary>Enforces fitting length, then write value. Treats the list like an array of unlimited size.</summary> 
        public static void Write<T>(this NativeList<T> list, int index, T value) 
            where T : unmanaged
        {
            if (list.Length <= index)
                list.Length = index + 1;
            list[index] = value;
        }
        
        public static long MemoryFootprintBytes<T>(this VUnsafeList<T> list)
            where T : unmanaged
        {
            return list.IsCreated ? list.Capacity * UnsafeUtility.SizeOf<T>() : 0;
        }

    #endregion

    #region Native List Sorted

        public static bool ContainsSorted<T>(this NativeList<T> sortedList, T value)
            where T : unmanaged, IComparable<T>
        {
            return sortedList.BinarySearch(value) is var index && index >= 0;
        }

        public static bool ContainsSorted<T>(this ref UnsafeList<T> sortedList, T value)
            where T : unmanaged, IComparable<T>
        {
            return sortedList.BinarySearch(value) is var index && index >= 0;
        }

        /// <summary> Resize, shift elements to the right of the insertion point, insert </summary>
        public static void Insert<T>(this ref UnsafeList<T> sortedList, int startIndex, T valueToAdd)
            where T : unmanaged
        {
            if (startIndex < 0)
                startIndex = ~startIndex;

            int endIndex = startIndex + 1;

            //If at end, add
            if (sortedList.Length <= startIndex)
                sortedList.Add(valueToAdd);
            else
            {
                //Make roooooom
                sortedList.InsertRangeWithBeginEnd(startIndex, endIndex);
                //Insert
                sortedList[startIndex] = valueToAdd;
            }
        }

        public static void AddRangeSorted<T>(this NativeList<T> sortedList, NativeSlice<T> valuesToAdd)
            where T : unmanaged, IComparable<T>
        {
            sortedList.Capacity = math.max(sortedList.Capacity, sortedList.Length + valuesToAdd.Length);
            for (int i = 0; i < valuesToAdd.Length; i++)
                sortedList.AddSorted(valuesToAdd[i], out _);
        }

        public static void AddRangeSorted<T>(this NativeList<T> sortedList, UnsafeList<T> valuesToAdd)
            where T : unmanaged, IComparable<T>
        {
            sortedList.Capacity = math.max(sortedList.Capacity, sortedList.Length + valuesToAdd.Length);
            for (int i = 0; i < valuesToAdd.Length; i++)
                sortedList.AddSorted(valuesToAdd[i], out _);
        }

        public static void AddRangeSorted<T>(this ref UnsafeList<T> sortedList, NativeSlice<T> valuesToAdd)
            where T : unmanaged, IComparable<T>
        {
            sortedList.Capacity = math.max(sortedList.Capacity, sortedList.Length + valuesToAdd.Length);
            for (int i = 0; i < valuesToAdd.Length; i++)
                sortedList.AddSorted(valuesToAdd[i], out _);
        }

        public static void AddRangeSortedBurst<T>(this NativeList<T> sortedList, NativeSlice<T> valuesToAdd, bool exclusive)
            where T : unmanaged, IComparable<T>
        {
            if (valuesToAdd.Length < 1)
                return;
            var jahb = new AddRangeSortedJob<T>(valuesToAdd, sortedList, exclusive);
            jahb.Schedule().Complete();
        }

        public static void AddRangeSortedExclusive<T>(this NativeList<T> sortedList, NativeSlice<T> valuesToAdd)
            where T : unmanaged, IComparable<T>
        {
            sortedList.Capacity = math.max(sortedList.Capacity, sortedList.Length + valuesToAdd.Length);
            for (int i = 0; i < valuesToAdd.Length; i++)
                sortedList.AddSortedExclusive(valuesToAdd[i], out _);
        }

        public static void AddRangeSortedExclusive<T>(this ref UnsafeList<T> sortedList, NativeSlice<T> valuesToAdd)
            where T : unmanaged, IComparable<T>
        {
            sortedList.Capacity = math.max(sortedList.Capacity, sortedList.Length + valuesToAdd.Length);
            for (int i = 0; i < valuesToAdd.Length; i++)
                sortedList.AddSortedExclusive(valuesToAdd[i], out _);
        }

        public static void AddSorted<T>(this NativeList<T> sortedList, T valueToAdd, out int index)
            where T : unmanaged, IComparable<T>
        {
            index = sortedList.BinarySearch(valueToAdd);
            index = index >= 0 ? index : ~index;
            sortedList.Insert(index, valueToAdd);
        }

        public static void AddSorted<T>(this ref UnsafeList<T> sortedList, T valueToAdd, out int index)
            where T : unmanaged, IComparable<T>
        {
            index = sortedList.BinarySearch(valueToAdd);
            index = index >= 0 ? index : ~index;
            sortedList.Insert(index, valueToAdd);
        }

        /// <summary> Adds to a sorted list, but only if the element does not already exist by the logic defined in 'T's IComparableT. </summary>
        /// <returns>True if added</returns>
        public static bool AddSortedExclusive<T>(this NativeList<T> sortedList, T valueToAdd, out int index)
            where T : unmanaged, IComparable<T>
        {
            index = sortedList.BinarySearch(valueToAdd);
            if (index < 0)
            {
                sortedList.Insert(index, valueToAdd);
                return true;
            }

            return false;
        }

        /// <summary> Adds to a sorted list, but only if the element does not already exist by the logic defined in 'T's IComparableT. </summary>
        /// <returns>True if added</returns>
        public static bool AddSortedExclusive<T>(this ref UnsafeList<T> sortedList, T valueToAdd, out int index)
            where T : unmanaged, IComparable<T>
        {
            index = sortedList.BinarySearch(valueToAdd);
            if (index < 0)
            {
                sortedList.Insert(index, valueToAdd);
                return true;
            }

            return false;
        }

        public static int BinarySearch<T, U>(this NativeList<T> sortedList, U comparison)
            where T : unmanaged, IComparable<T>
            where U : struct, IComparison<T>
        {
            //Copied from NativeSort.BinarySearch extension method for NativeList
            var offset = 0;

            for (var l = sortedList.Length; l != 0; l >>= 1)
            {
                var idx = offset + (l >> 1);
                var curr = sortedList[idx];

                var r = comparison.CompareTo(curr);
                if (r == 0)
                    return idx;

                if (r > 0)
                {
                    offset = idx + 1;
                    --l;
                }
            }

            return ~offset;
        }

        public static int BinarySearch<T, U>(this NativeList<T> sortedList, U comparison, int start, int count)
            where T : unmanaged, IComparable<T>
            where U : struct, IComparison<T>
        {
            //Copied from NativeSort.BinarySearch extension method for NativeList
            var searchStart = start;

            for (var searchSize = count; searchSize != 0; searchSize >>= 1)
            {
                var idx = searchStart + (searchSize >> 1);
                var curr = sortedList[idx];

                var r = comparison.CompareTo(curr);
                if (r == 0)
                    return idx;

                if (r > 0)
                {
                    searchStart = idx + 1;
                    --searchSize;
                }
            }

            return ~searchStart;
        }

        public static int BinarySearch<T, U>(this ref UnsafeList<T> sortedList, U comparison)
            where T : unmanaged, IComparable<T>
            where U : struct, IComparison<T>
        {
            //Copied from NativeSort.BinarySearch extension method for NativeList
            var offset = 0;

            for (var l = sortedList.Length; l != 0; l >>= 1)
            {
                var idx = offset + (l >> 1);
                var curr = sortedList[idx];

                var r = comparison.CompareTo(curr);
                if (r == 0)
                    return idx;

                if (r > 0)
                {
                    offset = idx + 1;
                    --l;
                }
            }

            return ~offset;
        }

        public static int IndexOfFirstMatching<T>(this NativeList<T> sortedList, int index)
            where T : unmanaged, IComparable<T>
        {
            if (index <= 0 || index >= sortedList.Length)
                return index;

            var comparable = sortedList[index];

            for (int i = index - 1; i >= 0; i--)
            {
                if (comparable.CompareTo(sortedList[i]) != 0)
                    return i + 1;
            }

            return 0;
        }

        public static int IndexOfFirstMatching<T>(this ref UnsafeList<T> sortedList, int index)
            where T : unmanaged, IComparable<T>
        {
            if (index <= 0 || index >= sortedList.Length)
                return index;

            var comparable = sortedList[index];

            for (int i = index - 1; i >= 0; i--)
            {
                if (comparable.CompareTo(sortedList[i]) != 0)
                    return i + 1;
            }

            return 0;
        }

        public static int IndexOfFirstMatching<T>(this NativeList<T> sortedList, T comparable)
            where T : unmanaged, IComparable<T>
            => IndexOfFirstMatching(sortedList, sortedList.BinarySearch(comparable));

        public static int IndexOfFirstMatching<T>(this ref UnsafeList<T> sortedList, T comparable)
            where T : unmanaged, IComparable<T>
            => IndexOfFirstMatching(ref sortedList, sortedList.BinarySearch(comparable));

        public static int IndexOfLastMatching<T>(this NativeList<T> sortedList, int index)
            where T : unmanaged, IComparable<T>
        {
            if (index < 0 || index >= sortedList.Length)
                return index;

            var comparable = sortedList[index];

            for (int i = index + 1; i < sortedList.Length; i++)
            {
                if (comparable.CompareTo(sortedList[i]) != 0)
                    return i - 1;
            }

            return sortedList.Length - 1;
        }

        public static int IndexOfLastMatching<T>(this ref UnsafeList<T> sortedList, int index)
            where T : unmanaged, IComparable<T>
        {
            if (index < 0 || index >= sortedList.Length)
                return index;

            var comparable = sortedList[index];

            for (int i = index + 1; i < sortedList.Length; i++)
            {
                if (comparable.CompareTo(sortedList[i]) != 0)
                    return i - 1;
            }

            return sortedList.Length - 1;
        }

        public static int IndexOfLastMatching<T>(this NativeList<T> sortedList, T comparable)
            where T : unmanaged, IComparable<T>
            => IndexOfLastMatching(sortedList, sortedList.BinarySearch(comparable));

        public static int IndexOfLastMatching<T>(this ref UnsafeList<T> sortedList, T comparable)
            where T : unmanaged, IComparable<T>
            => IndexOfLastMatching(ref sortedList, sortedList.BinarySearch(comparable));

        /// <summary> Only works on a sorted collection. Binary searches to find a match, then checks for the range.
        /// Range is [inclusive, inclusive]. </summary>
        public static bool FindIndexRangeOfMatchingSorted<T>(this NativeList<T> sortedList, T comparable, out int2 startEnd)
            where T : unmanaged, IComparable<T>
        {
            int index = sortedList.BinarySearch(comparable);

            if (index < 0 || index >= sortedList.Length)
            {
                startEnd = 0;
                return false;
            }

            int firstIndex = IndexOfFirstMatching(sortedList, index);
            int lastIndex = IndexOfLastMatching(sortedList, index);

            startEnd = new int2(firstIndex, lastIndex);
            return true;
        }

        /// <summary> Only works on a sorted collection. Binary searches to find a match, then checks for the range.
        /// Range is [inclusive, inclusive]. </summary>
        public static bool FindIndexRangeOfMatchingSorted<T>(this ref UnsafeList<T> sortedList, T comparable, out int2 startEnd)
            where T : unmanaged, IComparable<T>
        {
            int index = sortedList.BinarySearch(comparable);

            if (index < 0 || index >= sortedList.Length)
            {
                startEnd = 0;
                return false;
            }

            int firstIndex = sortedList.IndexOfFirstMatching(index);
            int lastIndex = sortedList.IndexOfLastMatching(index);

            startEnd = new int2(firstIndex, lastIndex);
            return true;
        }

        /// <summary> This is the preferred method if T implements IComparableT </summary>
        public static void ResortComparables<T>(this NativeList<T> sortedList)
            where T : unmanaged, IComparable<T>
        {
            var comparer = new ComparableComparer<T>();
            sortedList.Sort(comparer);
        }

        /// <summary> This is the preferred method if T implements IComparableT </summary>
        public static void ResortComparables<T>(this ref UnsafeList<T> sortedList)
            where T : unmanaged, IComparable<T>
        {
            var comparer = new ComparableComparer<T>();
            sortedList.Sort(comparer);
        }

        public static void PopulateWithUnsortedArray<T>(this NativeList<T> sortedList, NativeArray<T> unsortedArray)
            where T : unmanaged, IComparable<T>
        {
            sortedList.Clear();
            sortedList.AddRange(unsortedArray);
            sortedList.ResortComparables();
        }

        public static void PopulateWithUnsortedArray<T>(this ref UnsafeList<T> sortedList, UnsafeList<T> unsortedList)
            where T : unmanaged, IComparable<T>
        {
            sortedList.Clear();
            sortedList.AddRange(unsortedList);
            sortedList.ResortComparables();
        }

        /// <summary> Removes first entry that matches 'valueToRemove's by the logic of 'T's IComparableT. </summary>
        public static void RemoveSorted<T>(this NativeList<T> sortedList, T valueToRemove)
            where T : unmanaged, IComparable<T>
        {
            int index = sortedList.BinarySearch(valueToRemove);
            if (index >= 0)
                sortedList.RemoveAt(index);
        }

        /// <summary> Removes first entry that matches 'valueToRemove's by the logic of 'T's IComparableT. </summary>
        public static void RemoveSorted<T>(this ref UnsafeList<T> sortedList, T valueToRemove)
            where T : unmanaged, IComparable<T>
        {
            int index = sortedList.BinarySearch(valueToRemove);
            if (index >= 0)
                sortedList.RemoveAt(index);
        }

        public static bool RemoveValue<T>(this ref UnsafeList<T> list, T valueToRemove)
            where T : unmanaged, IEquatable<T>
        {
            var index = list.IndexOf(valueToRemove);
            if (index < 0) return false;
            
            list.RemoveAt(index);
            return true;
        }

        /// <summary>
        /// Remove the first object that matches the comparison!
        /// Be warned, an under-defined comparison could produce unexpected results!
        /// </summary>
        /// <param name="sortedList">List to remove from.</param>
        /// <param name="comparison">Struct with logic and data to compute a comparison.</param>
        /// <typeparam name="T">Type of </typeparam>
        /// <typeparam name="U"></typeparam>
        public static void RemoveSortedByComparison<T, U>(this NativeList<T> sortedList, U comparison)
            where T : unmanaged, IComparable<T>
            where U : struct, IComparison<T>
        {
            if (sortedList.BinarySearch(comparison) is int index && index >= 0)
                sortedList.RemoveAt(index);
        }

        /// <summary>
        /// Remove the first object that matches the comparison!
        /// Be warned, an under-defined comparison could produce unexpected results!
        /// </summary>
        /// <param name="sortedList">List to remove from.</param>
        /// <param name="comparison">Struct with logic and data to compute a comparison.</param>
        /// <typeparam name="T">Type of </typeparam>
        /// <typeparam name="U"></typeparam>
        public static void RemoveSortedByComparison<T, U>(this ref UnsafeList<T> sortedList, U comparison)
            where T : unmanaged, IComparable<T>
            where U : struct, IComparison<T>
        {
            if (sortedList.BinarySearch(comparison) is int index && index >= 0)
                sortedList.RemoveAt(index);
        }

    #endregion

    #region Native Hash

        public static void DisposeSafe<T, U>(this ref NativeParallelHashMap<T, U> nativeHashmap)
            where T : unmanaged, IEquatable<T>
            where U : unmanaged
        {
            if (nativeHashmap.IsCreated)
                nativeHashmap.Dispose();
        }

        public static void DisposeSafe<T, U>(this ref UnsafeParallelHashMap<T, U> nativeHashmap)
            where T : unmanaged, IEquatable<T>
            where U : unmanaged
        {
            if (nativeHashmap.IsCreated)
                nativeHashmap.Dispose();
        }

        public static void DisposeSafe<T>(this ref NativeParallelHashSet<T> nativeHashmap)
            where T : unmanaged, IEquatable<T>
        {
            if (nativeHashmap.IsCreated)
                nativeHashmap.Dispose();
        }

        public static void DisposeSafe<T>(this ref UnsafeParallelHashSet<T> unsafeHashMap)
            where T : unmanaged, IEquatable<T>
        {
            if (unsafeHashMap.IsCreated)
                unsafeHashMap.Dispose();
        }
        
        public static void DisposeSafe<T>(this ref UnsafeQueue<T> unsafeQueue)
            where T : unmanaged, IEquatable<T>
        {
            if (unsafeQueue.IsCreated)
                unsafeQueue.Dispose();
        }

        public static UnsafeParallelHashSet<K> KeySet<K, V>(this ref UnsafeParallelHashMap<K, V> map, Allocator allocator)
            where K : unmanaged, IEquatable<K>
            where V : unmanaged
        {
            var set = new UnsafeParallelHashSet<K>(map.Capacity, allocator);
            foreach (var keyValue in map)
            {
                set.Add(keyValue.Key);
            }
            return set;
        }
        
        public static UnsafeParallelHashSet<K> KeySet<K, V>(this ref UnsafeParallelHashMap<K, V>.ReadOnly map, Allocator allocator)
            where K : unmanaged, IEquatable<K>
            where V : unmanaged
        {
            var set = new UnsafeParallelHashSet<K>(map.Capacity, allocator);
            foreach (var keyValue in map)
            {
                set.Add(keyValue.Key);
            }
            return set;
        }

        public static HashSetGetKeysJob<T> GetKeysToArrayJob<T>(this ref NativeParallelHashSet<T> nativeHashSet, NativeArray<T> keysArray)
            where T : unmanaged, IEquatable<T>
        {
            return new HashSetGetKeysJob<T>(nativeHashSet, keysArray);
        }

        public static JobHandle StartAddRangeBurstJob<T>(this ref NativeParallelHashSet<T> hashSet, NativeSlice<T> addElements, JobHandle inDeps = default)
            where T : unmanaged, IEquatable<T>
        {
            return new HashSetAddRangeJob<T>(hashSet, addElements).Schedule(inDeps);
        }

        /// <returns>True if added, false if set.</returns>
        public static bool AddSet<T, U>(this ref NativeParallelHashMap<T, U> hashMap, T key, U value) 
            where T : unmanaged, IEquatable<T> 
            where U : unmanaged
        {
            if (!hashMap.TryAdd(key, value))
            {
                hashMap[key] = value;
                return false;
            }
            return true;
        }
        
        public static long MemoryFootprintBytes<T, U>(this NativeParallelHashMap<T, U> hashMap)
            where T : unmanaged, IEquatable<T>
            where U : unmanaged
        {
            return hashMap.IsCreated ? hashMap.Capacity * (UnsafeUtility.SizeOf<T>() + UnsafeUtility.SizeOf<U>()) : 0;
        }
        
        // Memory Footprint in bytes for UnsafeParallelHashMap
        public static long MemoryFootprintBytes<T, U>(this UnsafeParallelHashMap<T, U> hashMap)
            where T : unmanaged, IEquatable<T>
            where U : unmanaged
        {
            return hashMap.IsCreated ? hashMap.Capacity * (UnsafeUtility.SizeOf<T>() + UnsafeUtility.SizeOf<U>()) : 0;
        }
        
        // For NativeParallelHashSet
        public static long MemoryFootprintBytes<T>(this NativeParallelHashSet<T> hashSet)
            where T : unmanaged, IEquatable<T>
        {
            return hashSet.IsCreated ? hashSet.Capacity * UnsafeUtility.SizeOf<T>() : 0;
        }
        
        // For UnsafeParallelHashSet
        public static long MemoryFootprintBytes<T>(this UnsafeParallelHashSet<T> hashSet)
            where T : unmanaged, IEquatable<T>
        {
            return hashSet.IsCreated ? hashSet.Capacity * UnsafeUtility.SizeOf<T>() : 0;
        }

        #endregion

    #region Native Multi Hash

        public static void DisposeSafe<T, U>(this ref NativeParallelMultiHashMap<T, U> nativeHashmap)
            where T : unmanaged, IEquatable<T>
            where U : unmanaged
        {
            if (nativeHashmap.IsCreated)
                nativeHashmap.Dispose();
        }

    #endregion
    
    #region Native Queue
    
    public static void DisposeSafe<T>(this NativeQueue<T> queue)
        where T : unmanaged
    {
        if (queue.IsCreated)
            queue.Dispose();
    }
    
    #endregion

        public static void DisposeSafe(ref this TransformAccessArray transformAccessArray)
        {
            if (transformAccessArray.isCreated)
                transformAccessArray.Dispose();
        }
    }
    
    #region JOBS

    [BurstCompile]
    public struct RemoveHashedElementsSwapbackJob<T> : IJob
        where T : unmanaged, IEquatable<T>
    {
        NativeList<T> list;
        NativeParallelHashSet<T> toRemove;

        public RemoveHashedElementsSwapbackJob(NativeList<T> list, NativeParallelHashSet<T> toRemove)
        {
            this.list = list;
            this.toRemove = toRemove;
        }

        public void Execute()
        {
            if (toRemove.IsEmpty)
                return;
            
            //Check and Remove From List
            for (int i = 0; i < list.Length; i++)
            {
                if (toRemove.Contains(list[i]))
                {
                    list.RemoveAtSwapBack(i);
                    i--;
                }
            }
        }
    }

    [BurstCompile]
        public struct RemoveElementsSwapbackJob<T> : IJob
            where T : unmanaged, IEquatable<T>
        {
            NativeList<T> list;
            [ReadOnly] NativeArray<T> toRemove;

            public RemoveElementsSwapbackJob(ref NativeList<T> list, ref NativeArray<T> toRemove)
            {
                this.list = list;
                this.toRemove = toRemove;
            }

            public void Execute()
            {
                //List version, stable but slower
                /*NativeList<T> removalList = new NativeList<T>(toRemove.Length, Allocator.Temp);
                for (int i = 0; i < toRemove.Length; i++)
                    removalList.Add(toRemove[i]);

                //Check and Remove From List
                for (int i = 0; i < list.Length; i++)
                {
                    bool remove = false;
                    for (int r = 0; r < removalList.Length; r++)
                    {
                        if (list[i].Equals(removalList[r]))
                        {
                            remove = true;
                            removalList.RemoveAtSwapBack(r);
                            break;
                        }
                    }

                    if (remove)
                    {
                        list.RemoveAtSwapBack(i);
                        i--;
                    }
                }*/

                //Hashset Version, Just doesn't work consistently, can't tell why
                //Populate hashset for efficient checks
                NativeParallelHashMap<T, byte> hashset = new NativeParallelHashMap<T, byte>(toRemove.Length, Allocator.Temp);
                for (int i = 0; i < toRemove.Length; i++)
                    hashset.TryAdd(toRemove[i], 0);

                //Check and Remove From List
                for (int i = 0; i < list.Length; i++)
                {
                    if (hashset.ContainsKey(list[i]))
                    {
                        list.RemoveAtSwapBack(i);
                        i--;
                    }
#if DEBUGJAHBS
                    else
                    {
                        Debug.LogError("List.RemoveElementsSwapbackJob failed!");
                    }
#endif
                }


                //Dispose
                //removalList.Dispose();
                hashset.Dispose();
            }
        }
        
        [BurstCompile]
        public struct StripDefaultValuesJob<T> : IJob 
            where T : unmanaged, IEquatable<T>
        {
            public NativeList<T> list;
            
            public void Execute()
            {
                var defaultValue = default(T);
                for (int i = list.Length - 1; i >= 0; i--)
                {
                    if (list[i].Equals(defaultValue))
                        list.RemoveAt(i);
                }
            }
        }

        [BurstCompile]
        public struct RemoveHashIndicesFromListJob<T> : IJob
            where T : unmanaged
        {
            public NativeList<T> list;
            [ReadOnly] public UnsafeParallelHashSet<int> indicesHash;

            public void Execute()
            {
                var indices = indicesHash.ToNativeArray(Allocator.Temp);
                //Indices must be sorted so elements don't get offset by earlier elements being removed
                indices.Sort();

                //Loop backwards to maintain element alignment
                for (int i = indices.Length - 1; i >= 0; i--)
                {
                    var removeIndex = indices[i];
                    if (list.Length > removeIndex)
                        list.RemoveAt(removeIndex);
                }
            }
        }
    
        [BurstCompile]
        struct AddRangeSortedJob<T> : IJob
            where T : unmanaged, IComparable<T>
        {
            [ReadOnly] NativeSlice<T> source;
            NativeList<T> dest;
            bool exclusive;

            public AddRangeSortedJob(NativeSlice<T> source, NativeList<T> dest, bool exclusive)
            {
                this.source = source;
                this.dest = dest;
                this.exclusive = exclusive;
            }
            
            public void Execute()
            {
                if (source.Length < 1)
                    return;
                
                if (exclusive)
                    dest.AddRangeSortedExclusive(source);
                else
                    dest.AddRangeSorted(source);
            }
        }

        [BurstCompile]
        public struct HashSetGetKeysJob<T> : IJob
            where T : unmanaged, IEquatable<T>
        {
            NativeParallelHashSet<T> hashSet;
            NativeArray<T> keysArray;

            public HashSetGetKeysJob(NativeParallelHashSet<T> hashSet, NativeArray<T> keysArray)
            {
                this.hashSet = hashSet;
                this.keysArray = keysArray;
            }

            public void Execute()
            {
                var tempKeysArray = hashSet.ToNativeArray(Allocator.Temp);
                keysArray.CopyFrom(tempKeysArray);
                tempKeysArray.Dispose();
            }
        }

        [BurstCompile]
        public struct HashSetAddRangeJob<T> : IJob
            where T : unmanaged, IEquatable<T>
        {
            [ReadOnly] NativeSlice<T> add;
            NativeParallelHashSet<T> hashset;

            public HashSetAddRangeJob(NativeParallelHashSet<T> hashset, NativeSlice<T> add)
            {
                this.add = add;
                this.hashset = hashset;
            }

            public void Execute()
            {
                for (int i = 0; i < add.Length; i++)
                {
                    hashset.Add(add[i]);
                }
            }
        }
        
    #endregion
}