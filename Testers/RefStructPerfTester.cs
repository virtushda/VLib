/*using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using VLib;

namespace RefStructFeature.PerformanceTesting
{
    public class RefStructPerfTesting : MonoBehaviour
    {
        [Header("Create-Dispose")]
        [SerializeField] int createDisposeIterations = 10000;
        
        [Header("Read-Write")]
        [SerializeField] int iterations = 100000;
        [SerializeField] int bufferSize = 128;
        
        UnsafeList<VUnsafeRef<float4>> testBufferUnsafeRefs;
        UnsafeList<RefStruct<float4>> testBufferRefStructs;

        [Button, DisableInEditorMode]
        void RunTestVUnsafeRef()
        {
            Profiler.BeginSample("VUnsafeRef-TESTING");

            ValueStopwatch watch = ValueStopwatch.StartNew();
            
            // CREATION
            var creationTest = new UnsafeList<VUnsafeRef<float>>(createDisposeIterations, Allocator.Temp);
            for (int i = 0; i < createDisposeIterations; i++)
                creationTest.AddNoResize(new VUnsafeRef<float>(Allocator.Temp));
            
            Debug.Log($"VUnsafeRef Creation: {watch.Elapsed.TotalMilliseconds}ms");
            watch = ValueStopwatch.StartNew();
            
            // DESTRUCTION
            for (int i = 0; i < createDisposeIterations; i++)
                creationTest[i].Dispose();
            
            Debug.Log($"VUnsafeRef Destruction: {watch.Elapsed.TotalMilliseconds}ms");
            creationTest.Dispose();
            
            // PREP
            testBufferUnsafeRefs = new(bufferSize, Allocator.Persistent);
            for (int i = 0; i < bufferSize; i++)
                testBufferUnsafeRefs.Add(new VUnsafeRef<float4>(float4.zero, Allocator.Persistent));
            
            watch = ValueStopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                for (int j = 0; j < testBufferUnsafeRefs.Length; j++)
                {
                    var value = testBufferUnsafeRefs[j];
                    // Read write
                    var val = value.Value;
                    value.Value = val;
                }
            }
            
            Debug.Log($"VUnsafeRef: {watch.Elapsed.TotalMilliseconds}ms");
            
            // Dispose vrefs
            for (int i = 0; i < testBufferUnsafeRefs.Length; i++)
            {
                testBufferUnsafeRefs[i].Dispose();
            }
            testBufferUnsafeRefs.Dispose();
            Profiler.EndSample();
        }

        /*[Button, DisableInEditorMode]
        void RunTestVUnsafeKeyedRef()
        {
            Profiler.BeginSample("VUnsafeKeyedRef-TESTING");

            ValueStopwatch watch = ValueStopwatch.StartNew();
            
            // CREATION
            var creationTest = new UnsafeList<VUnsafeRef<float>>(createDisposeIterations, Allocator.Temp);
            for (int i = 0; i < createDisposeIterations; i++)
                creationTest.AddNoResize(new VUnsafeRef<float>(Allocator.Temp));
            
            Debug.Log($"VUnsafeRef Creation: {watch.Elapsed.TotalMilliseconds}ms");
            watch = ValueStopwatch.StartNew();
            
            // DESTRUCTION
            for (int i = 0; i < createDisposeIterations; i++)
                creationTest[i].Dispose();
            
            Debug.Log($"VUnsafeRef Destruction: {watch.Elapsed.TotalMilliseconds}ms");
            creationTest.Dispose();
            
            // PREP
            testBufferUnsafeRefs = new(bufferSize, Allocator.Persistent);
            for (int i = 0; i < bufferSize; i++)
                testBufferUnsafeRefs.Add(new VUnsafeRef<float4>(float4.zero, Allocator.Persistent));
            
            watch = ValueStopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                for (int j = 0; j < testBufferUnsafeRefs.Length; j++)
                {
                    var value = testBufferUnsafeRefs[j];
                    // Read write
                    var val = value.Value;
                    value.Value = val;
                }
            }
            
            Debug.Log($"VUnsafeRef: {watch.Elapsed.TotalMilliseconds}ms");
            
            // Dispose vrefs
            for (int i = 0; i < testBufferUnsafeRefs.Length; i++)
            {
                testBufferUnsafeRefs[i].Dispose();
            }
            testBufferUnsafeRefs.Dispose();
            Profiler.EndSample();
        }#1#

        [Button, DisableInEditorMode]
        void RunTestRefStruct()
        {
            Profiler.BeginSample("RefStruct-TESTING");

            ValueStopwatch watch = ValueStopwatch.StartNew();
            
            // CREATION
            var creationTest = new UnsafeList<RefStruct<float>>(createDisposeIterations, Allocator.Temp);
            for (int i = 0; i < createDisposeIterations; i++)
                creationTest.AddNoResize(RefStruct<float>.Create(default, Allocator.Temp));
            
            Debug.Log($"RefStruct Creation: {watch.Elapsed.TotalMilliseconds}ms");
            watch = ValueStopwatch.StartNew();
            
            // DESTRUCTION
            for (int i = 0; i < createDisposeIterations; i++)
                creationTest[i].Dispose();
            
            Debug.Log($"RefStruct Destruction: {watch.Elapsed.TotalMilliseconds}ms");
            creationTest.Dispose();
            
            // PREP
            testBufferRefStructs = new(bufferSize, Allocator.Persistent);
            for (int i = 0; i < bufferSize; i++)
            {
                testBufferRefStructs.Add(RefStruct<float4>.Create(float4.zero));
            }

            watch = ValueStopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                for (int j = 0; j < testBufferRefStructs.Length; j++)
                {
                    // Read write
                    var value = testBufferRefStructs[j];
                    // Read write
                    var val = value.ValueCopy;
                    value.ValueCopy = val; //
                }
            }
            
            Debug.Log($"RefStruct: {watch.Elapsed.TotalMilliseconds}ms");
            
            // Dispose vrefs
            for (int i = 0; i < testBufferRefStructs.Length; i++)
                testBufferRefStructs[i].Dispose();
            testBufferRefStructs.Dispose();
            //Profiler.EndSample();
        }
    }
}*/