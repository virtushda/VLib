using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace VLib
{
    public static class ListTExt
    {
        public static bool TryGet<T>(this IList<T> list, int index, out T value, bool logErrors = false)
        {
            if (index < 0)
            {
                if (logErrors)
                    Debug.LogError($"Index '{index}' is below 0!");
                value = default;
                return false;
            }

            if (index >= list.Count)
            {
                if (logErrors)
                    Debug.LogError($"Index '{index}' is beyond the range of list of count {list.Count}!");
                value = default;
                return false;
            }

            if (list.Count < 1)
            {
                if (logErrors)
                    Debug.LogError("List is empty!");
                value = default;
                return false;
            }

            value = list[index];
            return true;
        }
        
        public static bool TryGetReadOnly<T>(this IReadOnlyList<T> readOnlyList, int index, out T value, bool logErrors = false)
        {
            if (index < 0)
            {
                if (logErrors)
                    Debug.LogError($"Index '{index}' is below 0!");
                value = default;
                return false;
            }

            if (index >= readOnlyList.Count)
            {
                if (logErrors)
                    Debug.LogError($"Index '{index}' is beyond the range of list of count {readOnlyList.Count}!");
                value = default;
                return false;
            }

            if (readOnlyList.Count < 1)
            {
                if (logErrors)
                    Debug.LogError("List is empty!");
                value = default;
                return false;
            }

            value = readOnlyList[index];
            return true;
        }
        
        public static T GetOrDefault<T>(this IReadOnlyList<T> list, int index, T defaultValue = default)
        {
            if (list == default || index < 0 || index >= list.Count)
                return defaultValue;
            return list[index];
        }
        
        public static bool Contains<T>(this IReadOnlyList<T> list, T value)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Equals(value))
                    return true;
            }
            return false;
        }

        public static T Pop<T>(this List<T> list)
        {
            int listCount = list.Count;
            if (listCount > 0)
            {
                int listCountMinus = listCount - 1;
                var element = list[listCountMinus];
                list.RemoveAt(listCountMinus);
                return element;
            }
            return default;
        }

        public static T PopFirst<T>(this List<T> list)
        {
            if (list.Count > 0)
            {
                var element = list[0];
                list.RemoveAt(0);
                return element;
            }
            return default;
        }

        public static bool TryPop<T>(this List<T> list, out T poppedValue)
        {
            if (list.Count == 0)
            {
                poppedValue = default;
                return false;
            }

            poppedValue = list.Pop();
            return true;
        }

        public static bool TryPopFirst<T>(this List<T> list, out T poppedValue)
        {
            if (list.Count == 0)
            {
                poppedValue = default;
                return false;
            }

            poppedValue = list.PopFirst();
            return true;
        }
        
        public static bool TryFind<T>(this List<T> list, Predicate<T> match, out T value)
        {
            int index = list.FindIndex(match);
            if (index >= 0)
            {
                value = list[index];
                return true;
            }
            value = default;
            return false;
        }
        
        public static T[] GetInternalArray<T>(this List<T> list)
        {
            return NoAllocHelpers.ExtractArrayFromListT(list);
        }
        
        public static T[] GetInternalArrayAndCount<T>(this List<T> list, out int listCount)
        {
            listCount = list.Count;
            return NoAllocHelpers.ExtractArrayFromListT(list);
        }

        //Modified version of c# Array.BinarySearch
        public static int BinarySearch<T>(this List<T> list, T value)
            where T : IComparable<T>
        {
            var array = list.GetInternalArray();
            int count = list.Count;

            int low = 0;
            int hi = count - 1;

            while (low <= hi)
            {
                int median = GetMedian(low, hi);
                int comparison = array[median].CompareTo(value);

                if (comparison < 0)
                    low = median + 1;
                else if (comparison > 0)
                    hi = median - 1;
                else
                    return median;
            }

            return ~low;
        }
        
        //Modified version of c# Array.BinarySearch
        public static int BinarySearch<T>(this List<T> list, Compare<T> comparison)
        {
            var array = list.GetInternalArray();
            int count = list.Count;

            int low = 0;
            int hi = count - 1;

            while (low <= hi)
            {
                int median = GetMedian(low, hi);
                int compareResult = comparison.Invoke(array[median]);

                if (compareResult < 0)
                    low = median + 1;
                else if (compareResult > 0)
                    hi = median - 1;
                else
                    return median;
            }

            return ~low;
        }
        
        public static bool FindBinary<T>(this List<T> list, Compare<T> comparison, out T value)
        {
            int index = list.BinarySearch(comparison);
            if (index >= 0)
            {
                value = list[index];
                return true;
            }

            value = default;
            return false;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetMedian(int low, int hi) => low + (hi - low >> 1);

        public static void CapacityToDefault<T>(this List<T> list, bool overwriteExisting)
        {
            //Existing elements to default
            if (overwriteExisting)
            {
                for (int i = 0; i < list.Count; i++)
                    list[i] = default;
            }

            //All Others in Capacity
            while (list.Count < list.Capacity)
                list.Add(default);
        }

        public static void Invoke(this IReadOnlyList<Action> listOfActions)
        {
            for (int i = 0; i < listOfActions.Count; i++)
                listOfActions[i]?.Invoke();
        }

        /// <summary> Shallow clone, creates a new list pointing to the same references. </summary>
        public static List<T> CloneContainer<T>(this List<T> list)
        {
            var clonedList = new List<T>(list.Count);
            clonedList.AddRange(list);
            return clonedList;
        }

        public static bool Overlaps<T>(this ICollection<T> collectionA, ICollection<T> collectionB, out int overlapCount)
        {
            overlapCount = 0;
            foreach (var t in collectionA)
            {
                if (collectionB.Contains(t))
                    overlapCount++;
            }
            return overlapCount > 0;
        }

        public static bool TryGetRandom<T>(this IList<T> list, out T value)
        {
            int listCount = list.Count;
            if (listCount < 1)
            {
                value = default;
                return false;
            }

            int randomIndex = Random.Range(0, listCount);
            value = list[randomIndex];
            return true;
        }
        
        public static unsafe bool TryGetRandom<T>(this IList<T> list, ref Unity.Mathematics.Random random, out T value)
        { 
            value = default;
            if (list == null)
                return false;
            
            var listCount = list.Count;
            if (listCount == 0)
                return false;

            var randomIndex = random.NextInt(listCount);
            value = list[randomIndex];
            return true;
        }

        /// <summary>
        /// Finds and removes the first value which satisfies a predicate.
        /// </summary>
        /// <remarks>
        /// The first value satisfying the predicate is overwritten by the last element of the list, and the list's length is decremented by one.
        /// </remarks>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to search.</param>
        /// <param name="matcher">The predicate for testing the elements of the list.</param>
        /// <param name="removedValue">The value that was removed.</param>
        /// <returns>Returns true if an element was removed.</returns>
        public static bool RemoveSwapBack<T>(this List<T> list, Predicate<T> matcher, out T removedValue)
        {
            int index = list.FindIndex(matcher);
            if (index < 0)
            {
                removedValue = default;
                return false;
            }
            removedValue = list[index];
            list.RemoveAtSwapBack(index);
            return true;
        }

        /// <summary> Very slow </summary>
        public static bool AddUnique<T>(this List<T> list, T value)
        {
            if (list.Contains(value))
                return false;
            list.Add(value);
            return true;
        }

        /// <summary> Very stupidly slow, don't use on any hot-path. </summary>
        public static bool AddRangeUnique<T>(this List<T> list, IEnumerable<T> values)
        {
            bool added = false;
            foreach (var value in values)
            {
                if (list.AddUnique(value))
                    added = true;
            }
            return added;
        }

        /// <summary> Linear search, slow. </summary>
        public static int IndexOf<T>(this IReadOnlyList<T> list, T value)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (value.Equals(list[i]))
                    return i;
            }
            return -1;
        }
        
        public static void EnsureCapacity<T>(this List<T> list, int capacity)
        {
            if (list.Capacity < capacity)
                list.Capacity = capacity;
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void ConditionalCheckIndexValid<T>(this IReadOnlyList<T> readOnlyList, int index)
        {
            if (index < 0 || index >= readOnlyList.Count)
                throw new IndexOutOfRangeException($"Index '{index}' is out of range of list of count {readOnlyList.Count}!");
        }
    } 
}