using System;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace VLib.Tests
{
    public class VArrayPoolTests
    {
        VArrayPool<int> pool;

        [SetUp]
        public void SetUp()
        {
            VArrayPool.DisposeAllArrayPools();
            pool = VArrayPool<int>.Shared;
        }

        [TearDown]
        public void TearDown()
        {
            VArrayPool.DisposeAllArrayPools();
        }

        [Test]
        public void Rent_BucketsByPowerOfTwoAndLinearStep()
        {
            var array4096 = pool.Rent(4096);
            var array4097 = pool.Rent(4097);
            var array8193 = pool.Rent(8193);
            var array12001 = pool.Rent(12001);
            var array127999 = pool.Rent(127999);

            try
            {
                Assert.AreEqual(4096, array4096.Length);
                Assert.AreEqual(8192, array4097.Length);
                Assert.AreEqual(12000, array8193.Length);
                Assert.AreEqual(16000, array12001.Length);
                Assert.AreEqual(128000, array127999.Length);
            }
            finally
            {
                pool.Return(array4096);
                pool.Return(array4097);
                pool.Return(array8193);
                pool.Return(array12001);
                pool.Return(array127999);
            }
        }

        [Test]
        public void Rent_AboveMaxPoolSize_ReturnsExactUntrackedArray()
        {
            var first = pool.Rent(128001);
            Assert.AreEqual(128001, first.Length);
            pool.Return(first);

            var second = pool.Rent(128001);
            Assert.AreEqual(128001, second.Length);
            Assert.AreNotSame(first, second);
            pool.Return(second);
        }

        [Test]
        public void Return_UntrackedArray_IsIgnored()
        {
            var external = new int[4096];
            pool.Return(external);

            var rented = pool.Rent(4096);
            try
            {
                Assert.AreNotSame(external, rented);
            }
            finally
            {
                pool.Return(rented);
            }
        }

        [Test]
        public void Return_TrackedArray_IsReusedByBucket()
        {
            var first = pool.Rent(5000);
            Assert.AreEqual(8192, first.Length);

            pool.Return(first);
            var second = pool.Rent(5000);
            try
            {
                Assert.AreSame(first, second);
            }
            finally
            {
                pool.Return(second);
            }
        }

        [Test]
        public void Return_SameArrayTwice_DoesNotDuplicatePoolEntry()
        {
            var first = pool.Rent(5000);
            pool.Return(first);
            pool.Return(first);

            var rentedFirst = pool.Rent(5000);
            var rentedSecond = pool.Rent(5000);

            try
            {
                Assert.AreSame(first, rentedFirst);
                Assert.AreNotSame(first, rentedSecond);
            }
            finally
            {
                pool.Return(rentedFirst);
                pool.Return(rentedSecond);
            }
        }

        [Test]
        public void Shared_ConcurrentAccess_ReturnsSingleInstance()
        {
            const int workerCount = 64;
            var instances = new VArrayPool<int>[workerCount];

            Parallel.For(0, workerCount, i => instances[i] = VArrayPool<int>.Shared);

            var first = instances[0];
            for (int i = 1; i < instances.Length; i++)
                Assert.AreSame(first, instances[i]);
        }

        [Test]
        public void Dispose_DisposedPoolThrowsOnRent_AndSharedRecreates()
        {
            var disposedPool = VArrayPool<int>.Shared;
            disposedPool.Dispose(true);

            Assert.Throws<ObjectDisposedException>(() => disposedPool.Rent(32));

            var recreatedPool = VArrayPool<int>.Shared;
            Assert.AreNotSame(disposedPool, recreatedPool);

            var rented = recreatedPool.Rent(32);
            try
            {
                Assert.AreEqual(32, rented.Length);
            }
            finally
            {
                recreatedPool.Return(rented);
            }
        }

        [Test]
        public void DisposeAllArrayPools_DisposesSharedPoolAndAllowsRecreation()
        {
            var disposedPool = VArrayPool<int>.Shared;

            VArrayPool.DisposeAllArrayPools();

            Assert.Throws<ObjectDisposedException>(() => disposedPool.Rent(16));

            var recreatedPool = VArrayPool<int>.Shared;
            Assert.AreNotSame(disposedPool, recreatedPool);

            var rented = recreatedPool.Rent(16);
            try
            {
                Assert.AreEqual(16, rented.Length);
            }
            finally
            {
                recreatedPool.Return(rented);
            }
        }

        [Test]
        public void Return_OnDisposedPool_IsIgnored()
        {
            var disposedPool = VArrayPool<int>.Shared;
            var rented = disposedPool.Rent(64);

            disposedPool.Dispose(true);

            Assert.DoesNotThrow(() => disposedPool.Return(rented));
        }

        [Test]
        public void ReleaseAllPooled_DoesNotForgetRentedArrays()
        {
            var rented = pool.Rent(2000);

            LogAssert.Expect(LogType.Error, "Releasing all pooled arrays, but rent count is still: 1");
            pool.ReleaseAllPooled();
            pool.Return(rented);

            var rerented = pool.Rent(2000);
            try
            {
                Assert.AreSame(rented, rerented);
            }
            finally
            {
                pool.Return(rerented);
            }
        }

        [Test]
        public void ReleaseAllPooled_DropsCurrentlyPooledArrays()
        {
            var rented = pool.Rent(2000);
            pool.Return(rented);

            pool.ReleaseAllPooled();

            var rerented = pool.Rent(2000);
            try
            {
                Assert.AreNotSame(rented, rerented);
            }
            finally
            {
                pool.Return(rerented);
            }
        }
    }
}
