using UnityEngine;
using UnityEngine.Rendering;

namespace VLib
{
    public class CommandRenderRenderer : ICmdRenderable
    {
        Renderer renderer;
        Material material;

        public Renderer Renderer { get => renderer; }
        public Material Material { get => material; set => material = value; }

        public CommandRenderRenderer() { }

        public CommandRenderRenderer(Renderer renderer, Material material)
        {
            this.renderer = renderer;
            this.material = material;
        }

        public virtual void RenderFrom(CommandBuffer buffer)
        {
            buffer.DrawRenderer(renderer, material);
        }
    }
}