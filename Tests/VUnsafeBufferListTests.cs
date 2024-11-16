using System.Collections;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine.TestTools;
using VLib;

namespace Libraries.VLib.Tests
{
    public class VUnsafeBufferListTests
    {
        [Test]
        public void VUnsafeBufferListTestsSimplePasses()
        {
            var list = new VUnsafeBufferList<float>(512, true, Allocator.Persistent);
            
            // Setup some values
            for (int i = 0; i < 128; i++)
                list.AddCompact(i);

            // Remove every other value
            for (int i = 0; i < 128; i++)
            {
                if (i % 2 == 0)
                    list.RemoveAtClear(i);
            }

            // Check that the tryget detects this correctly
            int tryGetCount = 0;
            for (int i = 0; i < 128; i++)
            {
                if (list.TryGetValue(i, out _))
                    tryGetCount++;
            }
            Assert.True(tryGetCount == 64);
            
            // Test that adding 64 back fills properly to 128
            for (int i = 0; i < 64; i++)
                list.AddCompact(i);
            Assert.True(list.Length == 128);
            Assert.True(list.ActiveCount == 128);
            
            // Demand main range active to prove indices are well packed.
            for (int i = 0; i < 128; i++)
                Assert.True(list.IndexActive(i));
            
            // Take out a safeptr and check that it's valid
            var safePtr = list.RentIndexPointer(12);
            var safePtr2 = list.RentIndexPointer(13);

            safePtr.ValueCopy = 12345;
            safePtr2.ValueCopy = 54321;
            
            Assert.True(list[12].Equals(12345));
            Assert.True(list[13].Equals(54321));

            list.RemoveAtClear(12);
            Assert.False(safePtr.IsCreated);
            Assert.True(safePtr2.IsCreated);
            
            list.Clear();
            Assert.False(safePtr2.IsCreated);
            
            list.Dispose();
        }
    }
}