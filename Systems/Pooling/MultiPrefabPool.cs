using System.Collections.Generic;
using UnityEngine;

namespace VLib
{
    /// <summary> A pool of prefab pools.
    /// Not thread safe </summary>
    public class MultiPrefabPool
    {
        Dictionary<GameObject, ParameterlessPrefabPool> prefabAssetToPool = new();

        public GameObject Depool(GameObject prefabAsset)
        {
            if (!prefabAssetToPool.TryGetValue(prefabAsset, out var prefabPool))
            {
                prefabPool = new ParameterlessPrefabPool(prefabAsset);
                prefabAssetToPool.Add(prefabAsset, prefabPool);
            }
            return prefabPool.Fetch();
        }

        public void Repool(GameObject prefabAsset, GameObject instance)
        {
            if (!prefabAssetToPool.TryGetValue(prefabAsset, out var prefabPool))
            {
                prefabPool = new ParameterlessPrefabPool(prefabAsset);
                prefabAssetToPool.Add(prefabAsset, prefabPool);
            }
        }
    }
}