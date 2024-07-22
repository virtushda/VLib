using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VLib
{
    /// <summary> Just assign this to any LOD Group 
    [RequireComponent(typeof(LODGroup))]
    public class LODGroupEventMaster : MonoBehaviour
    {
        [SerializeField] bool analyzeOnAwake = true;

        LODGroup lodGroup;

        //List<int> lodIndices = new List<int>();
        [SerializeField, ReadOnly] List<Renderer[]> lodRendererArrays = new List<Renderer[]>();
        [SerializeField, ReadOnly] List<LODGroupEventNode> lodEventNodes = new List<LODGroupEventNode>();
        [SerializeField, ReadOnly] Dictionary<Renderer, int> rendererNodeIndexMap = new Dictionary<Renderer, int>();

        public LODGroup LODGroup { get => lodGroup; }
        public bool HasSkinnedRenderers
        {
            get
            {
                for (int i = 0; i < lodRendererArrays.Count; i++)
                {
                    for (int o = 0; o < lodRendererArrays[i].Length; o++)
                    {
                        if (lodRendererArrays[i][o] is SkinnedMeshRenderer)
                            return true;
                    }
                }
                return false;
            }
        }

        public event Action<int, Renderer> OnLODChange;
        public event Action<int, Renderer[]> OnActiveRenderGroupChange;

        // Use this for initialization
        void Awake()
        {
            lodGroup = GetComponent<LODGroup>();

            if (analyzeOnAwake)
                AnalyzeLodsSetupNodes();

            HookEvents();
        }

        [Button]
        public void BakeLodHandling()
        {
            lodGroup = GetComponent<LODGroup>();

            AnalyzeLodsSetupNodes();

            analyzeOnAwake = false;
        }

        void AnalyzeLodsSetupNodes()
        {
            lodRendererArrays.Clear();
            lodEventNodes.Clear();
            rendererNodeIndexMap.Clear();

            var lods = lodGroup.GetLODs();
            for (int i = 0; i < lods.Length; i++)
            {
                LOD lod = lods[i];

                //lodIndices.Add(i);

                if (lod.renderers == null || lod.renderers.Length == 0)
                {
                    lodRendererArrays.Add(null);
                    lodEventNodes.Add(null);
                    continue;
                }

                lodRendererArrays.Add(lod.renderers);
                LODGroupEventNode node = lod.renderers[0].GetOrAddComponent<LODGroupEventNode>();
                lodEventNodes.Add(node);

                rendererNodeIndexMap.Add(lod.renderers[0], i);
            }
        }

        void HookEvents()
        {
            foreach (var node in lodEventNodes)
            {
                if (node == null)
                    continue;
                
                node.OnVisible += HandleOnLODChange;
                node.OnVisible += HandleOnActiveRenderGroupChange;
            }
        }

        void HandleOnLODChange(Renderer r)
        {
            OnLODChange?.Invoke(rendererNodeIndexMap[r], r);
        }

        void HandleOnActiveRenderGroupChange(Renderer r)
        {
            int index = 0;
            if(rendererNodeIndexMap.TryGetValue(r, out index))
                OnActiveRenderGroupChange?.Invoke(index, lodRendererArrays[index]);
        }

        public bool TryFindIndexOfNode(LODGroupEventNode node, out int index)
        {
            index = lodEventNodes.FindIndex((n) => n == node);
            if (index < 0)
                return false;
            else
                return true;
        }

        public bool TryGetNodeFromRenderer(Renderer r, out Renderer[] renderers)
        {
            try
            {
                renderers = lodRendererArrays[rendererNodeIndexMap[r]];
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                renderers = null;
                return false;
            }
        }

        public bool TryGetRendererArrayFromNode(LODGroupEventNode node, out Renderer[] renderersArray)
        {
            int lodIndex = lodEventNodes.FindIndex((n) => n == node);

            if (lodIndex >= 0)
            {
                renderersArray = lodRendererArrays[lodIndex];
                return true;
            }
            else
            {
                renderersArray = null;
                return false;
            }
        }

        public bool TryGetCurrentRenderers(out Renderer[] renderers, Camera cam)
        {
            int index = lodGroup.GetVisibleLOD(cam);
            if (lodRendererArrays != null && lodRendererArrays.Count > index)
            {
                renderers = lodRendererArrays[index];
                return true;
            }
            renderers = null;
            return false;
        }
    }
}