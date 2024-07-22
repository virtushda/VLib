using System.Collections.Generic;
using UnityEngine;

namespace VLib
{
    public static class GameObjectExtensions
    {
        public static T GetOrAddComponent<T>(this GameObject obj) where T : Component
        {
            var comp = obj.GetComponent<T>();
            if (comp == null)
                comp = obj.AddComponent<T>();
            return comp;
        }

        public static T GetComponentInParentQueue<T>(this GameObject obj) where T : Component
        {
            Transform currentT = obj.transform;
            while (currentT.parent != null)
            {
                if (currentT.parent.GetComponent<T>() != null)
                   return currentT.parent.GetComponent<T>();
                currentT = currentT.parent;
            }
            return null;
        }

        public static (bool mesh, bool skinned) AnalyzeAndExtractRenderers(this GameObject obj, out MeshRenderer[] meshRenderers, out MeshFilter[] filters, out SkinnedMeshRenderer[] skinnedRenderers)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>();

            bool mesh = false, skinned = false;
            int meshCount = 0, skinnedCount = 0;

            foreach (var r in renderers)
            {
                if (r is MeshRenderer)
                {
                    meshCount++;
                    mesh = true;
                }
                else if (r is SkinnedMeshRenderer)
                {
                    skinnedCount++;
                    skinned = true;
                }
            }

            if (meshCount > 0)
            {
                meshRenderers = new MeshRenderer[meshCount];
                filters = new MeshFilter[meshCount];
            }
            else
            {
                meshRenderers = null;
                filters = null;
            }
            if (skinnedCount > 0)
                skinnedRenderers = new SkinnedMeshRenderer[skinnedCount];
            else
                skinnedRenderers = null;

            int meshIndex = 0, skinnedIndex = 0;

            foreach (var r in renderers)
            {
                if (meshCount > 0 && r is MeshRenderer)
                {
                    MeshRenderer meshRenderer = (r as MeshRenderer);
                    meshRenderers[meshIndex] = meshRenderer;
                    filters[meshIndex++] = meshRenderer.GetComponent<MeshFilter>();
                }
                else if (skinnedCount > 0 && r is SkinnedMeshRenderer)
                    skinnedRenderers[skinnedIndex++] = (r as SkinnedMeshRenderer);
            }

            return (mesh, skinned);
        }

        /// <summary> Move into selectable object component at some point... </summary>
        public static (bool mesh, bool skinned) AnalyzeAndExtractRenderers(this GameObject obj, out List<MeshRenderer> meshRenderers, out List<SkinnedMeshRenderer> skinnedRenderers)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>();

            bool mesh = false, skinned = false;
            meshRenderers = new List<MeshRenderer>();
            skinnedRenderers = new List<SkinnedMeshRenderer>();

            foreach (var r in renderers)
            {
                if (r is MeshRenderer)
                {
                    meshRenderers.Add(r as MeshRenderer);
                    mesh = true;
                }
                else if (r is SkinnedMeshRenderer)
                {
                    skinnedRenderers.Add(r as SkinnedMeshRenderer);
                    skinned = true;
                }
            }

            return (mesh, skinned);
        }

        public static bool IsValidSceneObj(this GameObject obj) => obj && obj.scene.IsValid() && obj.scene.isLoaded;
    }
}