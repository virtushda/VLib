using System;
using System.Runtime.CompilerServices;
using Libraries.KeyedAccessors.Lightweight;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using VLib.Iterators;

namespace VLib.SpatialAcceleration
{
    /// <summary> No write operations are thread-safe. This version does not use memory indirection for copy protection. </summary>
    public struct UnsafeSpatialHashGrid<T>
        where T : unmanaged, IEquatable<T>, ISpatialHashElement
    {
        internal float cellSize;
        internal UnsafeKeyedList<T> elements;
        internal UnsafeHashMap<int, int2x2> elementIndexToCellBounds;
        internal UnsafeKeyedListPair<int2, Cell> cells;

        public int Count => elements.Length;

        internal struct Cell : IEquatable<Cell>
        {
            int2 cellCoord;
            public UnsafeKeyedList<int> elementIndices;

            public Cell(int2 cellCoord)
            {
                this.cellCoord = cellCoord;
                elementIndices = new UnsafeKeyedList<int>(Allocator.Persistent);
            }

            public void Dispose() => elementIndices.Dispose();

            public bool Equals(Cell other) => cellCoord.Equals(other.cellCoord);
            public override bool Equals(object obj) => throw new NotImplementedException("NO BOXING (no capes!)"); //obj is Cell other && Equals(other);
            public override int GetHashCode() => cellCoord.GetHashCode();
            public static bool operator ==(Cell left, Cell right) => left.Equals(right);
            public static bool operator !=(Cell left, Cell right) => !left.Equals(right);
        }

        public UnsafeSpatialHashGrid(float cellSize, int initCapacity = 1024)
        {
            this.cellSize = cellSize;
            elements = new UnsafeKeyedList<T>(Allocator.Persistent, initCapacity);
            elementIndexToCellBounds = new UnsafeHashMap<int, int2x2>(initCapacity, Allocator.Persistent);
            cells = new UnsafeKeyedListPair<int2, Cell>(Allocator.Persistent);
        }

        public void Dispose()
        {
            elements.Dispose();
            elementIndexToCellBounds.Dispose();
            cells.Dispose();
        }

        public bool AddUpdate(in T value)
        {
            var boundsIncExc = ComputeCellBoundsIncExc(cellSize, value.SpatialHashPosition, value.SpatialHashHalfSize);

            // ADD
            if (elements.Add(value, out var valueIndex))
            {
                AddInternal(value, valueIndex, boundsIncExc);
                return true;
            }
            // UPDATE
            else
            {
                // Copy new value in to support updating value types
                elements.keys[valueIndex] = value;

                var prevBoundsIncExc = elementIndexToCellBounds[valueIndex];
                if (!prevBoundsIncExc.Equals(boundsIncExc))
                    MoveInternal(value, valueIndex, prevBoundsIncExc, boundsIncExc);
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(in T value) => elements.ContainsKey(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T ElementAt(int index) => ref elements.keys.ElementAt(index);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetElementIndex(in T value, out int index) => elements.keyIndexMap.TryGetValue(value, out index);

        public bool Remove(in T value)
        {
            if (!elements.keyIndexMap.TryGetValue(value, out var removalIndex))
                return false;
            var removalBoundsIncExc = elementIndexToCellBounds[removalIndex];
            RemoveInternal(value, removalIndex, removalBoundsIncExc);
            return true;
        }

        /// <summary> Simple, does not do any per-element testing. Does not return duplicates.
        /// These indices will only be valid until the collection is modified next. </summary>
        public void GetAllIndicesIntersecting(in RectNative worldRectXZ, ref UnsafeList<int> resultIndices)
        {
            var cellBoundsIncExc = ComputeCellBoundsIncExc(cellSize, worldRectXZ);

            // If single cell coverage
            if (cellBoundsIncExc.c0.Equals(cellBoundsIncExc.c1 - 1))
            {
                ref var cell = ref GetCellRef(cellBoundsIncExc.c0);
                resultIndices.AddRange(cell.elementIndices.keys);
            }
            // Multiple cells, potential repeated elements
            else
            {
                var initCapacity = math.min(elements.Length, worldRectXZ.CountInt);
                var elementFilter = new UnsafeHashSet<int>(initCapacity, Allocator.Temp);

                var iterator = new IteratorInt2Coords(cellBoundsIncExc, out var iteratorValue);
                while (iterator.MoveNext(ref iteratorValue))
                {
                    ref var cell = ref GetCellRef(iteratorValue);
                    for (var i = 0; i < cell.elementIndices.Length; i++)
                    {
                        var elementIndex = cell.elementIndices.keys[i];
                        if (elementFilter.Add(elementIndex))
                            resultIndices.Add(elementIndex);
                    }
                }

                elementFilter.Dispose();
            }
        }

        // TODO: Finish if needed
        /*/// <summary> Good for larger queries, culls cells that don't intersect the circle. </summary>
        public void GetAllIndicesIntersecting(in CircleNative worldCircle, ref UnsafeList<int> resultIndices)
        {
            var cellBoundsIncExc = ComputeCellBoundsIncExc(cellSize, worldCircle.GetBounds());

            // If single cell coverage
            if (cellBoundsIncExc.c0.Equals(cellBoundsIncExc.c1 - 1))
            {
                ref var cell = ref GetCellRef(cellBoundsIncExc.c0);
                resultIndices.AddRange(cell.elementIndices.keys);
            }
            // Multiple cells, potential repeated elements
            else
            {
                var initCapacity = (int)math.min(elements.Length, worldCircle.Area());
                var elementFilter = new UnsafeHashSet<int>(initCapacity, Allocator.Temp);

                var iterator = new IteratorInt2Coords(cellBoundsIncExc, out var iteratorValue);
                while (iterator.MoveNext(ref iteratorValue))
                {
                    ref var cell = ref GetCellRef(iteratorValue);
                    for (var i = 0; i < cell.elementIndices.Length; i++)
                    {
                        var elementIndex = cell.elementIndices.keys[i];
                        if (elementFilter.Add(elementIndex))
                            resultIndices.Add(elementIndex);
                    }
                }

                elementFilter.Dispose();
            }
        }*/

        void AddInternal(in T value, int valueIndex, in int2x2 bounds)
        {
            // Inject into bins
            var iterator = new IteratorInt2Coords(bounds, out var iteratorValue);
            while (iterator.MoveNext(ref iteratorValue))
            {
                ref var cell = ref GetCellRef(iteratorValue);
                cell.elementIndices.Add(valueIndex, out _);
            }
        }

        void MoveInternal(in T value, int valueIndex, in int2x2 oldBounds, in int2x2 newBounds)
        {
            // Yoink the indices out of the cells the old bounds cover
            var oldIterator = new IteratorInt2Coords(oldBounds, out var oldIteratorValue);
            while (oldIterator.MoveNext(ref oldIteratorValue))
            {
                ref var cell = ref GetCellRef(oldIteratorValue);
                cell.elementIndices.RemoveSwapBack(valueIndex, out _);
            }

            // Add the indices to the cells the new bounds cover
            AddInternal(value, valueIndex, newBounds);
        }

        void RemoveInternal(in T value, int removalIndex, in int2x2 boundsIncExc)
        {
            elements.RemoveAtSwapBack(value, removalIndex);
            // Where the 'swapbacked in' element WAS before it was moved to overwrite the removed element
            var otherElementOLDIndex = elements.Length;

            // Remove element from all cells it was assigned to
            var iterator = new IteratorInt2Coords(boundsIncExc, out var iteratorValue);
            while (iterator.MoveNext(ref iteratorValue))
            {
                ref var cell = ref GetCellRef(iteratorValue);
                cell.elementIndices.RemoveSwapBack(removalIndex, out _);
            }

            // Remove the OTHER element's OLD index
            var otherElementBoundsIncExc = elementIndexToCellBounds[otherElementOLDIndex];
            elementIndexToCellBounds.Remove(otherElementOLDIndex);
            var otherIterator = new IteratorInt2Coords(otherElementBoundsIncExc, out var otherIteratorValue);
            while (otherIterator.MoveNext(ref otherIteratorValue))
            {
                ref var cell = ref GetCellRef(otherIteratorValue);
                cell.elementIndices.RemoveSwapBack(otherElementOLDIndex, out _);
            }

            // Insert the OTHER element's NEW index (which is the removal index)
            otherIteratorValue = otherIterator.GetStartingIteratorValue();
            while (otherIterator.MoveNext(ref otherIteratorValue))
            {
                ref var cell = ref GetCellRef(otherIteratorValue);
                cell.elementIndices.Add(removalIndex, out _);
            }

            // Put the OTHER element's bounds back in the map with the new index
            elementIndexToCellBounds[removalIndex] = otherElementBoundsIncExc;
        }

        ref Cell GetCellRef(int2 cellCoord)
        {
            if (!cells.TryGetIndex(cellCoord, out var cellIndex))
            {
                var newCell = new Cell(cellCoord);
                cells.Add(cellCoord, newCell, out cellIndex);
            }

            return ref cells.values.ElementAt(cellIndex);
        }

        /// <summary> Is not concurrent safe, or safe to use during write operations to the maintaining <see cref="UnsafeSpatialHashGrid{T}"/> instance. </summary>
        public struct ElementRefIterator
        {
            UnsafeList<T> elementKeys;
            UnsafeList<int> indicesToRead;
            int currentIndex;

            public ElementRefIterator(in UnsafeSpatialHashGrid<T> grid, UnsafeList<int> indicesToRead)
            {
                elementKeys = grid.elements.keys;
                this.indicesToRead = indicesToRead;
                currentIndex = -1;
            }

            public ref T Current => ref elementKeys.ElementAt(indicesToRead[currentIndex]);

            public bool MoveNext()
            {
                ++currentIndex;
                return currentIndex < indicesToRead.Length;
            }
        }

        static int2x2 ComputeCellBoundsIncExc(float cellSize, float2 position, float radius)
        {
            var min = (int2) math.floor((position - radius) / cellSize);
            var max = (int2) math.ceil((position + radius) / cellSize);
            return new int2x2(min, max);
        }

        static int2x2 ComputeCellBoundsIncExc(float cellSize, RectNative rectNative)
        {
            var min = (int2) math.floor((rectNative.Min) / cellSize);
            var max = (int2) math.ceil((rectNative.Max) / cellSize);
            return new int2x2(min, max);
        }
    }
}