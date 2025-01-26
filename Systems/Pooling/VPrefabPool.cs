using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VLib
{
    public class VPrefabPool : VPool<GameObject>
    {
        public GameObject Prefab { get; private set; }

        /// <summary> Automatic constructor that sets up appropriate delegates for handling gameobjects/prefabs. See other constructor for more advanced options. </summary>
        public VPrefabPool(GameObject prefab) : this(prefab, DefaultInitialCapacity) // Call bigger constructor and let it auto-resolve
        { }
        
        /// <inheritdoc />
        public VPrefabPool(
            GameObject prefab, 
            int initPoolCapacity = DefaultInitialCapacity,
            Action<GameObject> depoolPostProcess = null,
            Action<GameObject> returnPreProcess = null,
            Func<GameObject> creationAction = null,
            Action<GameObject> disposalAction = null)
            : base(initPoolCapacity, depoolPostProcess, returnPreProcess, creationAction, disposalAction)
        {
            BurstAssert.True(prefab);
            BurstAssert.True(initPoolCapacity > 0);
            
            // Auto replace null actions
            this.depoolPostProcess ??= obj => obj.SetActive(true);
            this.repoolPreProcess ??= obj => obj.SetActive(false);
            this.createAction ??= () => Object.Instantiate(prefab);
            this.disposeAction ??= Object.Destroy;
            
            Prefab = prefab;
        }
    }
}