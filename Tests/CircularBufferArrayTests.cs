using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace VLib.Tests
{
    public class CircularBufferArrayTests
    {
        // ===== Constructor Tests =====
        
        [Test]
        public void Constructor_ValidCapacity_CreatesBuffer()
        {
            // Arrange & Act
            var buffer = new CircularBufferArray<int>(5);
            
            // Assert
            Assert.AreEqual(5, buffer.Capacity);
            Assert.AreEqual(0, buffer.Count);
        }
        
        [Test]
        public void Constructor_ZeroCapacity_ThrowsException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new CircularBufferArray<int>(0));
        }
        
        [Test]
        public void Constructor_NegativeCapacity_ThrowsException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new CircularBufferArray<int>(-1));
        }
        
        // ===== Add Tests =====
        
        [Test]
        public void Add_SingleItem_IncreasesCount()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            
            // Act
            buffer.Add(10);
            
            // Assert
            Assert.AreEqual(1, buffer.Count);
            Assert.AreEqual(10, buffer[0]);
        }
        
        [Test]
        public void Add_MultipleItems_MaintainsOrder()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            
            // Act
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            
            // Assert
            Assert.AreEqual(3, buffer.Count);
            Assert.AreEqual(1, buffer[0]);
            Assert.AreEqual(2, buffer[1]);
            Assert.AreEqual(3, buffer[2]);
        }
        
        [Test]
        public void Add_BeyondCapacity_OverwritesOldest()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(3);
            
            // Act
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4); // Should overwrite 1
            buffer.Add(5); // Should overwrite 2
            
            // Assert
            Assert.AreEqual(3, buffer.Count);
            Assert.AreEqual(3, buffer[0]);
            Assert.AreEqual(4, buffer[1]);
            Assert.AreEqual(5, buffer[2]);
        }
        
        // ===== Enqueue Tests =====
        
        [Test]
        public void Enqueue_SingleItem_IncreasesCount()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            
            // Act
            buffer.Enqueue(10);
            
            // Assert
            Assert.AreEqual(1, buffer.Count);
            Assert.AreEqual(10, buffer[0]);
        }
        
        [Test]
        public void Enqueue_MultipleItems_MaintainsOrder()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            
            // Act
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);
            
            // Assert
            Assert.AreEqual(3, buffer.Count);
            Assert.AreEqual(1, buffer[0]);
            Assert.AreEqual(2, buffer[1]);
            Assert.AreEqual(3, buffer[2]);
        }
        
        [Test]
        public void Enqueue_BeyondCapacity_OverwritesOldest()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(3);
            
            // Act
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);
            buffer.Enqueue(4); // Should overwrite 1
            
            // Assert
            Assert.AreEqual(3, buffer.Count);
            Assert.AreEqual(2, buffer[0]);
            Assert.AreEqual(3, buffer[1]);
            Assert.AreEqual(4, buffer[2]);
        }
        
        [Test]
        public void Enqueue_IdenticalToAdd_ProducesSameResult()
        {
            // Arrange
            var buffer1 = new CircularBufferArray<int>(5);
            var buffer2 = new CircularBufferArray<int>(5);
            
            // Act
            for (int i = 0; i < 7; i++)
            {
                buffer1.Add(i);
                buffer2.Enqueue(i);
            }
            
            // Assert
            Assert.AreEqual(buffer1.Count, buffer2.Count);
            for (int i = 0; i < buffer1.Count; i++)
            {
                Assert.AreEqual(buffer1[i], buffer2[i]);
            }
        }
        
        // ===== Dequeue Tests =====
        
        [Test]
        public void Dequeue_SingleItem_ReturnsAndRemovesItem()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Enqueue(10);
            
            // Act
            var result = buffer.Dequeue();
            
            // Assert
            Assert.AreEqual(10, result);
            Assert.AreEqual(0, buffer.Count);
        }
        
        [Test]
        public void Dequeue_MultipleItems_ReturnsFIFOOrder()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);
            
            // Act
            var first = buffer.Dequeue();
            var second = buffer.Dequeue();
            var third = buffer.Dequeue();
            
            // Assert
            Assert.AreEqual(1, first);
            Assert.AreEqual(2, second);
            Assert.AreEqual(3, third);
            Assert.AreEqual(0, buffer.Count);
        }
        
        [Test]
        public void Dequeue_EmptyBuffer_ThrowsInvalidOperationException()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => buffer.Dequeue());
        }
        
        [Test]
        public void Dequeue_AfterFullDequeue_ThrowsException()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(3);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Dequeue();
            buffer.Dequeue();
            
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => buffer.Dequeue());
        }
        
        [Test]
        public void Dequeue_AfterWraparound_ReturnsCorrectValues()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(3);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);
            buffer.Enqueue(4); // Wraps, buffer now [2, 3, 4]
            
            // Act
            var first = buffer.Dequeue();
            var second = buffer.Dequeue();
            
            // Assert
            Assert.AreEqual(2, first);
            Assert.AreEqual(3, second);
            Assert.AreEqual(1, buffer.Count);
            Assert.AreEqual(4, buffer[0]);
        }
        
        [Test]
        public void Dequeue_DecreasesCount()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);
            
            // Act & Assert
            Assert.AreEqual(3, buffer.Count);
            buffer.Dequeue();
            Assert.AreEqual(2, buffer.Count);
            buffer.Dequeue();
            Assert.AreEqual(1, buffer.Count);
            buffer.Dequeue();
            Assert.AreEqual(0, buffer.Count);
        }
        
        // ===== Queue Behavior Tests (Enqueue + Dequeue) =====
        
        [Test]
        public void QueueBehavior_AlternatingEnqueueDequeue_MaintainsCorrectness()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            
            // Act & Assert
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            Assert.AreEqual(1, buffer.Dequeue());
            buffer.Enqueue(3);
            buffer.Enqueue(4);
            Assert.AreEqual(2, buffer.Dequeue());
            Assert.AreEqual(3, buffer.Dequeue());
            buffer.Enqueue(5);
            Assert.AreEqual(4, buffer.Dequeue());
            Assert.AreEqual(5, buffer.Dequeue());
        }
        
        [Test]
        public void QueueBehavior_EnqueueDequeueWithWraparound_WorksCorrectly()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(3);
            
            // Act
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);
            var d1 = buffer.Dequeue(); // [2, 3]
            var d2 = buffer.Dequeue(); // [3]
            buffer.Enqueue(4); // [3, 4]
            buffer.Enqueue(5); // [3, 4, 5]
            
            // Assert
            Assert.AreEqual(1, d1);
            Assert.AreEqual(2, d2);
            Assert.AreEqual(3, buffer.Count);
            Assert.AreEqual(3, buffer[0]);
            Assert.AreEqual(4, buffer[1]);
            Assert.AreEqual(5, buffer[2]);
        }
        
        [Test]
        public void QueueBehavior_MultipleWrapCycles_MaintainsIntegrity()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(3);
            
            // Act - Perform multiple wrap cycles
            for (int i = 0; i < 10; i++)
            {
                buffer.Enqueue(i);
            }
            
            // Buffer should contain last 3 values [7, 8, 9]
            Assert.AreEqual(7, buffer.Dequeue());
            Assert.AreEqual(8, buffer.Dequeue());
            Assert.AreEqual(9, buffer.Dequeue());
            Assert.AreEqual(0, buffer.Count);
        }
        
        [Test]
        public void QueueBehavior_ContinuousUse_HandlesLargeNumberOfOperations()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(10);
    
            // Act - Enqueue many items, letting buffer auto-overwrite oldest
            for (int i = 0; i < 1000; i++)
            {
                buffer.Enqueue(i);
            }
    
            // Assert - Buffer should contain last 10 items [990-999]
            Assert.AreEqual(10, buffer.Count);
            for (int i = 0; i < 10; i++)
                Assert.AreEqual(990 + i, buffer.Dequeue());
        }
        
        // ===== Indexer Tests =====
        
        [Test]
        public void Indexer_Get_ReturnsCorrectValue()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Add(10);
            buffer.Add(20);
            buffer.Add(30);
            
            // Act & Assert
            Assert.AreEqual(10, buffer[0]);
            Assert.AreEqual(20, buffer[1]);
            Assert.AreEqual(30, buffer[2]);
        }
        
        [Test]
        public void Indexer_Set_UpdatesValue()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Add(10);
            buffer.Add(20);
            
            // Act
            buffer[1] = 25;
            
            // Assert
            Assert.AreEqual(25, buffer[1]);
        }
        
        [Test]
        public void Indexer_Get_OutOfRange_ThrowsException()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Add(10);
            
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = buffer[1]; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = buffer[-1]; });
        }
        
        [Test]
        public void Indexer_Set_OutOfRange_ThrowsException()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Add(10);
            
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer[1] = 20);
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer[-1] = 20);
        }
        
        [Test]
        public void Indexer_AfterWraparound_ReturnsCorrectValues()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(3);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4); // Wraps around
            
            // Act & Assert
            Assert.AreEqual(2, buffer[0]);
            Assert.AreEqual(3, buffer[1]);
            Assert.AreEqual(4, buffer[2]);
        }
        
        [Test]
        public void Indexer_AfterDequeue_ReturnsCorrectValues()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);
            buffer.Dequeue(); // Remove 1
            
            // Act & Assert
            Assert.AreEqual(2, buffer[0]);
            Assert.AreEqual(3, buffer[1]);
        }
        
        // ===== ElementAt Tests =====
        
        [Test]
        public void ElementAt_ReturnsRefToElement()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Add(10);
            buffer.Add(20);
            
            // Act
            ref var element = ref buffer.ElementAt(1);
            element = 25;
            
            // Assert
            Assert.AreEqual(25, buffer[1]);
        }
        
        [Test]
        public void ElementAt_OutOfRange_ThrowsException()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Add(10);
            
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.ElementAt(1));
        }
        
        [Test]
        public void ElementAt_AfterDequeue_ReturnsCorrectReference()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Enqueue(10);
            buffer.Enqueue(20);
            buffer.Enqueue(30);
            buffer.Dequeue(); // Remove 10
            
            // Act
            ref var element = ref buffer.ElementAt(0);
            
            // Assert
            Assert.AreEqual(20, element);
            element = 200;
            Assert.AreEqual(200, buffer[0]);
        }
        
        // ===== Capacity Tests =====
        
        [Test]
        public void Capacity_Set_IncreaseCapacity_PreservesElements()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(3);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            
            // Act
            buffer.Capacity = 5;
            
            // Assert
            Assert.AreEqual(5, buffer.Capacity);
            Assert.AreEqual(3, buffer.Count);
            Assert.AreEqual(1, buffer[0]);
            Assert.AreEqual(2, buffer[1]);
            Assert.AreEqual(3, buffer[2]);
        }
        
        [Test]
        public void Capacity_Set_DecreaseCapacity_TruncatesElements()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4);
            
            // Act
            buffer.Capacity = 2;
            
            // Assert
            Assert.AreEqual(2, buffer.Capacity);
            Assert.AreEqual(2, buffer.Count);
            Assert.AreEqual(1, buffer[0]);
            Assert.AreEqual(2, buffer[1]);
        }
        
        [Test]
        public void Capacity_Set_AfterWraparound_PreservesCorrectElements()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(3);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4); // Now contains [2, 3, 4]
            
            // Act
            buffer.Capacity = 5;
            
            // Assert
            Assert.AreEqual(2, buffer[0]);
            Assert.AreEqual(3, buffer[1]);
            Assert.AreEqual(4, buffer[2]);
        }
        
        [Test]
        public void Capacity_Set_AfterDequeue_PreservesRemainingElements()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);
            buffer.Dequeue(); // [2, 3]
            
            // Act
            buffer.Capacity = 3;
            
            // Assert
            Assert.AreEqual(2, buffer.Count);
            Assert.AreEqual(2, buffer[0]);
            Assert.AreEqual(3, buffer[1]);
        }
        
        [Test]
        public void Capacity_Set_ZeroOrNegative_ThrowsException()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Capacity = 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Capacity = -1);
        }
        
        [Test]
        public void Capacity_Set_SameValue_DoesNothing()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            
            // Act
            buffer.Capacity = 5;
            
            // Assert
            Assert.AreEqual(5, buffer.Capacity);
            Assert.AreEqual(2, buffer.Count);
        }
        
        // ===== Clear Tests =====
        
        [Test]
        public void Clear_ResetsCountAndHead()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            
            // Act
            buffer.Clear();
            
            // Assert
            Assert.AreEqual(0, buffer.Count);
        }
        
        [Test]
        public void Clear_AfterClear_CanAddAgain()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(3);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Clear();
            
            // Act
            buffer.Add(10);
            buffer.Add(20);
            
            // Assert
            Assert.AreEqual(2, buffer.Count);
            Assert.AreEqual(10, buffer[0]);
            Assert.AreEqual(20, buffer[1]);
        }
        
        [Test]
        public void Clear_AfterDequeue_ResetsBuffer()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Dequeue();
            
            // Act
            buffer.Clear();
            
            // Assert
            Assert.AreEqual(0, buffer.Count);
            Assert.Throws<InvalidOperationException>(() => buffer.Dequeue());
        }
        
        [Test]
        public void Clear_ThenEnqueue_WorksCorrectly()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(3);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Clear();
            
            // Act
            buffer.Enqueue(10);
            buffer.Enqueue(20);
            
            // Assert
            Assert.AreEqual(10, buffer.Dequeue());
            Assert.AreEqual(20, buffer.Dequeue());
        }
        
        // ===== Contains Tests =====
        
        [Test]
        public void Contains_ExistingItem_ReturnsTrue()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            
            // Act & Assert
            Assert.IsTrue(buffer.Contains(2));
        }
        
        [Test]
        public void Contains_NonExistingItem_ReturnsFalse()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            
            // Act & Assert
            Assert.IsFalse(buffer.Contains(5));
        }
        
        [Test]
        public void Contains_EmptyBuffer_ReturnsFalse()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            
            // Act & Assert
            Assert.IsFalse(buffer.Contains(1));
        }
        
        [Test]
        public void Contains_AfterDequeue_ChecksDequeuedItem_ReturnsFalse()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);
            buffer.Dequeue(); // Remove 1
            
            // Act & Assert
            Assert.IsFalse(buffer.Contains(1));
            Assert.IsTrue(buffer.Contains(2));
            Assert.IsTrue(buffer.Contains(3));
        }
        
        // ===== IndexOf Tests =====
        
        [Test]
        public void IndexOf_ExistingItem_ReturnsCorrectIndex()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Add(10);
            buffer.Add(20);
            buffer.Add(30);
            
            // Act & Assert
            Assert.AreEqual(0, buffer.IndexOf(10));
            Assert.AreEqual(1, buffer.IndexOf(20));
            Assert.AreEqual(2, buffer.IndexOf(30));
        }
        
        [Test]
        public void IndexOf_NonExistingItem_ReturnsNegativeOne()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Add(10);
            buffer.Add(20);
            
            // Act & Assert
            Assert.AreEqual(-1, buffer.IndexOf(30));
        }
        
        [Test]
        public void IndexOf_AfterWraparound_ReturnsCorrectIndex()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(3);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4); // [2, 3, 4]
            
            // Act & Assert
            Assert.AreEqual(0, buffer.IndexOf(2));
            Assert.AreEqual(2, buffer.IndexOf(4));
            Assert.AreEqual(-1, buffer.IndexOf(1)); // Overwritten
        }
        
        [Test]
        public void IndexOf_AfterDequeue_ReturnsCorrectIndex()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Enqueue(10);
            buffer.Enqueue(20);
            buffer.Enqueue(30);
            buffer.Dequeue(); // Remove 10
            
            // Act & Assert
            Assert.AreEqual(0, buffer.IndexOf(20));
            Assert.AreEqual(1, buffer.IndexOf(30));
            Assert.AreEqual(-1, buffer.IndexOf(10));
        }
        
        // ===== CopyTo Tests =====
        
        [Test]
        public void CopyTo_CopiesAllElements()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            var array = new int[5];
            
            // Act
            buffer.CopyTo(array, 0);
            
            // Assert
            Assert.AreEqual(1, array[0]);
            Assert.AreEqual(2, array[1]);
            Assert.AreEqual(3, array[2]);
        }
        
        [Test]
        public void CopyTo_WithOffset_CopiesCorrectly()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Add(10);
            buffer.Add(20);
            var array = new int[5];
            
            // Act
            buffer.CopyTo(array, 2);
            
            // Assert
            Assert.AreEqual(0, array[0]);
            Assert.AreEqual(0, array[1]);
            Assert.AreEqual(10, array[2]);
            Assert.AreEqual(20, array[3]);
        }
        
        [Test]
        public void CopyTo_AfterDequeue_CopiesRemainingElements()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);
            buffer.Dequeue(); // Remove 1
            var array = new int[5];
            
            // Act
            buffer.CopyTo(array, 0);
            
            // Assert
            Assert.AreEqual(2, array[0]);
            Assert.AreEqual(3, array[1]);
        }
        
        // ===== Remove Tests =====
        
        [Test]
        public void Remove_ExistingItem_RemovesAndReturnsTrue()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            
            // Act
            var result = buffer.Remove(2);
            
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(2, buffer.Count);
            Assert.AreEqual(1, buffer[0]);
            Assert.AreEqual(3, buffer[1]);
        }
        
        [Test]
        public void Remove_NonExistingItem_ReturnsFalse()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            
            // Act
            var result = buffer.Remove(5);
            
            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(2, buffer.Count);
        }
        
        // ===== RemoveAt Tests =====
        
        [Test]
        public void RemoveAt_ValidIndex_RemovesElement()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            
            // Act
            buffer.RemoveAt(1);
            
            // Assert
            Assert.AreEqual(2, buffer.Count);
            Assert.AreEqual(1, buffer[0]);
            Assert.AreEqual(3, buffer[1]);
        }
        
        [Test]
        public void RemoveAt_FirstElement_RemovesCorrectly()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            
            // Act
            buffer.RemoveAt(0);
            
            // Assert
            Assert.AreEqual(2, buffer.Count);
            Assert.AreEqual(2, buffer[0]);
            Assert.AreEqual(3, buffer[1]);
        }
        
        [Test]
        public void RemoveAt_LastElement_RemovesCorrectly()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            
            // Act
            buffer.RemoveAt(2);
            
            // Assert
            Assert.AreEqual(2, buffer.Count);
            Assert.AreEqual(1, buffer[0]);
            Assert.AreEqual(2, buffer[1]);
        }
        
        [Test]
        public void RemoveAt_InvalidIndex_ThrowsException()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Add(1);
            
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.RemoveAt(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.RemoveAt(10));
        }
        
        [Test]
        public void RemoveAt_AfterDequeue_RemovesCorrectElement()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);
            buffer.Dequeue(); // [2, 3]
            
            // Act
            buffer.RemoveAt(0);
            
            // Assert
            Assert.AreEqual(1, buffer.Count);
            Assert.AreEqual(3, buffer[0]);
        }
        
        // ===== Insert Tests =====
        
        [Test]
        public void Insert_ThrowsNotSupportedException()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            
            // Act & Assert
            Assert.Throws<NotSupportedException>(() => buffer.Insert(0, 10));
        }
        
        // ===== Forward Enumerator Tests =====
        
        [Test]
        public void Enumerator_Forward_IteratesAllElements()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            var result = new List<int>();
            
            // Act
            foreach (var item in buffer)
            {
                result.Add(item);
            }
            
            // Assert
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(1, result[0]);
            Assert.AreEqual(2, result[1]);
            Assert.AreEqual(3, result[2]);
        }
        
        [Test]
        public void Enumerator_Forward_EmptyBuffer_NoIterations()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            var count = 0;
            
            // Act
            foreach (var item in buffer)
            {
                count++;
            }
            
            // Assert
            Assert.AreEqual(0, count);
        }
        
        [Test]
        public void Enumerator_Forward_AfterWraparound_IteratesCorrectly()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(3);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4); // [2, 3, 4]
            buffer.Add(5); // [3, 4, 5]
            var result = new List<int>();
            
            // Act
            foreach (var item in buffer)
            {
                result.Add(item);
            }
            
            // Assert
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(3, result[0]);
            Assert.AreEqual(4, result[1]);
            Assert.AreEqual(5, result[2]);
        }
        
        [Test]
        public void Enumerator_Forward_AfterDequeue_IteratesRemainingElements()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);
            buffer.Dequeue(); // Remove 1
            var result = new List<int>();
            
            // Act
            foreach (var item in buffer)
            {
                result.Add(item);
            }
            
            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(2, result[0]);
            Assert.AreEqual(3, result[1]);
        }
        
        [Test]
        public void Enumerator_Forward_Reset_RestartsIteration()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            var enumerator = buffer.GetEnumerator();
            
            // Act
            enumerator.MoveNext();
            enumerator.MoveNext();
            enumerator.Reset();
            var result = new List<int>();
            while (enumerator.MoveNext())
            {
                result.Add(enumerator.Current);
            }
            
            // Assert
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(1, result[0]);
        }
        
        [Test]
        public void Enumerator_Forward_CurrentRef_AllowsModification()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            
            // Act
            var enumerator = buffer.GetEnumerator();
            enumerator.MoveNext();
            enumerator.MoveNext();
            enumerator.CurrentRef = 20;
            
            // Assert
            Assert.AreEqual(20, buffer[1]);
        }
        
        // ===== Reverse Enumerator Tests =====
        
        [Test]
        public void Enumerator_Reverse_IteratesAllElementsBackward()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            var result = new List<int>();
            
            // Act
            var enumerator = buffer.GetReverseEnumerator();
            while (enumerator.MoveNext())
            {
                result.Add(enumerator.Current);
            }
            
            // Assert
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(3, result[0]);
            Assert.AreEqual(2, result[1]);
            Assert.AreEqual(1, result[2]);
        }
        
        [Test]
        public void Enumerator_Reverse_EmptyBuffer_NoIterations()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            var count = 0;
            
            // Act
            var enumerator = buffer.GetReverseEnumerator();
            while (enumerator.MoveNext())
            {
                count++;
            }
            
            // Assert
            Assert.AreEqual(0, count);
        }
        
        [Test]
        public void Enumerator_Reverse_AfterWraparound_IteratesCorrectly()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(3);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4); // [2, 3, 4]
            buffer.Add(5); // [3, 4, 5]
            var result = new List<int>();
            
            // Act
            var enumerator = buffer.GetReverseEnumerator();
            while (enumerator.MoveNext())
            {
                result.Add(enumerator.Current);
            }
            
            // Assert
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(5, result[0]);
            Assert.AreEqual(4, result[1]);
            Assert.AreEqual(3, result[2]);
        }
        
        [Test]
        public void Enumerator_Reverse_AfterDequeue_IteratesRemainingElements()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);
            buffer.Dequeue(); // Remove 1
            var result = new List<int>();
            
            // Act
            var enumerator = buffer.GetReverseEnumerator();
            while (enumerator.MoveNext())
            {
                result.Add(enumerator.Current);
            }
            
            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(3, result[0]);
            Assert.AreEqual(2, result[1]);
        }
        
        [Test]
        public void Enumerator_Reverse_Reset_RestartsIteration()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            var enumerator = buffer.GetReverseEnumerator();
            
            // Act
            enumerator.MoveNext();
            enumerator.MoveNext();
            enumerator.Reset();
            var result = new List<int>();
            while (enumerator.MoveNext())
            {
                result.Add(enumerator.Current);
            }
            
            // Assert
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(3, result[0]);
        }
        
        [Test]
        public void Enumerator_Reverse_CurrentRef_AllowsModification()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            
            // Act
            var enumerator = buffer.GetReverseEnumerator();
            enumerator.MoveNext();
            enumerator.MoveNext();
            enumerator.CurrentRef = 20;
            
            // Assert
            Assert.AreEqual(20, buffer[1]);
        }
        
        // ===== IList Interface Tests =====
        
        [Test]
        public void IsReadOnly_ReturnsFalse()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            
            // Act & Assert
            Assert.IsFalse(buffer.IsReadOnly);
        }
        
        [Test]
        public void LastIndex_ReturnsCorrectValue()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            
            // Act & Assert
            Assert.AreEqual(2, buffer.LastIndex);
        }
        
        [Test]
        public void LastIndex_EmptyBuffer_ReturnsNegativeOne()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            
            // Act & Assert
            Assert.AreEqual(-1, buffer.LastIndex);
        }
        
        [Test]
        public void LastIndex_AfterDequeue_ReturnsCorrectValue()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(5);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);
            buffer.Dequeue();
            
            // Act & Assert
            Assert.AreEqual(1, buffer.LastIndex);
        }
        
        // ===== Reference Type Tests =====
        
        [Test]
        public void Buffer_WithReferenceTypes_WorksCorrectly()
        {
            // Arrange
            var buffer = new CircularBufferArray<string>(3);
            
            // Act
            buffer.Add("first");
            buffer.Add("second");
            buffer.Add("third");
            
            // Assert
            Assert.AreEqual("first", buffer[0]);
            Assert.AreEqual("second", buffer[1]);
            Assert.AreEqual("third", buffer[2]);
        }
        
        [Test]
        public void Buffer_WithReferenceTypes_Wraparound_OverwritesCorrectly()
        {
            // Arrange
            var buffer = new CircularBufferArray<string>(2);
            
            // Act
            buffer.Add("first");
            buffer.Add("second");
            buffer.Add("third"); // Overwrites "first"
            
            // Assert
            Assert.AreEqual(2, buffer.Count);
            Assert.AreEqual("second", buffer[0]);
            Assert.AreEqual("third", buffer[1]);
        }
        
        [Test]
        public void Buffer_WithReferenceTypes_Dequeue_WorksCorrectly()
        {
            // Arrange
            var buffer = new CircularBufferArray<string>(3);
            buffer.Enqueue("first");
            buffer.Enqueue("second");
            buffer.Enqueue("third");
            
            // Act
            var result = buffer.Dequeue();
            
            // Assert
            Assert.AreEqual("first", result);
            Assert.AreEqual(2, buffer.Count);
        }
        
        // ===== Stress Tests =====
        
        [Test]
        public void StressTest_ManyAdds_MaintainsIntegrity()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(100);
            
            // Act
            for (int i = 0; i < 1000; i++)
            {
                buffer.Add(i);
            }
            
            // Assert
            Assert.AreEqual(100, buffer.Count);
            Assert.AreEqual(900, buffer[0]); // First element should be 900
            Assert.AreEqual(999, buffer[99]); // Last element should be 999
        }
        
        [Test]
        public void StressTest_AlternatingAddAndRemove_MaintainsIntegrity()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(10);
            
            // Act
            for (int i = 0; i < 5; i++)
            {
                buffer.Add(i);
            }
            buffer.RemoveAt(0);
            buffer.RemoveAt(0);
            buffer.Add(10);
            buffer.Add(11);
            
            // Assert
            Assert.AreEqual(5, buffer.Count);
            Assert.AreEqual(2, buffer[0]);
            Assert.AreEqual(11, buffer[4]);
        }
        
        [Test]
        public void StressTest_ManyEnqueueDequeue_MaintainsIntegrity()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(50);
            
            // Act
            for (int i = 0; i < 500; i++)
            {
                buffer.Enqueue(i);
                if (i % 3 == 0 && buffer.Count > 0)
                {
                    buffer.Dequeue();
                }
            }
            
            // Assert
            Assert.IsTrue(buffer.Count <= 50);
            Assert.IsTrue(buffer.Count > 0);
        }
        
        [Test]
        public void StressTest_FillEmptyCycle_WorksCorrectly()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(10);
            
            // Act & Assert - Multiple fill and empty cycles
            for (int cycle = 0; cycle < 100; cycle++)
            {
                for (int i = 0; i < 10; i++)
                {
                    buffer.Enqueue(cycle * 10 + i);
                }
                Assert.AreEqual(10, buffer.Count);
                
                for (int i = 0; i < 10; i++)
                {
                    var value = buffer.Dequeue();
                    Assert.AreEqual(cycle * 10 + i, value);
                }
                Assert.AreEqual(0, buffer.Count);
            }
        }
        
        [Test]
        public void StressTest_QueueUsageWithMixedOperations_MaintainsCorrectness()
        {
            // Arrange
            var buffer = new CircularBufferArray<int>(20);
            var expected = new Queue<int>();
            
            // Act - Mirror operations on both structures
            for (int i = 0; i < 100; i++)
            {
                if (i % 3 == 0 && buffer.Count > 0)
                {
                    Assert.AreEqual(expected.Dequeue(), buffer.Dequeue());
                }
                else if (buffer.Count < 20)
                {
                    buffer.Enqueue(i);
                    expected.Enqueue(i);
                }
                else
                {
                    buffer.Enqueue(i); // Overwrites oldest
                    expected.Dequeue(); // Remove oldest
                    expected.Enqueue(i);
                }
            }
            
            // Assert - Both should have same remaining elements
            Assert.AreEqual(expected.Count, buffer.Count);
        }
    }
}