using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace VLib
{
    /// <summary>
    /// Either implement IComparable on type <typeparamref name="T"/>, or supply a Comparer<typeparamref name="T"/>.
    /// Any other use will not work properly.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class VSortedList<T> : IList<T>, IReadOnlyList<T>
        where T : IComparable<T>//, IEquatable<T>
    {
        [SerializeField] public List<T> list;

        [SerializeField] bool hasComparer = false;
        [SerializeField] IComparer<T> comparer;

        IComparer<T> defaultComparer;

        public VSortedList()
        {
            list = new List<T>();
        }

        public VSortedList(int initCapacity)
        {
            list = new List<T>(initCapacity);
        }

        public VSortedList(int initCapacity, IComparer<T> comparer)
        {
            list = new List<T>(initCapacity);
            this.hasComparer = comparer != null;
            this.comparer = comparer;
        }

        public bool HasComparer => hasComparer;
        public IComparer<T> Comparer => comparer;
        IComparer<T> DefaultComparer => defaultComparer ??= new ComparableComparer<T>();

        IReadOnlyList<T> AsReadOnly => list.AsReadOnly();

        /// <summary> Setting is unsafe! </summary>
        public T this[int i]
        {
            get
            {
            #if UNITY_EDITOR
                IsIndexValid(i, true);
            #endif
                return list[i];
            }
            set
            {
            #if UNITY_EDITOR
                IsIndexValid(i, true);
            #endif
                list[i] = value;
            }
        }

        public int Count => list.Count;

        bool ICollection<T>.IsReadOnly => false;

        public bool Contains(T obj) => IndexOf(obj) >= 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Clear() => list.Clear();

        /// <summary>
        /// Binary searches for first 'comparably equal' element, then checks each possibly equal element for true equality.
        /// </summary>
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
        public virtual void Add(T value)
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

        public void RemoveAt(int index)
        {
        #if UNITY_EDITOR
            IsIndexValid(index, true);
        #endif
            list.RemoveAt(index);
        }

        public void ReorderAt(int index)
        {
            var item = list[index];
            list.RemoveAt(index);
            Add(item);
        }

        /// <summary> Get and remove from collection. </summary>
        public T Pull(int index)
        {
#if UNITY_EDITOR
            IsIndexValid(index, true);
#endif
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

        public void CopyTo(T[] array, int arrayIndex)
        {
            int maxCount = math.min(list.Count, array.Length - arrayIndex + 1);
            for (int i = 0, arr = arrayIndex; i < maxCount; i++, arr++)
            {
                array[arr] = list[i];
            }
        }

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)list).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)list).GetEnumerator();
        
        /// <summary> Much faster than .Contains, especially for struct/unmanaged types. But the IComparer must be water-tight! </summary>
        /// <param name="obj"></param>
        /// <returns> Returns true if the IComparable.Compare or IComparer implementations report a match. </returns>
        public bool ContainsMatch(T obj) => IndexOfComparableMatch(obj) >= 0;

        /// <summary> Finds the first "match". </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOfComparableMatch(T item) => hasComparer ? list.BinarySearch(item, comparer) : list.BinarySearch(item);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOfByComparison(Compare<T> comparison) => list.BinarySearch(comparison);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsIndexValid(int index, bool throwException = false)
        {
            var count = list.Count;
            if (count < 1)
            {
                string error = "Index out of range, list is empty!";
                if (throwException)
                    throw new IndexOutOfRangeException(error);
                else
                    Debug.LogError(error);
            }

            if (index < 0 || index >= list.Count)
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
        public void AddRange(IReadOnlyList<T> collection)
        {
            for (int i = 0; i < collection.Count; i++)
                Add(collection[i]);
        }

        /// <summary> If the object is already in the collection, it will be rejected. </summary>
        public bool TryAddExclusive(T value) => TryAddExclusiveInternal(value, IndexOf(value));

        /// <summary> If the object is already in the collection OR an IComparer implementation decides the object already exists in this collection, it will be rejected. </summary>
        public bool TryAddExclusiveStrict(T value) => TryAddExclusiveInternal(value, IndexOfComparableMatch(value));

        protected bool TryAddExclusiveInternal(T value, int index)
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

        public T GetRandom(bool remove)
        {
            int index = Random.Range(0, Count);
            T val = list[index];
            if (remove)
                list.RemoveAt(index);
            return val;
        }

        public void Resort()
        {
            Resort(HasComparer ? Comparer : DefaultComparer);

            /*for (int i = 1; i < list.Count; i++)
                {
                    var toSort = list[i];
                    int index = list.BinarySearch(0, i, toSort, defaultComparer);
                    list.RemoveAt(i);
                    list.Insert(index < 0 ? ~index : index, toSort);
                }*/
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
        }

        /// <summary> Respect list's IComparer, if implemented. </summary>
        public int CompareItems(T itemA, T itemB) => HasComparer ? Comparer.Compare(itemA, itemB) : itemA.CompareTo(itemB);

        public bool FindByComparison(Compare<T> comparison, out T value)
        {
            return list.FindBinary(comparison, out value);
        }

        public int IndexOfFirstMatching(int index)
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
            => IndexOfFirstMatching(IndexOfComparableMatch(comparable));

        public int IndexOfLastMatching(int index)
        {
            //If index is invalid or already the last valid index, return the passed in index
            if (index < 0 || index >= list.Count - 1)
                return index;

            return IndexOfLastMatchingInternalNoSafety(index);
        }
        
        public int IndexOfLastMatchingInternalNoSafety(int index)
        {
            var comparable = list[index];

            for (int i = index + 1; i < list.Count; i++)
            {
                if (CompareItems(comparable, list[i]) != 0)
                    return i - 1;
            }

            return list.Count - 1;
        }

        public int IndexOfLastMatching(T comparable)
            => IndexOfLastMatching(IndexOfComparableMatch(comparable));

        /// <summary> Finds the start index and end index of all matching objects.
        /// Returns true if found ANY matches.</summary>
        public bool FindIndexRangeOfMatching(int indexOfComparable, out int2 startEnd)
        {
            if (indexOfComparable < 0 || indexOfComparable >= list.Count)
            {
                startEnd = 0;
                return false;
            }

            int firstIndex = IndexOfFirstMatchingInternalNoSafety(indexOfComparable);
            int lastIndex = IndexOfLastMatchingInternalNoSafety(indexOfComparable);

            startEnd = new int2(firstIndex, lastIndex);
            return true;
        }

        /// <summary> Finds the start index and end index of all matching objects.
        /// Returns true if found ANY matches.</summary>
        public bool FindIndexRangeOfMatching(T comparable, out int2 startEnd)
        {
            return FindIndexRangeOfMatching(IndexOfComparableMatch(comparable), out startEnd);
        }

        public void PopulateWithUnsortedList(List<T> unsortedList, bool additive)
        {
            if (!additive)
                list.Clear();

            int neededCapacity = list.Count + unsortedList.Count;
            if (list.Capacity < neededCapacity)
                list.Capacity = neededCapacity;

            list.AddRange(unsortedList);

            Resort();
        }

        public void SetInternalListWithUnsorted(List<T> unsortedCollection)
        {
            list = unsortedCollection;
            Resort();
        }

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

    /*public static class VSortedListTExt
    {
        public static int CompareItems<T>(this VSortedList<T> list, T itemA, T itemB)
            where T : IComparable<T>
        {
            //Respect list's IComparer, if implemented.
            return list.HasComparer ? list.Comparer.Compare(itemA, itemB) : itemA.CompareTo(itemB);
        }
        
        public static bool FindByComparison<T>(this VSortedList<T> list, Compare<T> comparison, out T value)
            where T : IComparable<T>
        {
            return list.list.FindBinary(comparison, out value);
        }

        public static int IndexOfFirstMatching<T>(this VSortedList<T> list, int index)
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

        public static int IndexOfFirstMatching<T>(this VSortedList<T> list, T comparable)
            where T : IComparable<T>
            => IndexOfFirstMatching(list, list.IndexOf(comparable));

        public static int IndexOfLastMatching<T>(this VSortedList<T> list, int index)
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

        public static int IndexOfLastMatching<T>(this VSortedList<T> list, T comparable)
            where T : IComparable<T>
            => IndexOfLastMatching(list, list.IndexOf(comparable));

        /// <summary> Finds the start index and end index of all matching objects.
        /// Returns true if found ANY matches.</summary>
        public static bool FindIndexRangeOfMatching<T>(this VSortedList<T> list, T comparable, out int2 startEnd)
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
        public static void ResortComparables<T>(this VSortedList<T> list)
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

        public static void PopulateWithUnsortedList<T>(this VSortedList<T> sortedList, List<T> list)
            where T : IComparable<T>
        {
            sortedList.list = list;
            sortedList.ResortComparables();
        }
    }*/
}