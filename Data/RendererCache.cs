using UnityEngine;

namespace VLib
{
    [System.Serializable]
    public class RendererCache
    {
        bool cachePopulated = false;

        bool hasMeshRenderersCached = false;
        bool hasSkinnedRenderersCached = false;
        bool hasLODEventsCached = false;

        GameObject obj;
        public GameObject RootObject { get => obj; }

        MaskableArrayParam<MeshFilter> meshFilters;
        MaskableArrayParam<Renderer> meshRenderers;
        MaskableArrayParam<Renderer> skinnedMeshRenderers;
        LODGroupEventMaster[] lodEventMasters;

        public bool CachePopulated { get => cachePopulated; set => cachePopulated = value; }
        public bool HasMeshSafe
        {
            get
            {
                Populate();
                return hasMeshRenderersCached;
            }
        }
        public bool HasMesh => hasMeshRenderersCached;

        public bool HasSkinnedSafe 
        {
            get
            {
                Populate();
                return hasSkinnedRenderersCached;
            }
        }
        public bool HasSkinned => hasSkinnedRenderersCached;

        public bool HasLodEventsSafe
        {
            get
            {
                Populate();
                return hasLODEventsCached;
            }
        }
        public bool HasLODEvents => hasLODEventsCached;

        public MeshFilter[] MeshFiltersSafe
        {
            get
            {
                Populate();
                return MeshFiltersParam.GetCurrentValue();
            }
        }
        public MeshFilter[] MeshFilters => MeshFiltersParam.GetCurrentValue();

        public Renderer[] MeshRenderersSafe
        {
            get
            {
                Populate();
                return MeshRenderersParam.GetCurrentValue();
            }
        }
        public Renderer[] MeshRenderers => MeshRenderersParam.GetCurrentValue();

        public Renderer[] SkinnedRenderersSafe
        {
            get
            {
                Populate();
                return SkinnedMeshRenderersParam.GetCurrentValue();
            }
        }
        public Renderer[] SkinnedRenderers => SkinnedMeshRenderersParam.GetCurrentValue();

        public LODGroupEventMaster[] LODEventMastersSafe
        {
            get
            {
                Populate();
                return lodEventMasters;
            }
        }
        public LODGroupEventMaster[] LODEventMasters => lodEventMasters;

        public MaskableArrayParam<MeshFilter> MeshFiltersParam
        {
            get
            {
                Populate();
                return meshFilters;
            }
            set => meshFilters = value;
        }
        public MaskableArrayParam<Renderer> MeshRenderersParam
        {
            get
            {
                Populate();
                return meshRenderers;
            }
            set => meshRenderers = value;
        }
        public MaskableArrayParam<Renderer> SkinnedMeshRenderersParam
        {
            get
            {
                Populate();
                return skinnedMeshRenderers;
            }
            set => skinnedMeshRenderers = value;
        }

        public RendererCache(GameObject obj)
        {
            SetObject(obj);
            Populate(true);
        }

        public void Clear()
        {
            hasMeshRenderersCached = false;
            hasSkinnedRenderersCached = false;
            MeshFiltersParam = null;
            MeshRenderersParam = null;
            SkinnedMeshRenderersParam = null;
            lodEventMasters = null;

            cachePopulated = false;
        }

        public void SetObject(GameObject obj)
        {
            this.obj = obj;
            cachePopulated = false;
        }

        public RendererCache Populate(bool forced = false)
        {
            if (obj == null)
            {
                Debug.LogError("RendererCache has no GameObject reference!");
                return this;
            }

            if (!forced && cachePopulated)
                return this;

            var (mesh, skinned) = obj.AnalyzeAndExtractRenderers(out var meshRend, out var filters, out var smrs);

            hasMeshRenderersCached = mesh;
            hasSkinnedRenderersCached = skinned;

            if (mesh)
            {
                meshFilters ??= new MaskableArrayParam<MeshFilter>(null);
                meshRenderers ??= new MaskableArrayParam<Renderer>(null);

                meshFilters.Value = filters;
                meshRenderers.Value = meshRend;
            }
            if (skinned)
            {
                skinnedMeshRenderers ??= new MaskableArrayParam<Renderer>(null);
                skinnedMeshRenderers.Value = smrs;
            }

            lodEventMasters = obj.GetComponentsInChildren<LODGroupEventMaster>();
            hasLODEventsCached = lodEventMasters != null && lodEventMasters.Length > 0;

            cachePopulated = true;

            return this;
        }
    } 
}