using System.Collections;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine.TestTools;
using VLib;

namespace Libraries.VLib.Tests
{
    public class VUnsafeBufferArrayTests
    {
        [Test]
        public void VUnsafeBufferListTestsSimplePasses()
        {
            const int testCount = 1024;
            var bufferArray = new VUnsafeBufferArray<float>(1024, true, Allocator.Persistent);
            
            // Setup some values
            for (int i = 0; i < testCount; i++)
                bufferArray.AddCompact(i);

            // Remove every other value
            for (int i = 0; i < testCount; i += 2)
                bufferArray.RemoveAtClear(i);
            
            for (int i = 0; i < testCount; i++)
            { 
                // Demand correct active state
                if (i % 2 == 0)
                    Assert.False(bufferArray.IndexActive(i));
                else
                    Assert.True(bufferArray.IndexActive(i));
                
                // Scan raw data and demand perfection
                var active = bufferArray.IndexActive(i);
                if (active)
                    Assert.True(bufferArray[i].Equals(i));
                else
                    Assert.True(bufferArray.ElementAtUnsafe(i).Equals(0));
            }

            // Check that the tryget detects this correctly
            int tryGetCount = 0;
            for (int i = 0; i < testCount; i++)
            {
                if (bufferArray.TryGetValue(i, out _))
                    tryGetCount++;
            }
            Assert.True(tryGetCount == testCount / 2);
            
            // Test that adding 64 back fills properly to 128
            for (int i = 0; i < testCount / 2; i++)
                bufferArray.AddCompact(i);
            Assert.True(bufferArray.Length == testCount);
            Assert.True(bufferArray.ActiveCount == testCount);
            
            // Demand main range active to prove indices are well packed.
            for (int i = 0; i < testCount; i++)
                Assert.True(bufferArray.IndexActive(i));
            
            // Take out a safeptr and check that it's valid
            var safePtr = bufferArray.RentIndexPointer(12);
            var safePtr2 = bufferArray.RentIndexPointer(13);

            safePtr.ValueCopy = 12345;
            safePtr2.ValueCopy = 54321;
            
            Assert.True(bufferArray[12].Equals(12345));
            Assert.True(bufferArray[13].Equals(54321));

            bufferArray.RemoveAtClear(12);
            Assert.False(safePtr.IsCreated);
            Assert.True(safePtr2.IsCreated);
            
            bufferArray.Clear();
            Assert.False(safePtr2.IsCreated);
            
            bufferArray.Dispose();
        }
    }
}