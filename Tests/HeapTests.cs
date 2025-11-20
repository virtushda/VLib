using NUnit.Framework;
using System;
using System.Collections.Generic;
using VLib.Collections;

namespace VLib.Tests
{
    /// <summary>
    /// Comprehensive test suite for <see cref="Heap{T}"/> implementation
    /// Tests both min-heap and max-heap behavior across all operations
    /// </summary>
    public class HeapTests
    {
        #region Constructor Tests

        [Test]
        public void Constructor_DefaultCapacity_CreatesEmptyHeap()
        {
            var heap = new Heap<int>();
            
            Assert.AreEqual(0, heap.Count);
            Assert.AreEqual(64, heap.Capacity);
        }

        [Test]
        public void Constructor_CustomCapacity_SetsCorrectCapacity()
        {
            var heap = new Heap<int>(128);
            
            Assert.AreEqual(0, heap.Count);
            Assert.AreEqual(128, heap.Capacity);
        }

        [Test]
        public void Constructor_MinHeapFlag_CreatesMinHeap()
        {
            var heap = new Heap<int>(64, isMinHeap: true);
            heap.Add(5);
            heap.Add(3);
            heap.Add(7);
            
            Assert.AreEqual(3, heap.Peek()); // Smallest at root
        }

        [Test]
        public void Constructor_MaxHeapFlag_CreatesMaxHeap()
        {
            var heap = new Heap<int>(64, isMinHeap: false);
            heap.Add(5);
            heap.Add(3);
            heap.Add(7);
            
            Assert.AreEqual(7, heap.Peek()); // Largest at root
        }

        [Test]
        public void Constructor_ZeroCapacity_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new Heap<int>(0));
        }

        [Test]
        public void Constructor_NegativeCapacity_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new Heap<int>(-1));
        }

        #endregion

        #region Add Tests

        [Test]
        public void Add_SingleItem_IncreasesCount()
        {
            var heap = new Heap<int>();
            heap.Add(5);
            
            Assert.AreEqual(1, heap.Count);
        }

        [Test]
        public void Add_MultipleItems_MaintainsMinHeapProperty()
        {
            var heap = new Heap<int>(8, isMinHeap: true);
            int[] values = { 5, 3, 7, 1, 9, 2, 8, 4 };
            
            foreach (int value in values)
                heap.Add(value);
            
            Assert.AreEqual(8, heap.Count);
            Assert.IsTrue(heap.IsValidHeap());
            Assert.AreEqual(1, heap.Peek()); // Min at root
        }

        [Test]
        public void Add_MultipleItems_MaintainsMaxHeapProperty()
        {
            var heap = new Heap<int>(8, isMinHeap: false);
            int[] values = { 5, 3, 7, 1, 9, 2, 8, 4 };
            
            foreach (int value in values)
                heap.Add(value);
            
            Assert.AreEqual(8, heap.Count);
            Assert.IsTrue(heap.IsValidHeap());
            Assert.AreEqual(9, heap.Peek()); // Max at root
        }

        [Test]
        public void Add_ExceedsCapacity_AutomaticallyResizes()
        {
            var heap = new Heap<int>(2);
            heap.Add(1);
            heap.Add(2);
            heap.Add(3); // Should trigger resize
            
            Assert.AreEqual(3, heap.Count);
            Assert.GreaterOrEqual(heap.Capacity, 3);
        }

        [Test]
        public void Add_NullItem_ThrowsArgumentNullException()
        {
            var heap = new Heap<string>();
            
            Assert.Throws<ArgumentNullException>(() => heap.Add(null));
        }

        [Test]
        public void Add_DuplicateValues_AllowsMultipleInstances()
        {
            var heap = new Heap<int>();
            heap.Add(5);
            heap.Add(5);
            heap.Add(5);
            
            Assert.AreEqual(3, heap.Count);
        }

        #endregion

        #region RemoveRoot Tests

        [Test]
        public void RemoveRoot_SingleItem_ReturnsItemAndBecomesEmpty()
        {
            var heap = new Heap<int>();
            heap.Add(42);
            
            int removed = heap.RemoveRoot();
            
            Assert.AreEqual(42, removed);
            Assert.AreEqual(0, heap.Count);
        }

        [Test]
        public void RemoveRoot_MinHeap_ReturnsSmallestItem()
        {
            var heap = new Heap<int>(8, isMinHeap: true);
            int[] values = { 5, 3, 7, 1, 9 };
            foreach (int value in values)
                heap.Add(value);
            
            int removed = heap.RemoveRoot();
            
            Assert.AreEqual(1, removed);
            Assert.AreEqual(4, heap.Count);
            Assert.IsTrue(heap.IsValidHeap());
        }

        [Test]
        public void RemoveRoot_MaxHeap_ReturnsLargestItem()
        {
            var heap = new Heap<int>(8, isMinHeap: false);
            int[] values = { 5, 3, 7, 1, 9 };
            foreach (int value in values)
                heap.Add(value);
            
            int removed = heap.RemoveRoot();
            
            Assert.AreEqual(9, removed);
            Assert.AreEqual(4, heap.Count);
            Assert.IsTrue(heap.IsValidHeap());
        }

        [Test]
        public void RemoveRoot_MultipleRemovals_MaintainsHeapProperty()
        {
            var heap = new Heap<int>(8, isMinHeap: true);
            int[] values = { 5, 3, 7, 1, 9, 2, 8, 4 };
            foreach (int value in values)
                heap.Add(value);
            
            var removed = new List<int>();
            while (heap.Count > 0)
            {
                removed.Add(heap.RemoveRoot());
                if (heap.Count > 0)
                    Assert.IsTrue(heap.IsValidHeap());
            }
            
            // Min heap should return items in ascending order
            Assert.AreEqual(new[] { 1, 2, 3, 4, 5, 7, 8, 9 }, removed.ToArray());
        }

        [Test]
        public void RemoveRoot_EmptyHeap_ThrowsInvalidOperationException()
        {
            var heap = new Heap<int>();
            
            Assert.Throws<InvalidOperationException>(() => heap.RemoveRoot());
        }

        [Test]
        public void RemoveRoot_TwoElements_MaintainsCorrectOrder()
        {
            var heap = new Heap<int>();
            heap.Add(5);
            heap.Add(3);
            
            Assert.AreEqual(3, heap.RemoveRoot());
            Assert.AreEqual(5, heap.RemoveRoot());
        }

        #endregion

        #region Peek Tests

        [Test]
        public void Peek_NonEmptyHeap_ReturnsRootWithoutRemoving()
        {
            var heap = new Heap<int>();
            heap.Add(5);
            heap.Add(3);
            
            int peeked = heap.Peek();
            
            Assert.AreEqual(3, peeked);
            Assert.AreEqual(2, heap.Count); // Count unchanged
        }

        [Test]
        public void Peek_EmptyHeap_ThrowsInvalidOperationException()
        {
            var heap = new Heap<int>();
            
            Assert.Throws<InvalidOperationException>(() => heap.Peek());
        }

        #endregion

        #region TryPeek Tests

        [Test]
        public void TryPeek_NonEmptyHeap_ReturnsTrueAndItem()
        {
            var heap = new Heap<int>();
            heap.Add(5);
            heap.Add(3);
            
            bool success = heap.TryPeek(out int item);
            
            Assert.IsTrue(success);
            Assert.AreEqual(3, item);
            Assert.AreEqual(2, heap.Count); // Count unchanged
        }

        [Test]
        public void TryPeek_EmptyHeap_ReturnsFalseAndDefault()
        {
            var heap = new Heap<int>();
            
            bool success = heap.TryPeek(out int item);
            
            Assert.IsFalse(success);
            Assert.AreEqual(default(int), item);
        }

        #endregion

        #region Clear Tests

        [Test]
        public void Clear_NonEmptyHeap_RemovesAllItems()
        {
            var heap = new Heap<int>();
            heap.Add(1);
            heap.Add(2);
            heap.Add(3);
            
            heap.Clear();
            
            Assert.AreEqual(0, heap.Count);
        }

        [Test]
        public void Clear_AfterClear_CanAddNewItems()
        {
            var heap = new Heap<int>();
            heap.Add(1);
            heap.Clear();
            
            heap.Add(5);
            
            Assert.AreEqual(1, heap.Count);
            Assert.AreEqual(5, heap.Peek());
        }

        #endregion

        #region Contains Tests

        [Test]
        public void Contains_ItemExists_ReturnsTrue()
        {
            var heap = new Heap<int>();
            heap.Add(5);
            heap.Add(3);
            heap.Add(7);
            
            Assert.IsTrue(heap.Contains(3));
            Assert.IsTrue(heap.Contains(5));
            Assert.IsTrue(heap.Contains(7));
        }

        [Test]
        public void Contains_ItemDoesNotExist_ReturnsFalse()
        {
            var heap = new Heap<int>();
            heap.Add(5);
            heap.Add(3);
            
            Assert.IsFalse(heap.Contains(10));
        }

        [Test]
        public void Contains_NullItem_ReturnsFalse()
        {
            var heap = new Heap<string>();
            heap.Add("test");
            
            Assert.IsFalse(heap.Contains(null));
        }

        [Test]
        public void Contains_EmptyHeap_ReturnsFalse()
        {
            var heap = new Heap<int>();
            
            Assert.IsFalse(heap.Contains(5));
        }

        #endregion

        #region BuildHeap Tests

        [Test]
        public void BuildHeap_FromArray_CreatesValidMinHeap()
        {
            var heap = new Heap<int>(8, isMinHeap: true);
            int[] values = { 5, 3, 7, 1, 9, 2, 8, 4 };
            
            heap.BuildHeap(values, values.Length);
            
            Assert.AreEqual(8, heap.Count);
            Assert.IsTrue(heap.IsValidHeap());
            Assert.AreEqual(1, heap.Peek());
        }

        [Test]
        public void BuildHeap_FromArray_CreatesValidMaxHeap()
        {
            var heap = new Heap<int>(8, isMinHeap: false);
            int[] values = { 5, 3, 7, 1, 9, 2, 8, 4 };
            
            heap.BuildHeap(values, values.Length);
            
            Assert.AreEqual(8, heap.Count);
            Assert.IsTrue(heap.IsValidHeap());
            Assert.AreEqual(9, heap.Peek());
        }

        [Test]
        public void BuildHeap_PartialArray_UsesOnlySpecifiedLength()
        {
            var heap = new Heap<int>();
            int[] values = { 5, 3, 7, 1, 9 };
            
            heap.BuildHeap(values, 3); // Only use first 3 elements
            
            Assert.AreEqual(3, heap.Count);
            Assert.IsTrue(heap.IsValidHeap());
        }

        [Test]
        public void BuildHeap_EmptyArray_CreatesEmptyHeap()
        {
            var heap = new Heap<int>();
            int[] values = new int[0];
            
            heap.BuildHeap(values, 0);
            
            Assert.AreEqual(0, heap.Count);
        }

        [Test]
        public void BuildHeap_NullArray_ThrowsArgumentNullException()
        {
            var heap = new Heap<int>();
            
            Assert.Throws<ArgumentNullException>(() => heap.BuildHeap(null, 0));
        }

        [Test]
        public void BuildHeap_InvalidLength_ThrowsArgumentOutOfRangeException()
        {
            var heap = new Heap<int>();
            int[] values = { 1, 2, 3 };
            
            Assert.Throws<ArgumentOutOfRangeException>(() => heap.BuildHeap(values, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => heap.BuildHeap(values, 10));
        }

        [Test]
        public void BuildHeap_ExceedsCapacity_AutomaticallyResizes()
        {
            var heap = new Heap<int>(2);
            int[] values = { 1, 2, 3, 4, 5 };
            
            heap.BuildHeap(values, values.Length);
            
            Assert.AreEqual(5, heap.Count);
            Assert.GreaterOrEqual(heap.Capacity, 5);
        }

        #endregion

        #region IsValidHeap Tests

        [Test]
        public void IsValidHeap_EmptyHeap_ReturnsTrue()
        {
            var heap = new Heap<int>();
            
            Assert.IsTrue(heap.IsValidHeap());
        }

        [Test]
        public void IsValidHeap_SingleElement_ReturnsTrue()
        {
            var heap = new Heap<int>();
            heap.Add(42);
            
            Assert.IsTrue(heap.IsValidHeap());
        }

        [Test]
        public void IsValidHeap_AfterMultipleOperations_RemainsValid()
        {
            var heap = new Heap<int>();
            
            // Add items
            for (int i = 10; i >= 1; i--)
                heap.Add(i);
            
            Assert.IsTrue(heap.IsValidHeap());
            
            // Remove some items
            for (int i = 0; i < 5; i++)
                heap.RemoveRoot();
            
            Assert.IsTrue(heap.IsValidHeap());
        }

        #endregion

        #region ToArray Tests

        [Test]
        public void ToArray_NonEmptyHeap_ReturnsCorrectArray()
        {
            var heap = new Heap<int>();
            heap.Add(5);
            heap.Add(3);
            heap.Add(7);
            
            int[] array = heap.ToArray();
            
            Assert.AreEqual(3, array.Length);
            Assert.AreEqual(heap.Count, array.Length);
        }

        [Test]
        public void ToArray_EmptyHeap_ReturnsEmptyArray()
        {
            var heap = new Heap<int>();
            
            int[] array = heap.ToArray();
            
            Assert.AreEqual(0, array.Length);
        }

        [Test]
        public void ToArray_DoesNotModifyHeap()
        {
            var heap = new Heap<int>();
            heap.Add(5);
            heap.Add(3);
            
            int countBefore = heap.Count;
            int[] array = heap.ToArray();
            
            Assert.AreEqual(countBefore, heap.Count);
        }

        #endregion

        #region Stress Tests

        [Test]
        public void StressTest_LargeNumberOfItems_MaintainsHeapProperty()
        {
            var heap = new Heap<int>(16, isMinHeap: true);
            var random = new System.Random(42);
            
            // Add 1000 random items
            for (int i = 0; i < 1000; i++)
                heap.Add(random.Next(10000));
            
            Assert.AreEqual(1000, heap.Count);
            Assert.IsTrue(heap.IsValidHeap());
            
            // Remove all items and verify sorted order
            int previous = int.MinValue;
            while (heap.Count > 0)
            {
                int current = heap.RemoveRoot();
                Assert.GreaterOrEqual(current, previous);
                previous = current;
            }
        }

        [Test]
        public void StressTest_AlternatingAddRemove_MaintainsValidity()
        {
            var heap = new Heap<int>(16, isMinHeap: true);
            var random = new System.Random(42);
            
            for (int i = 0; i < 100; i++)
            {
                // Add 10 items
                for (int j = 0; j < 10; j++)
                    heap.Add(random.Next(1000));
                
                Assert.IsTrue(heap.IsValidHeap());
                
                // Remove 5 items
                for (int j = 0; j < 5 && heap.Count > 0; j++)
                    heap.RemoveRoot();
                
                Assert.IsTrue(heap.IsValidHeap());
            }
        }

        #endregion

        #region Custom Type Tests

        private class Person : IComparable<Person>
        {
            public string Name { get; set; }
            public int Age { get; set; }

            public int CompareTo(Person other)
            {
                if (other == null) return 1;
                return Age.CompareTo(other.Age);
            }
        }

        [Test]
        public void CustomType_MinHeap_OrdersByAge()
        {
            var heap = new Heap<Person>(8, isMinHeap: true);
            
            heap.Add(new Person { Name = "Alice", Age = 30 });
            heap.Add(new Person { Name = "Bob", Age = 25 });
            heap.Add(new Person { Name = "Charlie", Age = 35 });
            
            var youngest = heap.RemoveRoot();
            
            Assert.AreEqual("Bob", youngest.Name);
            Assert.AreEqual(25, youngest.Age);
        }

        [Test]
        public void CustomType_MaxHeap_OrdersByAge()
        {
            var heap = new Heap<Person>(8, isMinHeap: false);
            
            heap.Add(new Person { Name = "Alice", Age = 30 });
            heap.Add(new Person { Name = "Bob", Age = 25 });
            heap.Add(new Person { Name = "Charlie", Age = 35 });
            
            var oldest = heap.RemoveRoot();
            
            Assert.AreEqual("Charlie", oldest.Name);
            Assert.AreEqual(35, oldest.Age);
        }

        #endregion

        #region Edge Cases

        [Test]
        public void EdgeCase_SingleCapacity_WorksCorrectly()
        {
            var heap = new Heap<int>(1);
            heap.Add(5);
            
            Assert.AreEqual(1, heap.Count);
            Assert.AreEqual(5, heap.Peek());
            
            heap.Add(3); // Should trigger resize
            Assert.AreEqual(2, heap.Count);
        }

        [Test]
        public void EdgeCase_AllSameValues_WorksCorrectly()
        {
            var heap = new Heap<int>();
            for (int i = 0; i < 10; i++)
                heap.Add(42);
            
            Assert.AreEqual(10, heap.Count);
            Assert.IsTrue(heap.IsValidHeap());
            
            while (heap.Count > 0)
                Assert.AreEqual(42, heap.RemoveRoot());
        }

        [Test]
        public void EdgeCase_AlreadySorted_Ascending()
        {
            var heap = new Heap<int>(8, isMinHeap: true);
            for (int i = 1; i <= 8; i++)
                heap.Add(i);
            
            Assert.IsTrue(heap.IsValidHeap());
            Assert.AreEqual(1, heap.Peek());
        }

        [Test]
        public void EdgeCase_AlreadySorted_Descending()
        {
            var heap = new Heap<int>(8, isMinHeap: true);
            for (int i = 8; i >= 1; i--)
                heap.Add(i);
            
            Assert.IsTrue(heap.IsValidHeap());
            Assert.AreEqual(1, heap.Peek());
        }

        #endregion
    }
}