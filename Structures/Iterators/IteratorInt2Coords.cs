using Unity.Mathematics;

namespace VLib.Iterators
{
    /// <summary> Iterate over a 2D grid of coordinates represented by a min (inclusive) and max (exclusive). </summary>
    public struct IteratorInt2Coords
    {
        readonly int2x2 minMax;
        int2 current;
        
        public int2 Current => current;

        /// <summary> Pass the outed iterator value back in to the move next function in a loop to iterate. </summary>
        public IteratorInt2Coords(in int2x2 minMax) : this()
        {
            this.minMax = minMax;
            Reset();
        }

        public bool MoveNext(out int2 currentCoords)
        {
            // Move X
            if (++current.x < minMax.c1.x)
            {
                currentCoords = current;
                return true;
            }
            // Reset X
            current.x = minMax.c0.x;
            
            // Move Y
            if (++current.y < minMax.c1.y)
            {
                currentCoords = current;
                return true;
            }
            
            // End
            currentCoords = default;
            return false;
        }

        public void Reset()
        {
            var iteratorValue = minMax.c0;
            --iteratorValue.x;
            current = iteratorValue;
        }
    }
}