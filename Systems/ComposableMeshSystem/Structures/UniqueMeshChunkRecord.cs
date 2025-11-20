using UnityEngine;

namespace Rendering.WaterV2
{
    /// <summary> Keeps track of an allocated mesh chunk and its associated data. </summary>
    public readonly struct UniqueMeshChunkRecord
    {
        public readonly UniqueMeshChunkID id;
        /// <summary> If null, the mesh was constructed programatically. </summary>
        public readonly Mesh mesh;
        
        public UniqueMeshChunkRecord(UniqueMeshChunkID id, Mesh mesh)
        {
            this.id = id;
            this.mesh = mesh;
        }
    }
}