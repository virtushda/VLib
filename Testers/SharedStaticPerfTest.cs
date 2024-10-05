/*using Sirenix.OdinInspector;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace VLib.Testers
{
    public class SharedStaticPerfTest : MonoBehaviour
    {
        readonly static SharedStatic<Internal> sharedInt = SharedStatic<Internal>.GetOrCreate<SharedStaticPerfTest, Internal>();

        struct Internal
        {
            public float4x2 thingy;
        }

        [Button]
        void TestPerf(int iterations = 1000000)
        {
            Test(iterations, true);
            Test(iterations, false);
        }

        unsafe void Test(int iterations, bool warmup)
        {
            float4x2 plainValue = new float4x2(1, 2, 3, 4, 5, 6, 7, 8);
            using VUnsafeRef<float4x2> refValue = new VUnsafeRef<float4x2>(Allocator.Temp);
            refValue.ValueRef = plainValue;

            float4x2 writeTarget = new float4x2(1, 2, 3, 4, 5, 6, 7, 8);
            
            // PLAIN - READ
            var watch = ValueStopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
                writeTarget = plainValue;
            if (!warmup)
                Debug.Log($"PLAIN READ {watch.ElapsedMilliseconds}ms");
            
            // PLAIN - WRITE
            watch = ValueStopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
                plainValue = writeTarget;
            if (!warmup)
                Debug.Log($"PLAIN WRITE {watch.ElapsedMilliseconds}ms");
            
            refValue.ValueRef = plainValue;
            
            // REF - READ
            watch = ValueStopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
                writeTarget = refValue.ValueRef;
            if (!warmup)
                Debug.Log($"REF READ {watch.ElapsedMilliseconds}ms");
            
            // REF - WRITE
            watch = ValueStopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
                refValue.ValueRef = writeTarget;
            if (!warmup)
                Debug.Log($"REF WRITE {watch.ElapsedMilliseconds}ms");
            
            // SHARED STATIC - READ
            watch = ValueStopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
                writeTarget = sharedInt.Data.thingy;
            if (!warmup)
                Debug.Log($"SHARED STATIC READ {watch.ElapsedMilliseconds}ms");
            
            // SHARED STATIC - WRITE
            watch = ValueStopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
                sharedInt.Data.thingy = writeTarget;
            if (!warmup)
                Debug.Log($"SHARED STATIC WRITE {watch.ElapsedMilliseconds}ms");
            
            // SHARED STATIC UNSAFE - READ
            var sharedUnsafe = (Internal*)sharedInt.UnsafeDataPointer;
            watch = ValueStopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
                writeTarget = sharedUnsafe->thingy;
            if (!warmup)
                Debug.Log($"SHARED STATIC UNSAFE READ {watch.ElapsedMilliseconds}ms");
            
            // SHARED STATIC UNSAFE - WRITE
            watch = ValueStopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
                sharedUnsafe->thingy = writeTarget;
            if (!warmup)
                Debug.Log($"SHARED STATIC UNSAFE WRITE {watch.ElapsedMilliseconds}ms");
        }
    }
}*/