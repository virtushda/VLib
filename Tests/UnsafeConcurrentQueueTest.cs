/*using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using VLib;
using VLib.Libraries.VLib.Unsafe.Collections;
using VLib.Systems;

namespace Libraries.VLib.Tests
{
    public class UnsafeConcurrentQueueTest
    {
        [Test]
        public void UnsafeConcurrentQueueTestSimplePasses()
        {
            const int count = 100000; //
            
            VUnsafeRef<UnsafeConcurrentQueue<int>> concurrentQueue = new(new UnsafeConcurrentQueue<int>(Allocator.TempJob), Allocator.TempJob);
            VUnsafeList<int> numbers = new VUnsafeList<int>(count, Allocator.TempJob);
            UnsafeHashSet<int> numbersSet = new UnsafeHashSet<int>(count, Allocator.TempJob);
            
            VUnsafeList<int> numbersOut = new VUnsafeList<int>(count, Allocator.TempJob);
            
            // Prep set of numbers
            for (int i = 0; i < count; i++)
            {
                numbers.Add(i);
                numbersSet.Add(i);
            }
            
            // Start write job 
            WriteTest writeTest = new WriteTest
            {
                queueHolder = concurrentQueue,
                numbersIn = numbers
            };
            var writeHandle = writeTest.Schedule(count, 16);
            
            // Start read job, yank stuff outta that queue while it's still enqueuing!
            ReadTestJob readTest = new ReadTestJob
            {
                queueHolder = concurrentQueue,
                numbersOut = numbersOut.AsParallelWriter()
            };
            JobHandle readHandle = readTest.Schedule(count, 16, writeHandle);
            
            JobHandle.ScheduleBatchedJobs();
            JobHandle.CombineDependencies(writeHandle, readHandle).Complete();

            var concurrentQueueCount = concurrentQueue.ValueRef.Count;
            if (concurrentQueueCount > 0)
                Assert.Fail($"Queue not empty, has {concurrentQueueCount} elements!");
            
            Assert.IsTrue(numbersOut.Length == count);
            for (int i = 0; i < count; i++)
                Assert.IsTrue(numbersSet.Remove(numbersOut[i]));
            
            // Test count function
            var writeTest2 = new WriteTest
            {
                queueHolder = concurrentQueue,
                numbersIn = numbers
            };
            writeTest2.Schedule(count, 16).Complete();
            
            if (concurrentQueue.ValueRef.Count != concurrentQueue.ValueRef.BruteForceCount())
                Assert.Fail("Brute force count does not match!");
            
            Assert.IsTrue(concurrentQueue.ValueRef.Count == count);
            
            concurrentQueue.ValueRef.Dispose();
            concurrentQueue.Dispose();
            numbers.Dispose();
            numbersSet.Dispose();
            numbersOut.Dispose();

            Assert.Pass();
        }

        //[BurstCompile]
        struct WriteTest : IJobParallelForBatch
        {
            public VUnsafeRef<UnsafeConcurrentQueue<int>> queueHolder;
            public VUnsafeList<int> numbersIn;
            
            public void Execute(int startIndex, int count)
            {
                ref var queue = ref queueHolder.ValueRef;
                for (int i = startIndex; i < startIndex + count; i++)
                    queue.Enqueue(numbersIn[i]);
            }
        }

        //[BurstCompile]
        struct ReadTestJob : IJobParallelForBatch
        {
            public VUnsafeRef<UnsafeConcurrentQueue<int>> queueHolder;
            public VUnsafeList<int>.ParallelWriter numbersOut;
            
            public void Execute(int index, int count)
            {
                ref var queue = ref queueHolder.ValueRef;

                for (int i = index; i < index + count; i++)
                {
                    var time = VTime.time;
                    var result = -12345;
                    while (!queue.TryDequeue(out result))
                    {
                        if (VTime.time - time > 1)
                        {
                            Debug.LogError("Failed to dequeue in time!");
                            return;
                        }
                    }
                    if (result != -12345)
                        numbersOut.AddNoResize(result);
                }
            }
        }
    }
}*/