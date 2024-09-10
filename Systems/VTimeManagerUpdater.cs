using UnityEngine;

namespace VLib.Systems
{
    /// <summary> This is a bandaid, this hook SHOULD be implemented manually. </summary>
    [DefaultExecutionOrder(-32000)]
    public class VTimeManagerUpdater : MonoBehaviour
    {
        void Update() => VTimeManager.OnEarlyUpdate();
    }
}