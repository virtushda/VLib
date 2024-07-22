using UnityEngine;

namespace VLib
{
    public static class RenderTextureExt
    {
        public static Texture2D ToTexture2D(this RenderTexture rt, bool uploadToGPU, TextureFormat format = TextureFormat.RGB24)
        {
            if (rt == null)
            {
                Debug.LogError("RenderTexture is null! (returning null texture)");
                return null;
            }

            RenderTexture oldRT = RenderTexture.active;
            RenderTexture.active = rt;
            
            Texture2D tex = new Texture2D(rt.width, rt.height, format, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            
            RenderTexture.active = oldRT;
            
            if (uploadToGPU)
                tex.Apply(true);
            
            return tex;
        }
    }
}