using UnityEngine;
using UnityEngine.Rendering;

namespace VLib
{
    public class CommandRenderMesh : ICmdRenderable
    {
        Mesh mesh;
        Material material;
        Matrix4x4 matrix;
        MaterialPropertyBlock propertyBlock;

        public Material Material { get => material; set => material = value; }
        public Matrix4x4 Matrix { get => matrix; set => matrix = value; }
        public MaterialPropertyBlock PropertyBlock { get => propertyBlock; set => propertyBlock = value; }

        public CommandRenderMesh(Mesh mesh, Material material)
        {
            this.mesh = mesh;
            this.material = material;
            matrix = Matrix4x4.identity;
        }

        public CommandRenderMesh(Mesh mesh, Material material, Matrix4x4 matrix)
        {
            this.mesh = mesh;
            this.material = material;
            this.matrix = matrix;
        }

        public void RenderFrom(CommandBuffer buffer)
        {
            buffer.DrawMesh(mesh, matrix, material, 0, -1, propertyBlock);
        }
    }
}