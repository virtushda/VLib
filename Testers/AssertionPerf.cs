// Burst asserts are about 10% faster at 10mil iterations

/*using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;

namespace VLib.Testers
{
    public class AssertionPerf : MonoBehaviour
    {
        [SerializeField] int assertions = 1000000;
        
        [Button]
        void TestAssertions()
        {
            var watch = ValueStopwatch.StartNew();
            for (int i = 0; i < assertions; i++)
                Assert.IsTrue(true);
            Debug.Log($"Assert.IsTrue: {watch.ElapsedMilliseconds}ms");
            
            watch = ValueStopwatch.StartNew();
            for (int i = 0; i < assertions; i++)
                BurstAssert.True(true);
            Debug.Log($"BurstAssert.True: {watch.ElapsedMilliseconds}ms");
        }
    }
}*/