using Unity.Mathematics;

namespace VLib.Libraries.VLib.Structures
{
    public struct VGrid
    {
        /// <summary> Cell-space (local) grid bounds. </summary>
        public readonly RectNative localGridRect;
        /// <summary> Where is the bottom-left corner of this grid. </summary>
        public readonly float2 origin;
        /// <summary> The size of each cell. </summary>
        public readonly float2 cellSize;

        /// <summary> A constructor for a perfectly square grid. </summary>
        public VGrid(int cellCount, float cellSize, float2 origin = default)
        {
            localGridRect = RectNative.FromMinMax(origin, origin + cellCount);
            this.cellSize = cellSize;
            this.origin = origin;
        }
        
        
    }
}