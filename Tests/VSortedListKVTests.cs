using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace VLib.Tests
{
    public class VSortedListKVTests
    {
        [Test]
        public void VSortedListTKSimplePasses()
        {
            const int iterations = 1024;

            var regularList = new List<float>(iterations);
            var sortedList = new VSortedList<float, int>(iterations);
            
            for (int i = 0; i < iterations; i++)
            {
                float key = Random.value;
                
                regularList.Add(key);
                sortedList.Add(key, i);
            }

            // Check against regular list sort
            regularList.Sort();
            
            for (int i = 0; i < iterations; i++)
                Assert.IsTrue(regularList[i].Equals(sortedList.Keys[i]));

            Assert.Pass();
        }

        /*// A UnityTest behaves like a coroutine in PlayMode
        // and allows you to yield null to skip a frame in EditMode
        [UnityTest]
        public IEnumerator VSortedListTKWithEnumeratorPasses()
        {
            // Use the Assert class to test conditions.
            // yield to skip a frame
            yield return null;
        }*/
    }
}