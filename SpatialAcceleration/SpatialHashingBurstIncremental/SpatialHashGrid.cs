using System;
using Libraries.KeyedAccessors.Lightweight;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using VLib.Iterators;

namespace VLib.SpatialAcceleration
{
    /// <summary> No write operations are thread-safe. </summary>
    public struct SpatialHashGrid<T>
        where T : unmanaged, IEquatable<T>, ISpatialHashElement
    {
        VUnsafeRef<Native> nativeMemory;

        struct Native
        {
            float cellSize;
            UnsafeKeyedList<T> elements;
            UnsafeHashMap<int, int2x2> elementIndexToCellBounds;
            UnsafeKeyedListPair<int2, Cell> cells;

            struct Cell : IEquatable<Cell>
            {
                int2 cellCoord;
                public UnsafeKeyedList<int> elements;

                public Cell(int2 cellCoord)
                {
                    this.cellCoord = cellCoord;
                    elements = new UnsafeKeyedList<int>(Allocator.Persistent);
                }
                
                public void Dispose() => elements.Dispose();

                public bool Equals(Cell other) => cellCoord.Equals(other.cellCoord);
                public override bool Equals(object obj) => throw new NotImplementedException("NO BOXING (no capes!)"); //obj is Cell other && Equals(other);
                public override int GetHashCode() => cellCoord.GetHashCode();
                public static bool operator ==(Cell left, Cell right) => left.Equals(right);
                public static bool operator !=(Cell left, Cell right) => !left.Equals(right);
            }

            /// <summary> Returns true if value was added. </summary>
            public bool AddUpdate(in T value)
            {
                var boundsIncExc = ComputeCellBoundsIncExc(cellSize, value.Position, value.HalfSize);
                
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
            
            public bool Contains(in T value) => elements.ContainsKey(value);

            public void Remove(in T value)
            {
                if (!elements.keyIndexMap.TryGetValue(value, out var removalIndex))
                    return;
                var removalBoundsIncExc = elementIndexToCellBounds[removalIndex];
                RemoveInternal(value, removalIndex, removalBoundsIncExc);
            }

            /// <summary> Simple, does not do any per-element testing. Does not return duplicates.
            /// These indices will only be valid until the collection is modified next. </summary>
            public void GetAllIndicesIntersecting(RectNative worldRectXZ, ref UnsafeList<int> resultIndices)
            {
                var cellBoundsIncExc = ComputeCellBoundsIncExc(cellSize, worldRectXZ);
                
                var initCapacity = worldRectXZ.CountInt * 16;
                var elementFilter = new UnsafeHashSet<int>(initCapacity, Allocator.Temp);
                
                var iterator = new IteratorInt2Coords(cellBoundsIncExc, out var iteratorValue);
                while (iterator.MoveNext(ref iteratorValue))
                {
                    ref var cell = ref GetCellRef(iteratorValue);
                    for (var i = 0; i < cell.elements.Length; i++)
                    {
                        var elementIndex = cell.elements.keys[i];
                        if (elementFilter.Add(elementIndex))
                            resultIndices.Add(elementIndex);
                    }
                }
                
                elementFilter.Dispose();
            }

            void AddInternal(in T value, int valueIndex, in int2x2 bounds)
            {
                // Inject into bins
                var iterator = new IteratorInt2Coords(bounds, out var iteratorValue);
                while (iterator.MoveNext(ref iteratorValue))
                {
                    ref var cell = ref GetCellRef(iteratorValue);
                    cell.elements.Add(valueIndex, out _);
                }
            }
            
            void MoveInternal(in T value, int valueIndex, in int2x2 oldBounds, in int2x2 newBounds)
            {
                // Yoink the indices out of the cells the old bounds cover
                var oldIterator = new IteratorInt2Coords(oldBounds, out var oldIteratorValue);
                while (oldIterator.MoveNext(ref oldIteratorValue))
                {
                    ref var cell = ref GetCellRef(oldIteratorValue);
                    cell.elements.RemoveSwapBack(valueIndex, out _);
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
                    cell.elements.RemoveSwapBack(removalIndex, out _);
                }
                
                // Remove the OTHER element's OLD index
                var otherElementBoundsIncExc = elementIndexToCellBounds[otherElementOLDIndex];
                elementIndexToCellBounds.Remove(otherElementOLDIndex);
                var otherIterator = new IteratorInt2Coords(otherElementBoundsIncExc, out var otherIteratorValue);
                while (otherIterator.MoveNext(ref otherIteratorValue))
                {
                    ref var cell = ref GetCellRef(otherIteratorValue);
                    cell.elements.RemoveSwapBack(otherElementOLDIndex, out _);
                }

                // Insert the OTHER element's NEW index (which is the removal index)
                otherIteratorValue = otherIterator.GetStartingIteratorValue();
                while (otherIterator.MoveNext(ref otherIteratorValue))
                {
                    ref var cell = ref GetCellRef(otherIteratorValue);
                    cell.elements.Add(removalIndex, out _);
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
        }
        
        public bool AddUpdate(in T value) => nativeMemory.ValueRef.AddUpdate(value);
        public bool Contains(in T value) => nativeMemory.ValueRef.Contains(value);
        public void Remove(in T value) => nativeMemory.ValueRef.Remove(value);

        static int2x2 ComputeCellBoundsIncExc(float cellSize, float2 position, float radius)
        {
            var min = (int2)math.floor((position - radius) / cellSize);
            var max = (int2)math.ceil((position + radius) / cellSize);
            return new int2x2(min, max);
        }

        static int2x2 ComputeCellBoundsIncExc(float cellSize, RectNative rectNative)
        {
            var min = (int2)math.floor((rectNative.Min) / cellSize);
            var max = (int2)math.ceil((rectNative.Max) / cellSize);
            return new int2x2(min, max);
        }
    }
}