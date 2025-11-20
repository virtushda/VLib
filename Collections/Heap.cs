using System;
using System.Collections.Generic;

namespace VLib.Collections
{
    /// <summary>
    /// Generic binary heap that supports both min and max heap behavior.
    /// O(log N) insert/remove, O(1) peek, O(N) construction from array
    /// </summary>
    /// <typeparam name="T">Type that implements IComparable for comparison</typeparam>
    public class Heap<T>
        where T : IComparable<T>
    {
        T[] items;
        int count;
        readonly bool isMinHeap;

        public int Count => count;
        public int Capacity => items.Length;

        /// <summary>
        /// Create a new heap
        /// </summary>
        /// <param name="capacity">Initial capacity</param>
        /// <param name="isMinHeap">True for min-heap (smallest first), false for max-heap (largest first)</param>
        public Heap(int capacity = 64, bool isMinHeap = true)
        {
            if (capacity <= 0)
                throw new ArgumentException("Capacity must be positive", nameof(capacity));

            items = new T[capacity];
            count = 0;
            this.isMinHeap = isMinHeap;
        }

        /// <summary>
        /// Add item to heap. O(log N)
        /// </summary>
        public void Add(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (count == items.Length)
                Resize(items.Length * 2);

            items[count] = item;
            count++;
            HeapifyUp(count - 1);
        }

        /// <summary>
        /// Remove and return the root item (min or max). O(log N)
        /// </summary>
        public T RemoveRoot()
        {
            if (count == 0)
                throw new InvalidOperationException("Heap is empty");

            T root = items[0];
            count--;

            if (count > 0)
            {
                items[0] = items[count];
                items[count] = default;
                HeapifyDown(0);
            }
            else
            {
                items[0] = default;
            }

            return root;
        }

        /// <summary>
        /// Get root item without removing. O(1)
        /// </summary>
        public T Peek()
        {
            if (count == 0)
                throw new InvalidOperationException("Heap is empty");
            return items[0];
        }

        /// <summary>
        /// Try to get root item without throwing exception. O(1)
        /// </summary>
        public bool TryPeek(out T item)
        {
            if (count == 0)
            {
                item = default;
                return false;
            }

            item = items[0];
            return true;
        }

        /// <summary>
        /// Clear all items from heap. O(N)
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < count; i++)
                items[i] = default;
            count = 0;
        }

        /// <summary>
        /// Check if heap contains item. O(N) - linear search
        /// </summary>
        public bool Contains(T item)
        {
            if (item == null)
                return false;

            for (int i = 0; i < count; i++)
            {
                if (EqualityComparer<T>.Default.Equals(items[i], item))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Build heap from existing array. O(N)
        /// </summary>
        public void BuildHeap(T[] sourceArray, int length)
        {
            if (sourceArray == null)
                throw new ArgumentNullException(nameof(sourceArray));
            if (length < 0 || length > sourceArray.Length)
                throw new ArgumentOutOfRangeException(nameof(length));

            if (length > items.Length)
                items = new T[length];

            Array.Copy(sourceArray, items, length);
            count = length;

            // Heapify from bottom up (more efficient than adding one by one)
            for (int i = (count >> 1) - 1; i >= 0; i--)
                HeapifyDown(i);
        }

        void HeapifyUp(int index)
        {
            T item = items[index];

            while (index > 0)
            {
                int parentIndex = (index - 1) >> 1; // Faster than / 2
                T parent = items[parentIndex];

                // Compare based on heap type
                int comparison = item.CompareTo(parent);
                bool shouldSwap = isMinHeap ? comparison < 0 : comparison > 0;

                if (!shouldSwap)
                    break;

                // Move parent down
                items[index] = parent;
                index = parentIndex;
            }

            items[index] = item;
        }

        void HeapifyDown(int index)
        {
            T item = items[index];
            int halfCount = count >> 1; // Items with at least one child

            while (index < halfCount)
            {
                int leftChild = (index << 1) + 1; // Faster than * 2 + 1
                int rightChild = leftChild + 1;
                int priorityChild = leftChild;

                // Find child with priority (min or max based on heap type)
                if (rightChild < count)
                {
                    int comparison = items[rightChild].CompareTo(items[leftChild]);
                    bool rightHasPriority = isMinHeap ? comparison < 0 : comparison > 0;

                    if (rightHasPriority)
                        priorityChild = rightChild;
                }

                // Check if we should swap with priority child
                int itemComparison = item.CompareTo(items[priorityChild]);
                bool shouldSwap = isMinHeap ? itemComparison > 0 : itemComparison < 0;

                if (!shouldSwap)
                    break;

                // Move priority child up
                items[index] = items[priorityChild];
                index = priorityChild;
            }

            items[index] = item;
        }

        void Resize(int newCapacity)
        {
            T[] newItems = new T[newCapacity];
            Array.Copy(items, newItems, count);
            items = newItems;
        }

        /// <summary>
        /// Debug helper - validate heap property holds for all nodes
        /// </summary>
        public bool IsValidHeap()
        {
            for (int i = 0; i < count; i++)
            {
                int leftChild = (i << 1) + 1;
                int rightChild = leftChild + 1;

                if (leftChild < count)
                {
                    int comparison = items[i].CompareTo(items[leftChild]);
                    bool isValid = isMinHeap ? comparison <= 0 : comparison >= 0;
                    if (!isValid)
                        return false;
                }

                if (rightChild < count)
                {
                    int comparison = items[i].CompareTo(items[rightChild]);
                    bool isValid = isMinHeap ? comparison <= 0 : comparison >= 0;
                    if (!isValid)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Get array representation of heap (for debugging)
        /// </summary>
        public T[] ToArray()
        {
            T[] result = new T[count];
            Array.Copy(items, result, count);
            return result;
        }
    }
}