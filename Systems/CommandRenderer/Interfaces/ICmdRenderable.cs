using UnityEngine;
using UnityEngine.Rendering;

namespace VLib
{
    public interface ICmdRenderable
    {
        Material Material { get; set; }

        void RenderFrom(CommandBuffer buffer);
    }
}