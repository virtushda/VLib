using UnityEngine;

namespace VLib.Systems
{
    /// <summary> This is a bandaid, this hook SHOULD be implemented manually. </summary>
    [DefaultExecutionOrder(-32000)]
    public class VTimeUpdater : MonoBehaviour
    {
        bool hasRun = false;

        void Update()
        {
            if (!hasRun)
            {
                Debug.LogError("Do not use this long-term!");
                hasRun = true;
            }
            VTime.OnEarlyUpdate();
        }
    }
}