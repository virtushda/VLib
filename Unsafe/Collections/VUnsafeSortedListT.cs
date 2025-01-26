using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;

namespace VLib
{
    /// <summary>
    /// Native version of <see cref="VSortedList{T}"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [GenerateTestsForBurstCompatibility]
    public unsafe struct VUnsafeSortedList<T> : ICollection<T>, IReadOnlyList<T>, IVLibUnsafeContainer
        where T : unmanaged, IComparable<T>, IEquatable<T>
    {
        public VUnsafeList<T> list;

        public VUnsafeSortedList(int initialCapacity, Allocator allocator) => list = new VUnsafeList<T>(initialCapacity, allocator);

        /// <summary> Prefer extension 'DisposeRef' to be safe. </summary>
        public void Dispose() => list.Dispose();

        VUnsafeList<T>.ReadOnly AsReadOnly => list.AsReadOnly();

        /// <summary> To do an unsafe 'set', call <see cref="SetUnsafe"/> </summary>
        public T this[int i] => list[i];

        public void SetUnsafe(int index, T value) => list[index] = value;

        #region Interface Implementations
        
        public int Count => list.Count;

        bool ICollection<T>.IsReadOnly => false;
        
        public void CopyTo(T[] array, int arrayIndex) => VCollectionUtils.CopyFromTo(this, array, 0, arrayIndex, Count);
        
        #region IVLibUnsafeContainer

        public int Length
        {
            get => Count;
            set => list.Length = value;
        }

        public int Capacity
        {
            get => list.Capacity;
            set => list.Capacity = value;
        }

        public bool IsCreated => list.IsCreated;

        public void* GetUnsafePtr() => list.GetUnsafePtr();

        #endregion
        
        #endregion

        public bool Contains(T obj) => IndexOf(obj) >= 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => list.Clear();

        /// <summary> Binary searches for first 'comparably equal' element, then checks each possibly equal element for true equality. </summary>
        public int IndexOf(T item)
        {
            int searchResult = IndexOfComparableMatch(item);
            if (searchResult < 0)
                return searchResult;

            //If hit the exact object initially
            if (list[searchResult].Equals(item)) // Types need to implement proper equality checks for this to work without alloc
                return searchResult;

            //Else check valid range for true equality TODO OPT Search
            FindIndexRangeOfMatching(searchResult, out int2 searchRange);

            for (int i = searchRange.x; i <= searchRange.y; i++)
                if (list[i].Equals(item))
                    return i;

            //Return original search result as insertion value
            return ~searchResult;
        }

        /// <summary>Brute-force searches the entire list for an equal value</summary>
        public int IndexOfSlow(T item)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Equals(item))
                    return i;
            }
            return -1;
        }

        /// <summary> Can add duplicates, and anything an IComparer says is 'equal'. </summary>
        public void Add(T value)
        {
            int searchResult = IndexOfComparableMatch(value);
            list.Insert(searchResult >= 0 ? searchResult : ~searchResult, value);
        }

        /// <summary> Will only remove if an EXACT match is found. </summary>
        public bool Remove(T obj)
        {
            int searchResult = IndexOf(obj);

            if (searchResult < 0)
                return false;

            list.RemoveAt(searchResult);
            return true;
        }
        
        /// <summary> Will only remove if an EXACT match is found. Uses <see cref="IndexOfSlow"/> for value matching</summary>
        public bool RemoveSlow(T obj)
        {
            int searchResult = IndexOfSlow(obj);

            if (searchResult < 0)
                return false;

            list.RemoveAt(searchResult);
            return true;
        }

        public void RemoveAt(int index) => list.RemoveAt(index);

        public void ReorderAt(int index)
        {
            var item = list[index];
            list.RemoveAt(index);
            Add(item);
        }

        public void Resort() => list.ListData.Sort();

        /// <summary> Get and remove from collection. </summary>
        public T Pull(int index)
        {
            var value = list[index];
            list.RemoveAt(index);
            return value;
        }

        public void Insert(int index, T item)
        {
            EditorOnlyAssertValidInsertionIndex(index);
            list.Insert(index, item);
            EditorOnlyVerifyLocalKeyOrder(index);
        }

        public VUnsafeList<T>.Enumerator GetEnumerator() => list.GetEnumerator();
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        /// <summary> Much faster than .Contains, especially for struct/unmanaged types. But the IComparer must be water-tight! </summary>
        /// <param name="obj"></param>
        /// <returns> Returns true if the IComparable.Compare or IComparer implementations report a match. </returns>
        public bool ContainsMatch(T obj) => IndexOfComparableMatch(obj) >= 0;

        /// <summary> Finds the first "match". </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOfComparableMatch(T item) => list.BinarySearch(item);

        /*[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOfByComparison(Compare<T> comparison) => list.BinarySearch(comparison);*/

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsIndexValid(int index, bool throwException = false)
        {
            var count = list.Count;
            var countBad = count < 1;
            if (Hint.Unlikely(countBad))
            {
                string error = "Index out of range, list is empty!";
                if (throwException)
                    throw new IndexOutOfRangeException(error);
                else
                    Debug.LogError(error);
            }

            var indexBad = index < 0 || index >= count;
            if (Hint.Unlikely(indexBad))
            {
                var exception = new IndexOutOfRangeException($"Index '{index}' is out of range [0 - {list.Count - 1}]");
                if (throwException)
                    throw exception;
                else
                    Debug.LogException(exception);
                return false;
            }

            return true;
        }

        /// <summary> Follows same rules as 'Add'. </summary>
        public void AddRange<TCollection>(TCollection collection)
            where TCollection : IReadOnlyList<T>
        {
            for (int i = 0; i < collection.Count; i++)
                Add(collection[i]);
        }

        /// <summary> If the object is already in the collection, it will be rejected. </summary>
        public bool TryAddExclusive(T value) => TryAddExclusiveInternal(value, IndexOf(value));

        /// <summary> If the object is already in the collection OR an IComparer implementation decides the object already exists in this collection, it will be rejected. </summary>
        public bool TryAddExclusiveStrict(T value) => TryAddExclusiveInternal(value, IndexOfComparableMatch(value));

        bool TryAddExclusiveInternal(T value, int index)
        {
            if (index >= 0)
                return false;
            list.Insert(~index, value);
            return true;
        }

        /// <summary>
        /// Slightly faster, but will remove anything that 'matches', which can include an over-simplistic IComparer<typeparamref name="T"/> for instance!
        /// </summary>
        public bool RemoveFirstMatch(T obj)
        {
            int searchResult = IndexOfComparableMatch(obj);
            if (searchResult < 0)
                return false;

            list.RemoveAt(searchResult);
            return true;
        }

        [BurstDiscard]
        public void RemoveNonOverlapping(HashSet<T> hashset)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (!hashset.Contains(list[i]))
                {
                    RemoveAt(i--);
                }
            }
        }

        /*public void Resort()
        {
            Resort(HasComparer ? Comparer : DefaultComparer);

            /*for (int i = 1; i < list.Count; i++)
                {
                    var toSort = list[i];
                    int index = list.BinarySearch(0, i, toSort, defaultComparer);
                    list.RemoveAt(i);
                    list.Insert(index < 0 ? ~index : index, toSort);
                }#1#
        }

        public void Resort<U>(U customComparer)
            where U : IComparer<T>
        {
            for (int i = 1; i < list.Count; i++)
            {
                var toSort = list[i];
                int index = list.BinarySearch(0, i, toSort, customComparer);
                list.RemoveAt(i);
                list.Insert(index < 0 ? ~index : index, toSort);
            }
        }*/

        /// <summary> Respect list's IComparer, if implemented. </summary>
        //public int CompareItems(T itemA, T itemB) => HasComparer ? Comparer.Compare(itemA, itemB) : itemA.CompareTo(itemB);

        /*public bool FindByComparison(Compare<T> comparison, out T value)
        {
            return list.FindBinary(comparison, out value);
        }*/

        /*public int IndexOfFirstMatching(int index)
        {
            //If index is invalid or equals the first valid index, return index
            if (index <= 0 || index >= list.Count)
                return index;

            return IndexOfFirstMatchingInternalNoSafety(index);
        }
        
        int IndexOfFirstMatchingInternalNoSafety(int index)
        {
            var comparable = list[index];

            for (int i = index - 1; i >= 0; i--)
            {
                if (CompareItems(comparable, list[i]) != 0)
                    return i + 1;
            }

            return 0;
        }

        public int IndexOfFirstMatching(T comparable)
            => IndexOfFirstMatching(IndexOfComparableMatch(comparable));*/

        /*public int IndexOfLastMatching(int index)
        {
            //If index is invalid or already the last valid index, return the passed in index
            if (index < 0 || index >= list.Count - 1)
                return index;

            return IndexOfLastMatchingInternalNoSafety(index);
        }*/
        
        /*public int IndexOfLastMatchingInternalNoSafety(int index)
        {
            var comparable = list[index];

            
            
            /*for (int i = index + 1; i < list.Count; i++)
            {
                if (CompareItems(comparable, list[i]) != 0)
                    return i - 1;
            }

            return list.Count - 1;#1#
        }*/

        /*public int IndexOfLastMatching(T comparable)
            => IndexOfLastMatching(IndexOfComparableMatch(comparable));*/

        /// <summary> Step in the search direction until we find the last index. Steps start broad and then halve each time an overstep occurs. </summary>
        public int SearchBroadMatchingIndexByComparison(int indexOfComparable, int searchDir)
        {
            VCollectionUtils.ConditionalCheckValueNonZero(searchDir);
            VCollectionUtils.ConditionalCheckIndexValid(indexOfComparable, list.Count);
            ref var comparable = ref list.ElementAt(indexOfComparable);
            var currentIndex = indexOfComparable;
            
            var collectionLength = list.Count;
            var searchDirMult = math.sign(searchDir);
            int stepSize = math.max(1, math.ceilpow2(collectionLength / 16)) * searchDirMult;

            int stepsLimit = collectionLength;
            while (math.abs(stepSize) > 0 && stepsLimit-- > 0)
            {
                // Try step
                int searchFromIndex = currentIndex + stepSize;
                
                // Check for overstep
                bool oversteppedBounds = searchFromIndex < 0 || searchFromIndex >= collectionLength;
                bool overstepped = oversteppedBounds || comparable.CompareTo(list[searchFromIndex]) != 0;
                if (overstepped)
                    stepSize /= 2;
                else
                    currentIndex = searchFromIndex; // If stepped correctly, move the current index
            }
            
            // Ensure that the loop didn't run out of steps
            VCollectionUtils.ConditionalCheckValueGreaterThanZero(stepsLimit);

            // If we moved the index, we found the first, otherwise we're at the first
            return currentIndex;
        }

        /*/// <summary> Step in the search direction until we find the last index. Steps are linear, this method is the slowest but is required for full equality checks. </summary>
        public int SearchLinearMatchingIndexByEquality(int indexOfComparable, int searchDir)
        {
            VCollectionUtils.ConditionalCheckValueNonZero(searchDir);
            VCollectionUtils.ConditionalCheckIndexValid(indexOfComparable, list.Count);
            ref var comparableElement = ref list.ElementAt(indexOfComparable);
            var currentIndex = indexOfComparable;
            
            var collectionLength = list.Count;
            searchDir = math.sign(searchDir);

            if (searchDir < 0)
            {
                for (int i = indexOfComparable - 1; i >= 0; i--)
                {
                    if (comparableElement.Equals(list[i]))
                        currentIndex = i;
                    else
                        break;
                }
            }

            int stepsLimit = collectionLength;
            while (stepsLimit-- > 0)
            {
                // Try step
                int searchFromIndex = currentIndex + searchDir;
                
                // Check for overstep
                bool oversteppedBounds = searchFromIndex < 0 || searchFromIndex >= collectionLength;
                bool overstepped = oversteppedBounds || comparableElement.CompareTo(list[searchFromIndex]) != 0;
                if (overstepped)
                    break;
                else
                    currentIndex = searchFromIndex; // If stepped correctly, move the current index
            }
            
            // Ensure that the loop didn't run out of steps
            VCollectionUtils.ConditionalCheckValueGreaterThanZero(stepsLimit);

            // If we moved the index, we found the first, otherwise we're at the first
            return currentIndex;
        }*/

        /// <summary> Finds the start index and end index of all matching objects.
        /// Returns true if found ANY matches.</summary>
        public bool FindIndexRangeOfMatching(int indexOfComparable, out int2 startEnd)
        {
            if (indexOfComparable < 0 || indexOfComparable >= list.Count)
            {
                startEnd = 0;
                return false;
            }

            int firstIndex = SearchBroadMatchingIndexByComparison(indexOfComparable, -1);
            int lastIndex = SearchBroadMatchingIndexByComparison(indexOfComparable, 1);

            startEnd = new int2(firstIndex, lastIndex);
            return true;
        }

        /// <summary> The collection supports multiple keys that are the same according to IComparable{T}. <br/>
        /// This finds the index of the key that is also equal according to IEquatable{T}. </summary>
        public bool FindKeyEquals(T key, out int keyIndex)
        {
            keyIndex = IndexOfComparableMatch(key);
            if (keyIndex < 0)
                return false;

            if (list[keyIndex].Equals(key))
                return true;

            // Search
            if (!FindIndexRangeOfMatching(keyIndex, out int2 searchRange))
                return false;
            
            for (int i = searchRange.x; i <= searchRange.y; i++)
            {
                if (list[i].Equals(key))
                {
                    keyIndex = i;
                    return true;
                }
            }
            return false;
        }

        /// <summary> Finds the start index and end index of all matching objects.
        /// Returns true if found ANY matches.</summary>
        /*public bool FindIndexRangeOfMatching(T comparable, out int2 startEnd)
        {
            return FindIndexRangeOfMatching(IndexOfComparableMatch(comparable), out startEnd);
        }*/

        /*public void PopulateWithUnsortedList(List<T> unsortedList, bool additive)
        {
            if (!additive)
                list.Clear();

            int neededCapacity = list.Count + unsortedList.Count;
            if (list.Capacity < neededCapacity)
                list.Capacity = neededCapacity;

            list.AddRange(unsortedList);

            Resort();
        }*/

        /*public void SetInternalListWithUnsorted(List<T> unsortedCollection)
        {
            list = unsortedCollection;
            Resort();
        }*/

        /// <summary> Checks that the index is greater than or equal to 0, and less than or equal to list.Count. </summary>
        [Conditional("UNITY_EDITOR")]
        public void EditorOnlyAssertValidInsertionIndex(int index)
        {
            if (index < 0)
                throw new IndexOutOfRangeException($"Index is: {index}.. Index cannot be negative!");
            if (index > list.Count)
                throw new IndexOutOfRangeException($"Index is: {index}.. Index for insertion specifically cannot be greater than list.Count! (but CAN be equal)");
        }
        
        [Conditional("UNITY_EDITOR")]
        public void EditorOnlyVerifyLocalKeyOrder(int index)
        {
            if (index > 0)
                Assert.IsTrue(list[index - 1].CompareTo(list[index]) <= 0);
            if (index < list.Count - 1)
                Assert.IsTrue(list[index + 1].CompareTo(list[index]) >= 0);
        }
    }

    /*public static class VUnsafeSortedListTExt
    {
        public static int CompareItems<T>(this VUnsafeSortedList<T> list, T itemA, T itemB)
            where T : IComparable<T>
        {
            //Respect list's IComparer, if implemented.
            return list.HasComparer ? list.Comparer.Compare(itemA, itemB) : itemA.CompareTo(itemB);
        }
        
        public static bool FindByComparison<T>(this VUnsafeSortedList<T> list, Compare<T> comparison, out T value)
            where T : IComparable<T>
        {
            return list.list.FindBinary(comparison, out value);
        }

        public static int IndexOfFirstMatching<T>(this VUnsafeSortedList<T> list, int index)
            where T : IComparable<T>
        {
            if (index <= 0 || index >= list.Count)
                return index;

            var comparable = list[index];

            for (int i = index - 1; i >= 0; i--)
            {
                if (CompareItems(list, comparable, list[i]) != 0)
                    return i + 1;
            }

            return 0;
        }

        public static int IndexOfFirstMatching<T>(this VUnsafeSortedList<T> list, T comparable)
            where T : IComparable<T>
            => IndexOfFirstMatching(list, list.IndexOf(comparable));

        public static int IndexOfLastMatching<T>(this VUnsafeSortedList<T> list, int index)
            where T : IComparable<T>
        {
            if (index < 0 || index >= list.Count)
                return index;

            var comparable = list[index];

            for (int i = index + 1; i < list.Count; i++)
            {
                if (CompareItems(list, comparable, list[i]) != 0)
                    return i - 1;
            }

            return list.Count - 1;
        }

        public static int IndexOfLastMatching<T>(this VUnsafeSortedList<T> list, T comparable)
            where T : IComparable<T>
            => IndexOfLastMatching(list, list.IndexOf(comparable));

        /// <summary> Finds the start index and end index of all matching objects.
        /// Returns true if found ANY matches.</summary>
        public static bool FindIndexRangeOfMatching<T>(this VUnsafeSortedList<T> list, T comparable, out int2 startEnd)
            where T : IComparable<T>
        {
            int index = list.IndexOf(comparable);

            if (index < 0 || index >= list.Count)
            {
                startEnd = 0;
                return false;
            }

            int firstIndex = IndexOfFirstMatching(list, index);
            int lastIndex = IndexOfLastMatching(list, index);

            startEnd = new int2(firstIndex, lastIndex);
            return true;
        }

        /// <summary> This is the preferred method if T implements IComparableT </summary>
        public static void ResortComparables<T>(this VUnsafeSortedList<T> list)
            where T : IComparable<T>
        {
            if (list.HasComparer)
                list.Resort(list.Comparer);
            else
            {
                var comparer = new ComparableComparer<T>();
                list.Resort(comparer);
            }
        }

        public static void PopulateWithUnsortedList<T>(this VUnsafeSortedList<T> sortedList, List<T> list)
            where T : IComparable<T>
        {
            sortedList.list = list;
            sortedList.ResortComparables();
        }
    }*/
}