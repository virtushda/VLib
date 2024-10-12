using System.Collections.Generic;
using UnityEngine.Profiling;

namespace VLib
{
    public class CommandRenderer
    {
        List<CommandRenderLayer> renderLayers = new List<CommandRenderLayer>();

        public void RebuildAllBuffers(bool additive)
        {
            for (int i = 0; i < renderLayers.Count; i++)
            {
                renderLayers[i].RebuildBuffer(additive);
            }
        }

        public void SubmitRenderLayer(CommandRenderLayer layer)
        {
            if (!renderLayers.Contains(layer))
            {
                renderLayers.Add(layer);
                if (layer.Cam != null)
                    layer.Cam.AddCommandBuffer(layer.CamEvent, layer.Buffer);
            }
        }

        public void RemoveRenderLayer(CommandRenderLayer layer)
        {
            if (renderLayers.Contains(layer))
            {
                renderLayers.Remove(layer);
                if (layer.Cam != null)
                    layer.Cam.RemoveCommandBuffer(layer.CamEvent, layer.Buffer);
            }
        }

        public void DisposeClearAll()
        {
            Profiler.BeginSample("CommandRenderer.DisposeClearAll");
            for (int i = 0; i < renderLayers.Count; i++)
            {
                renderLayers[i].Dispose();
            }
            renderLayers.Clear();
            Profiler.EndSample();
        }
    }
}