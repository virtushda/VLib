using System.Threading;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using VLib.Systems;

namespace VLib.Libraries.VLib.Tests
{
    public class VRWLockPerfTester : MonoBehaviour
    {
        static ReaderWriterLockSlim rwLock;
        static VReaderWriterLockSlim vrwLock;
        //static VStateLock stateLock;

        public bool multiWrite = false;
        
        [Button]
        public void Benchmark(int readIterations = 1000000, int writeIterations = 10000)
        {
            rwLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            vrwLock = new VReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            //stateLock = new VStateLock();
            
            using VUnsafeRef<int> valueHolder = new(0, Allocator.TempJob);
            
            var watch = ValueStopwatch.StartNew();
            BenchmarkLockless(readIterations, writeIterations, valueHolder);
            Debug.Log($"Lockless: {watch.ElapsedMillisecondsF}ms");
            
            watch = ValueStopwatch.StartNew();
            BenchmarkLock(readIterations, writeIterations, valueHolder);
            Debug.Log($"Lock: {watch.ElapsedMillisecondsF}ms");
            
            watch = ValueStopwatch.StartNew();
            BenchmarkLockSlim(readIterations, writeIterations, valueHolder);
            Debug.Log($"LockSlim: {watch.ElapsedMillisecondsF}ms");
            
            watch = ValueStopwatch.StartNew();
            BenchmarkVLockSlimScoped(readIterations, writeIterations, valueHolder);
            Debug.Log($"VLockSlimScoped: {watch.ElapsedMillisecondsF}ms");
            
            /*valueHolder.ValueRef = 0;
            watch = ValueStopwatch.StartNew();
            BenchmarkStateLockScoped(readIterations, writeIterations, valueHolder);
            Debug.Log($"VStateLockScoped: {watch.ElapsedMillisecondsF}ms");
            Debug.Log($"Value: {valueHolder.Value}, {(writeIterations == valueHolder.Value ? "Success" : "Failure")}");*/
            
            // BENCH LOCKS DIRECTLY
            watch = ValueStopwatch.StartNew();
            for (int i = 0; i < readIterations; i++)
            {
                rwLock.EnterReadLock();
                rwLock.ExitReadLock();
            }
            Debug.Log($"LockSlim Alone: {watch.ElapsedMillisecondsF}ms");
            
            watch = ValueStopwatch.StartNew();
            for (int i = 0; i < readIterations; i++)
            {
                rwLock.EnterWriteLock();
                rwLock.ExitWriteLock();
            }
            Debug.Log($"LockSlim WRITE Alone: {watch.ElapsedMillisecondsF}ms");
            
            watch = ValueStopwatch.StartNew();
            for (int i = 0; i < readIterations; i++)
            {
                using var lockHolder = vrwLock.ScopedReadLock();
            }
            Debug.Log($"VLockSlimScoped Alone: {watch.ElapsedMillisecondsF}ms");
            
            /*watch = ValueStopwatch.StartNew();
            for (int i = 0; i < readIterations; i++)
            {
                using var lockHolder = stateLock.ChangeInScope(1);
            }
            Debug.Log($"VStateLock Alone: {watch.ElapsedMillisecondsF}ms");*/
            
            rwLock.Dispose();
            rwLock = null;
            vrwLock.Dispose();
            vrwLock = null;
        }
        
        void BenchmarkLockless(int readIterations, int writeIterations, VUnsafeRef<int> valueHolder)
        {
            var readJob = new LocklessReadJob {valueHolder = valueHolder};
            JobHandle handle = readJob.Schedule(readIterations, 128);
            JobHandle.ScheduleBatchedJobs();
            if (writeIterations > 0)
            {
                var job = new LocklessWriteJob {valueHolder = valueHolder, iterations = writeIterations};
                handle = JobHandle.CombineDependencies(handle, job.Schedule());
            }
            handle.Complete();
        }
        
        void BenchmarkLock(int readIterations, int writeIterations, VUnsafeRef<int> valueHolder)
        {
            var readJob = new LockingReadJob {valueHolder = valueHolder};
            JobHandle handle = readJob.Schedule(readIterations, 128);
            JobHandle.ScheduleBatchedJobs();
            if (writeIterations > 0)
            {
                var job = new LockingWriteJob {valueHolder = valueHolder, iterations = writeIterations};
                handle = JobHandle.CombineDependencies(handle, job.Schedule());
            }
            handle.Complete();
        }
        
        void BenchmarkLockSlim(int readIterations, int writeIterations, VUnsafeRef<int> valueHolder)
        {
            var readJob = new LockSlimReadJob {valueHolder = valueHolder};
            JobHandle handle = readJob.Schedule(readIterations, 128);
            JobHandle.ScheduleBatchedJobs();
            if (writeIterations > 0)
            {
                var job = new LockSlimWriteJob {valueHolder = valueHolder, iterations = writeIterations};
                handle = JobHandle.CombineDependencies(handle, job.Schedule());
            }
            handle.Complete();
        }
        
        void BenchmarkVLockSlimScoped(int readIterations, int writeIterations, VUnsafeRef<int> valueHolder)
        {
            var readJob = new VLockSlimScopedReadJob {valueHolder = valueHolder};
            JobHandle handle = readJob.Schedule(readIterations, 128);
            JobHandle.ScheduleBatchedJobs();
            if (writeIterations > 0)
            {
                var job = new VLockSlimScopedWriteJob {valueHolder = valueHolder, iterations = writeIterations};
                handle = JobHandle.CombineDependencies(handle, job.Schedule());
            }
            handle.Complete();
        }
        
        /*void BenchmarkStateLockScoped(int readIterations, int writeIterations, VUnsafeRef<int> valueHolder)
        {
            var readJob = new VStateLockScopedReadJob {valueHolder = valueHolder};
            JobHandle handle = readJob.Schedule(readIterations, 128);
            JobHandle.ScheduleBatchedJobs();
            if (writeIterations > 0)
            {
                if (multiWrite)
                {
                    var job = new VStateLockScopedWriteJob {valueHolder = valueHolder};
                    handle = JobHandle.CombineDependencies(handle, job.Schedule(writeIterations, 128));
                }
                else
                {
                    var job = new VStateLockScopedWriteSingleThreadJob {valueHolder = valueHolder, iterations = writeIterations};
                    handle = JobHandle.CombineDependencies(handle, job.Schedule());
                }
            }
            handle.Complete();
        }*/

        struct LocklessReadJob : IJobParallelFor
        {
            public VUnsafeRef<int> valueHolder;
            
            public void Execute(int index)
            {
                int value = valueHolder.Value;
            }
        }

        struct LocklessWriteJob : IJob
        {
            public VUnsafeRef<int> valueHolder;
            public int iterations;

            public void Execute()
            {
                for (int i = 0; i < iterations; i++)
                    ++valueHolder.Value;
            }
        }
        
        struct LockingReadJob : IJobParallelFor
        {
            public VUnsafeRef<int> valueHolder;
            
            public void Execute(int index)
            {
                lock(rwLock)
                {
                    int value = valueHolder.Value;
                }
            }
        }

        struct LockingWriteJob : IJob
        {
            public VUnsafeRef<int> valueHolder;
            public int iterations;
            
            public void Execute()
            {
                lock(rwLock)
                    for (int i = 0; i < iterations; i++)
                        ++valueHolder.Value;
            }
        }
        
        struct LockSlimReadJob : IJobParallelFor
        {
            public VUnsafeRef<int> valueHolder;
            
            public void Execute(int index)
            {
                rwLock.EnterReadLock();
                int value = valueHolder.Value;
                rwLock.ExitReadLock();
            }
        }
        
        struct LockSlimWriteJob : IJob
        {
            public VUnsafeRef<int> valueHolder;
            public int iterations;
            
            public void Execute()
            {
                rwLock.EnterWriteLock();
                for (int i = 0; i < iterations; i++)
                    ++valueHolder.Value;
                rwLock.ExitWriteLock();
            }
        }
        
        struct VLockSlimScopedReadJob : IJobParallelFor
        {
            public VUnsafeRef<int> valueHolder;
            
            public void Execute(int index)
            {
                using var lockHolder = vrwLock.ScopedReadLock();
                int value = valueHolder.Value;
            }
        }
        
        struct VLockSlimScopedWriteJob : IJob
        {
            public VUnsafeRef<int> valueHolder;
            public int iterations;
            
            public void Execute()
            {
                using var lockHolder = vrwLock.ScopedExclusiveLock();
                for (int i = 0; i < iterations; i++)
                    ++valueHolder.Value;
            }
        }
        
        /*struct VStateLockScopedReadJob : IJobParallelFor
        {
            public VUnsafeRef<int> valueHolder;
            
            public void Execute(int index)
            {
                using var lockHolder = stateLock.ChangeInScope(0);
                int value = valueHolder.Value;
            }
        }

        struct VStateLockScopedWriteSingleThreadJob : IJob
        {
            public VUnsafeRef<int> valueHolder;
            public int iterations;
            
            public void Execute()
            {
                using var lockHolder = stateLock.ChangeInScope(1);
                for (int i = 0; i < iterations; i++)
                    ++valueHolder.Value;
            }
        }
        
        struct VStateLockScopedWriteJob : IJobParallelFor
        {
            public VUnsafeRef<int> valueHolder;
            [NativeSetThreadIndex] public int threadIndex;
            
            public void Execute(int index)
            {
                using var lockHolder = stateLock.ChangeInScope(threadIndex + 1);
                ++valueHolder.Value;
                //Interlocked.Increment(ref valueHolder.ValueRef);
            }
        }*/
    }
}