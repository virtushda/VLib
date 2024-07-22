using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace VLib
{
    /// <summary> A List and Hashset taped together, when you want a list but with super fast .Contains checks. Useful for building lists of unique items. </summary>
    [System.Serializable]
    public class HashList<T> : ICollection<T>, IEnumerable<T>, IList<T>
    {
        public List<T> list;
        public HashSet<T> hashset;

        public HashList()
        {
            list = new List<T>();
            hashset = new HashSet<T>();
        }

        public HashList(int initCapacity)
        {
            list = new List<T>(initCapacity);
            hashset = new HashSet<T>();
        }

        public T this[int i]
        {
            get
            {
                if (i >= list.Count || i < 0)
                    throw new IndexOutOfRangeException($"Index '{i}' is out of range [0 - {list.Count - 1}]");
                return list[i];
            }
            set
            {
                if (i >= list.Count || i < 0)
                    throw new IndexOutOfRangeException($"Index '{i}' is out of range [0 - {list.Count - 1}]");
                list[i] = value;
            }
        }

        public int Count => list.Count;
        bool ICollection<T>.IsReadOnly { get; }

        /// <summary> Cannot add if value present in hashset </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T obj) => TryAdd(obj);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(T obj)
        {
            if (Contains(obj))
                return false;
            
            list.Add(obj);
            hashset.Add(obj);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(IReadOnlyList<T> collection)
        {
            for (int i = 0; i < collection.Count; i++)
            {
                T obj = collection[i];

                if (hashset.Contains(obj))
                    continue;

                list.Add(obj);
                hashset.Add(obj);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(T obj)
        {
            if (!hashset.Contains(obj))
                return false;

            list.Remove(obj);
            hashset.Remove(obj);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAt(int index)
        {
            if (hashset.Count == 0 || index >= hashset.Count)
                return;

            T item = list[index];
            hashset.Remove(item);
            list.RemoveAt(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveRange(IReadOnlyList<T> collection)
        {
            for (int i = 0; i < collection.Count; i++)
            {
                if (hashset.Contains(collection[i]) == false)
                    continue;

                list.Remove(collection[i]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveNonOverlapping(HashSet<T> otherHashset)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (!otherHashset.Contains(list[i]))
                {
                    RemoveAt(i);
                    i--;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(T obj)
        {
            return hashset.Contains(obj);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            list.Clear();
            hashset.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MirrorToAdditive(HashList<T> hList)
        {
            hList.AddRange(list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MirrorFromAdditive(HashList<T> hList)
        {
            AddRange(hList.list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MirrorTo(HashList<T> hList)
        {
            MirrorExact(this, hList);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MirrorFrom(HashList<T> hList)
        {
            MirrorExact(hList, this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MirrorExact(HashList<T> mirrorRef, HashList<T> mirrorTarget)
        {
            mirrorTarget.RemoveNonOverlapping(mirrorRef.hashset);
            mirrorRef.MirrorToAdditive(mirrorTarget);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(T item)
        {
            return list.FindIndex((i) => i.Equals(item));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Insert(int index, T item)
        {
            //Move
            if (hashset.Contains(item))
            {
                if (!list[index].Equals(item))
                {
                    int idx = IndexOf(item);
                    T itemToMove = list[idx];
                    list.RemoveAt(idx);
                    list.Insert(index, itemToMove);
                }
            }
            else //Insert
            {
                hashset.Add(item);
                list.Insert(index, item);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(T[] array, int arrayIndex)
        {
            int maxCount = math.min(list.Count, array.Length - arrayIndex + 1);
            for (int i = 0, arr = arrayIndex; i < maxCount; i++, arr++)
            {
                array[arr] = list[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }
    }

    public static class HashListExt
    {
        public static long MemoryFootprintBytes<T>(this HashList<T> dictionary)
            where T : unmanaged
        {
            return dictionary.list.MemoryFootprintBytes() + dictionary.hashset.MemoryFootprintBytes();
        }
        
        public static long MemoryFootprintBytesManaged<T>(this HashList<T> dictionary)
        {
            return dictionary.list.MemoryFootprintBytesManaged() + dictionary.hashset.MemoryFootprintBytesManaged();
        }
    }
}