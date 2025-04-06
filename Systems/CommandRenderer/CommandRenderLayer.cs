using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace VLib
{
    /// <summary>
    /// Create with constructor, call rebuild buffer after init or making any changes
    /// </summary>
    public class CommandRenderLayer : IDisposable
    {
        CommandBuffer buffer;
        Camera cam;
        CameraEvent camEvent;

        public CommandBuffer Buffer => buffer;
        public Camera Cam => cam;
        public CameraEvent CamEvent => camEvent;
        public string Name { get => buffer.name; set => buffer.name = value; }

        public HashList<ICmdRenderable> renderables;
        public HashList<ICmdRefreshable> refreshables;

        public event Action<CommandBuffer> OnPreBufferRebuild;
        public event Action<CommandBuffer> OnPostBufferRebuild;

        public CommandRenderLayer(Camera cam, CameraEvent cameraEvent, string name = "")
        {
            this.buffer = new CommandBuffer();
            buffer.name = name;
            this.cam = cam;
            this.camEvent = cameraEvent;

            renderables = new HashList<ICmdRenderable>();
            refreshables = new HashList<ICmdRefreshable>();
        }

        public void Dispose()
        {
            if (buffer != null)
            {
                if (cam != null)
                    cam.RemoveCommandBuffer(camEvent, buffer);
                buffer.Dispose();
            }
            buffer = null;

            for (int i = 0; i < renderables.Count; i++)
            {
                (renderables[i] as IDisposable)?.Dispose();
            }

            for (int i = 0; i < refreshables.Count; i++)
            {
                (refreshables[i] as IDisposable)?.Dispose();
            }
        }

        /// <summary> Updates the internal CommandBuffer. Call this after making any ICmdRenderable additions or removals. </summary>
        public void RebuildBuffer(bool additive, bool attachedToCamera = true, bool forceAllRefreshables = false)
        {
            if (!additive)
                buffer.Clear();
            if (attachedToCamera && cam != null)
                cam.RemoveCommandBuffer(camEvent, buffer);

            for (int i = 0; i < refreshables.Count; i++)
            {
                if (forceAllRefreshables || refreshables[i].NeedsRefresh)
                    refreshables[i].RefreshBuffers();
            }

            OnPreBufferRebuild?.Invoke(buffer);

            for (int i = 0; i < renderables.Count; i++)
            {
                renderables[i].RenderFrom(buffer);
            }

            OnPostBufferRebuild?.Invoke(buffer);

            if (attachedToCamera && cam != null)
                cam.AddCommandBuffer(camEvent, buffer);
        }

        /// <summary> Doesn't update until 'RebuildBuffer' is called. </summary>
        public void AddRenderable<T>(T renderable)
            where T : ICmdRenderable
        {
            renderables.Add(renderable as ICmdRenderable);
            if (renderable is ICmdRefreshable)
                refreshables.Add(renderable as ICmdRefreshable);
        }

        /// <summary> Doesn't update until 'RebuildBuffer' is called. </summary>
        public void RemoveRenderable<T>(T renderable)
            where T : ICmdRenderable
        {
            renderables.Remove(renderable as ICmdRenderable);
            if (renderable is ICmdRefreshable)
                refreshables.Remove(renderable as ICmdRefreshable);
        }

        public void Execute()
        {
            if (buffer != null)
                Graphics.ExecuteCommandBuffer(buffer);
        }
    }
}