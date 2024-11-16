/*using Drawing;
using Unity.Mathematics;
using UnityEngine;

namespace VLib.Testers
{
    [ExecuteAlways]
    public class SpiralGridTester : MonoBehaviour
    {
        void Update()
        {
            var posCoord = (int2) math.floor(transform.position.XZ());

            var spiralIndex = VMath.Grid.SpiralCoordToIndex(posCoord);
            var spiralCoord = VMath.Grid.SpiralIndexToCoord(spiralIndex);

            using var draw = DrawingManager.GetBuilder();
            
            var gridPos3D = new float3(posCoord.x, 0, posCoord.y) + new float3(0.5f, 0, 0.5f);
            // Correct
            draw.WireBox(gridPos3D, new float3(1, 0.1f, 1), Color.green);
            
            // Produced
            var prodGridPos = new float3(spiralCoord.x, 0, spiralCoord.y) + new float3(0.5f, 0, 0.5f);
            draw.WireBox(prodGridPos + math.up() * .1f, new float3(1, 0.1f, 1), Color.red);
            draw.Label2D(prodGridPos + math.up() * .5f, $"Spiral Index: {spiralIndex}", Color.red);
        }
    }
}*/