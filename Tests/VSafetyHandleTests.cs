#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace VLib.Tests
{
    public class VSafetyHandleTests
    {
        [Test]
        public void VSafetyHandleTestsSimplePasses()
        {
            var takenHandlesBeforeTest = VSafetyHandleManager.InternalMemoryField.Data.TakenHandles;
            
            int testCount = 2048;
            
            // Parallel create and destroy test
            Parallel.For((long) 0, testCount, _ =>
            {
                const int handleCount = 64;
                
                // Use a list to ensure the handle struct is being copied, arrays have some weird reference behavior
                List<VSafetyHandle> handles = new(handleCount);
                while (handles.Count < handleCount)
                    handles.Add(default);
                
                for (int j = 0; j < handleCount; j++)
                {
                    if (j.IsEven())
                    {
                        handles[j] = VSafetyHandle.Create();
                        if (!handles[j].IsValid)
                            Assert.Fail($"Just created handle {j} is not valid!");
                    }
                    else
                    {
                        handles[j].Dispose();
                        if (handles[j].IsValid)
                            Assert.Fail($"Just disposed handle {j} is valid!");
                    }
                }
                for (int j = 0; j < handleCount; j++)
                {
                    handles[j].Dispose();
                    if (handles[j].IsValid)
                        Assert.Fail($"Just disposed handle {j} is valid!");
                }
            });

            var takenHandles = VSafetyHandleManager.InternalMemoryField.Data.TakenHandles;
            if (takenHandles != takenHandlesBeforeTest)
                Assert.Fail($"Taken handle count is {takenHandles}!");
            
            // Test that default handles are invalid
            var defaultHandle = default(VSafetyHandle);
            if (defaultHandle.IsValid)
                Assert.Fail("Default handle is valid!");
            
            // Burst
            var errors = new VUnsafeList<VSafetyHandleTestBurstJob.HandleError>(testCount, Allocator.TempJob);
            new VSafetyHandleTestBurstJob(errors).Schedule(testCount, 32).Complete();
            
            while (errors.TryPopLastElement(out var error))
            {
                switch (error)
                {
                    case VSafetyHandleTestBurstJob.HandleError.CreatedHandleNotValid:
                        Assert.Fail("Created handle is not valid!");
                        break;
                    case VSafetyHandleTestBurstJob.HandleError.DisposedHandleIsValid:
                        Assert.Fail("Disposed handle is valid!");
                        break;
                }
            }
            
            errors.Dispose();
            
            Assert.Pass("Passed");
        }

        /*// A UnityTest behaves like a coroutine in PlayMode
        // and allows you to yield null to skip a frame in EditMode
        [UnityEngine.TestTools.UnityTest]
        public System.Collections.IEnumerator VSafetyHandleTestsWithEnumeratorPasses()
        {
            // Use the Assert class to test conditions.
            // yield to skip a frame
            yield return null;
        }*/
    }

    [BurstCompile]
    struct VSafetyHandleTestBurstJob : IJobParallelFor
    {
        public enum HandleError
        {
            None,
            CreatedHandleNotValid,
            DisposedHandleIsValid
        }

        VUnsafeList<HandleError>.ParallelWriter errors;
        
        public VSafetyHandleTestBurstJob( VUnsafeList<HandleError> errors)
        {
            errors.Clear();
            this.errors = errors.AsParallelWriter();
        }
        
        public void Execute(int index)
        {
            const int handleCount = 64;
                
            // Use a list to ensure the handle struct is being copied, arrays have some weird reference behavior
            VUnsafeList<VSafetyHandle> handles = new(handleCount, Allocator.Temp);
            while (handles.Count < handleCount)
                handles.Add(default);
                
            for (int j = 0; j < handleCount; j++)
            {
                if (j.IsEven())
                {
                    handles[j] = VSafetyHandle.Create();
                    if (!handles[j].IsValid)
                    {
                        errors.AddNoResize(HandleError.CreatedHandleNotValid);
                        return;
                    }
                }
                else
                {
                    handles[j].Dispose();
                    if (handles[j].IsValid)
                    {
                        errors.AddNoResize(HandleError.DisposedHandleIsValid);
                        return;
                    }
                }
            }
            for (int j = 0; j < handleCount; j++)
            {
                handles[j].Dispose();
                if (handles[j].IsValid)
                {
                    errors.AddNoResize(HandleError.DisposedHandleIsValid);
                    return;
                }
            }
            handles.Dispose();
        }
    }
}
#endif