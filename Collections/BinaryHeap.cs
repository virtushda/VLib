// #define VALIDATE_BINARY_HEAP
#pragma warning disable 162
#pragma warning disable 429
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Mathematics;
using Unity.Collections;

namespace VLib
{
	/// <summary>Make sure the default value for HeapIndex is <see cref="NotInHeap"/></summary>
	public interface IHeapItem
	{
		public const int NotInHeap = -1;
		
		/// <summary>Make sure the default value for HeapIndex is <see cref="NotInHeap"/></summary>
		public int HeapIndex { get; set; }
	}

	/// <summary>
	/// Binary heap implementation.
	/// Binary heaps are really fast for ordering nodes in a way that
	/// makes it possible to get the node with the lowest score.
	/// Also known as a priority queue.
	///
	/// This has actually been rewritten as a 4-ary heap
	/// for performance, but it's the same principle.
	///
	/// See: http://en.wikipedia.org/wiki/Binary_heap
	/// See: https://en.wikipedia.org/wiki/D-ary_heap
	/// </summary>
	public class BinaryHeap<TCost, TItem>
		where TCost : unmanaged, IComparable<TCost>
		where TItem : IHeapItem
	{
		/// <summary>The tree will grow by at least this factor every time it is expanded</summary>
		public const float GrowthFactor = 2;
		
		/// <summary>Number of children of each node in the tree.</summary>
		const int D = 4;

		/// <summary>Internal backing array for the heap</summary>
		private HeapNode[] heap;
		
		private int count;
		public int Count => count;
		
		public bool IsEmpty => count <= 0;
		
		private struct HeapNode
		{
			public TItem item;
			public TCost cost;

			public HeapNode(TItem item, TCost cost)
			{
				this.item = item;
				this.cost = cost;
			}
		}

		/// <summary>Create a new heap with the specified initial capacity</summary>
		public BinaryHeap (int capacity)
		{
			// Make sure the size has remainder 1 when divided by D
			// This allows us to always guarantee that indices used in the Remove method
			// will never throw out of bounds exceptions
			capacity = RoundUpToNextMultipleMod1(capacity);
			heap = new HeapNode[capacity];
			count = 0;
		}

		/// <summary>Removes all elements from the heap</summary>
		public void Clear ()
		{
			// Clear all heap indices
			for (int i = 0; i < count; i++)
			{
				heap[i].item.HeapIndex = IHeapItem.NotInHeap;
				heap[i] = default;
			}
			count = 0;
		}

		public TItem GetItem(int heapIndex) => heap[heapIndex].item;
		public TCost GetCost(int heapIndex) => heap[heapIndex].cost;

		/// <summary>Sorts an item from the items collection into the heap. If the item already exists in the heap, it can be updated with a lower cost, but not a higher one</summary>
		public void AddSet (TItem item, TCost cost)
		{
			// Check if node is already in the heap
			if (item.HeapIndex != IHeapItem.NotInHeap)
			{
				var replacementNode = new HeapNode(item, cost);
				var costComparison = heap[item.HeapIndex].cost.CompareTo(cost);
				switch (costComparison)
				{
					case > 0:
						SortDown(replacementNode, item.HeapIndex);
						break;
					case < 0:
						SortUp(count, replacementNode, item.HeapIndex);
						break;
					default:
						return;
				}
				Validate();
			}
			else
			{
				if (count == heap.Length) 
				{
					Expand();
				}
				Validate();

				SortDown(new HeapNode(item, cost), count);
				count++;
				Validate();
			}
		}
		
		/// <summary>Returns the node with the lowest F score from the heap</summary>
		public TItem Pop (out TCost removedScore)
		{
			if (count == 0)
				throw new InvalidOperationException("Removing item from empty heap");

			// This is the smallest item in the heap.
			// Mark it as removed from the heap.
			Hint.Assume(0 < heap.Length);
			var returnItem = heap[0].item;
			returnItem.HeapIndex = IHeapItem.NotInHeap;
			removedScore = heap[0].cost;

			count--;
			if (count == 0)
				return returnItem;
			
			// Last item in the heap array
			SortUp(count, heap[count], 0);
			return returnItem;
		}
		
		/// <summary>
		/// Rounds up v so that it has remainder 1 when divided by D.
		/// I.e it is of the form n*D + 1 where n is any non-negative integer.
		/// </summary>
		private static int RoundUpToNextMultipleMod1 (int v)
		{
			// "I have a feeling there is a nicer way to do this" - Aron Granberg
			return v + (4 - ((v-1) % D)) % D;
		}
		
		/// <summary>Expands to a larger backing array when the current one is too small</summary>
		private void Expand ()
		{
			int newSize = math.max(heap.Length+4,  (int)math.round(heap.Length * GrowthFactor));

			// Make sure the size has remainder 1 when divided by D
			// This allows us to always guarantee that indices used in the Remove method
			// will never throw out of bounds exceptions
			newSize = RoundUpToNextMultipleMod1(newSize);
			Array.Resize(ref heap, newSize);
		}

		private void SortUp (int heapCount, HeapNode node, int index)
		{
			Hint.Assume(heapCount < heap.Length);
			var swapNodeScore = node.cost;
			
			// Trickle upwards
			while (true)
			{
				var parentIndex = index;
				var firstChildNodeIndex = parentIndex * D + 1;
				
				// If this holds, then the indices used
				// below are guaranteed to not throw an index out of bounds
				// exception since we choose the size of the array in that way
				if (firstChildNodeIndex >= heapCount)
					break;

				var bestChildIndex = firstChildNodeIndex;
				var bestChildScore = heap[bestChildIndex].cost;
				for (int i = 1; i < D; i++)
				{
					var compareIndex = firstChildNodeIndex + i;
					if (compareIndex >= heapCount)
						break;

					Hint.Assume(compareIndex < heap.Length);
					var compareScore = heap[compareIndex].cost;
					if(bestChildScore.CompareTo(compareScore) <= 0)
						continue;

					bestChildIndex = (ushort)compareIndex;
					bestChildScore = compareScore;
				}

				if(bestChildScore.CompareTo(swapNodeScore) > 0)
					break;

				index = bestChildIndex;

				// One if the parent's children are smaller or equal, swap them
				// (actually we are just pretenting we swapped them, we hold the swapItem
				// in local variable and only assign it once we know the final index)
				Hint.Assume(parentIndex < heap.Length);
				Hint.Assume(index < heap.Length);
				heap[parentIndex] = heap[index];
				Hint.Assume(index < heap.Length);
				var item = heap[index].item;
				item.HeapIndex = parentIndex;
				heap[index] = new HeapNode(item, heap[index].cost);
			}

			// Assign element to the final position
			Hint.Assume(index < heap.Length);
			node.item.HeapIndex = index;
			heap[index] = node;
		}

		private void SortDown (HeapNode node, int index)
		{
			while (index != 0)
			{
				var parentIndex = (index - 1) / D;

				Hint.Assume(parentIndex < heap.Length);
				Hint.Assume(index < heap.Length);
				if (node.cost.CompareTo(heap[parentIndex].cost) > 0)
					break;

				var item = heap[parentIndex].item;
				item.HeapIndex = index;
				heap[index] = new HeapNode(item, heap[parentIndex].cost);
				index = parentIndex;
			}

			Hint.Assume(index < heap.Length);
			node.item.HeapIndex = index;
			heap[index] = node;
		}

		[System.Diagnostics.Conditional("VALIDATE_BINARY_HEAP")]
		private void Validate ()
		{
			for (int heapIndex = 1; heapIndex < count; heapIndex++)
			{
				int parentIndex = (heapIndex-1)/D;
				if (heap[parentIndex].cost.CompareTo(heap[heapIndex].cost) <= 0)
				{
					throw new Exception($"Score mismatch! Node at childIndex={heapIndex}, score={heap[heapIndex].cost} must be greater than parentIndex={parentIndex}, score = {heap[parentIndex].cost}");
				}

				if (heap[heapIndex].item.HeapIndex != heapIndex)
				{
					throw new Exception("Invalid heap index");
				}
			}
		}
		
		public override string ToString()
		{
			var numberOfItems = count;

			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			NodeToString(0, 0);

			return sb.ToString();
			
			void NodeToString(int index, int depth)
			{
				for (int i = 0; i < depth; i++)
					sb.Append("    ");
				sb.Append($"item = {heap[index].item}, score = {heap[index].cost}\n");
				
				var firstChildNodeIndex = (ushort)(index * D + 1);
				if (firstChildNodeIndex >= numberOfItems)
					return;
				
				for (int i = 0; i < D; i++)
				{
					var childIndex = firstChildNodeIndex + i;
					if (childIndex >= numberOfItems)
						break;
					
					NodeToString(childIndex, depth + 1);
				}
			}
		}
	}
}
