using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace VLib
{
    public class SimplePrefabPool : SimplePoolParameterless<GameObject> // Not parameterless, but this is simpler
    {
        public GameObject prefab;
        
        public SimplePrefabPool(GameObject prefab)
        {
            this.prefab = prefab;
        }

        public SimplePrefabPool(GameObject prefab, int initPoolCapacity) : base(initPoolCapacity)
        {
            this.prefab = prefab;
        }

        public override GameObject CreateNewItem() => GameObject.Instantiate(prefab); 

        public override void Repool(GameObject prefabInstanceToPool)
        {
            prefabInstanceToPool.SetActive(false);
            AddCollectionItem(prefabInstanceToPool);
        }
    }
}