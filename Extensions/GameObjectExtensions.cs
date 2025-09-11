using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace VLib
{
    public static class GameObjectExtensions
    {
        public static T GetOrAddComponent<T>(this GameObject obj) where T : Component => obj.GetOrAddComponent<T>(out _);

        public static T GetOrAddComponent<T>(this GameObject c, out bool added)
            where T : Component
        {
            if (c.GetComponent<T>() is var retrievedComponent && retrievedComponent)
            {
                added = false;
                return retrievedComponent;
            }

            added = true;
            return c.AddComponent<T>();
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
        
        #region Prefab

        public static bool TrySavePrefab(this GameObject prefabObject)
        {
#if UNITY_EDITOR
            /*// Is this object part of a prefab at all? EDIT: THIS RETURNS FALSE FOR PREFABS EDITED IN A 'STAGE' (when you double click on a prefab... BIG LOGIC!)
            if (!PrefabUtility.IsPartOfAnyPrefab(prefabObject))
                return false;*/
            
            // Check if we're editing in Prefab Stage
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage && prefabObject == prefabStage.prefabContentsRoot)
            {
                string assetPath = prefabStage.assetPath;
                if (!string.IsNullOrEmpty(assetPath))
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabObject, assetPath, out var savedSuccessfully);
                    return savedSuccessfully;
                }
                return false;
            }

            // Check if it's a prefab asset (not an instance)
            if (PrefabUtility.IsPartOfPrefabAsset(prefabObject))
            {
                // SavePrefabAsset returns the root GameObject if successful, null otherwise
                PrefabUtility.SavePrefabAsset(prefabObject, out var savedSuccessfully);
                return savedSuccessfully;
            }

            // Otherwise, it's a prefab instance in the scene
            if (PrefabUtility.IsPartOfPrefabThatCanBeAppliedTo(prefabObject))
            {
                try
                {
                    PrefabUtility.ApplyPrefabInstance(prefabObject, InteractionMode.AutomatedAction);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to save prefab instance: {e.Message}");
                    return false;
                }
            }
            return true;
#else
            return false;
#endif
        }
        
        #endregion
    }
}