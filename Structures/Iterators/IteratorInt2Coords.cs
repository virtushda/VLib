using Unity.Mathematics;

namespace VLib.Iterators
{
    /// <summary> Iterate over a 2D grid of coordinates represented by a min and max. </summary>
    public struct IteratorInt2Coords
    {
        readonly int2x2 minMax;

        /// <summary> Pass the outed iterator value back in to the move next function in a loop to iterate. </summary>
        public IteratorInt2Coords(in int2x2 minMax, out int2 iteratorValue)
        {
            this.minMax = minMax;
            iteratorValue = GetStartingIteratorValue();
        }

        public bool MoveNext(ref int2 iteratorValue)
        {
            if (++iteratorValue.x <= minMax.c1.x)
                return true;
            if (++iteratorValue.y <= minMax.c1.y)
            {
                iteratorValue.x = minMax.c0.x;
                return true;
            }
            return false;
        }

        public int2 GetStartingIteratorValue()
        {
            var iteratorValue = minMax.c0;
            --iteratorValue.x;
            return iteratorValue;
        }
    }
}