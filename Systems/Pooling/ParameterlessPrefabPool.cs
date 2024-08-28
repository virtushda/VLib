using UnityEngine;

namespace VLib
{
    public class ParameterlessPrefabPool : ParameterlessPool<GameObject> // Not parameterless, but this is simpler
    {
        public GameObject prefab;
        
        public ParameterlessPrefabPool(GameObject prefab)
        {
            this.prefab = prefab;
        }

        public ParameterlessPrefabPool(GameObject prefab, int initPoolCapacity) : base(initPoolCapacity)
        {
            this.prefab = prefab;
        }

        public override GameObject CreateNewItem() => Object.Instantiate(prefab);

        /// <summary> Depools and enables a prefab instance </summary>
        public override GameObject Fetch()
        {
            // Base impl already partially overridden to use CreateNewItem, we will get an object no matter what
            var obj = base.Fetch();
            obj.SetActive(true);
            return obj;
        }

        /// <summary> Depools a prefab instance, but does not enable it for you </summary>
        public GameObject FetchInactive() => base.Fetch();

        public override void Repool(GameObject prefabInstanceToPool)
        {
            prefabInstanceToPool.SetActive(false);
            AddCollectionItem(prefabInstanceToPool);
        }
    }
}