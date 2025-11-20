using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using VLib.Collections;

namespace VLib.Tests
{
    public class VUnsafeWeightedIndicesTests
    {
        const int iterations = 100000;
        const int length = 1000;
        const float weightRange = 2000f;
        
        [Test]
        public void LinearSearchAccuracyTest()
        {
            var weightedIndices = GenerateStandardRandomWeightedIndices();
            
            Debug.Log($"Total weight: {weightedIndices.TotalWeight}");
            
            for (int i = 0; i < iterations; i++)
            {
                var testWeight = (double)i / iterations * weightedIndices.TotalWeight;
                var linearSearchIndex = weightedIndices.GetIndexLinearSearch(testWeight);

                var linearOption = weightedIndices[linearSearchIndex];
                var minInclusive = linearOption.precedingWeightSum;
                var maxExclusive = minInclusive + linearOption.weight;
                if (minInclusive > testWeight || testWeight >= maxExclusive)
                    Assert.Fail($"Linear search failed for weight {testWeight} at index {linearSearchIndex}, expected weight between {minInclusive} and {maxExclusive}! (iteration {i})");
            }
            Assert.Pass();
        }
        
        [Test]
        public void BinarySearchAccuracyTest()
        {
            var weightedIndices = GenerateStandardRandomWeightedIndices();
            
            Debug.Log($"Total weight: {weightedIndices.TotalWeight}");
            
            for (int i = 0; i < iterations; i++)
            {
                var testWeight = (double)i / iterations * weightedIndices.TotalWeight;
                var binarySearchIndex = weightedIndices.GetIndexBinarySearch(testWeight);
                
                var binaryOption = weightedIndices[binarySearchIndex];
                var minInclusive = binaryOption.precedingWeightSum;
                var maxExclusive = minInclusive + binaryOption.weight;
                if (minInclusive > testWeight || testWeight >= maxExclusive)
                    Assert.Fail($"Binary search failed for weight {testWeight} at index {binarySearchIndex}, expected weight between {minInclusive} and {maxExclusive}! (iteration {i})");
            }

            Assert.Pass();
        }
        
        [Test]
        public void BinaryVsLinearTest()
        {
            var weightedIndices = GenerateStandardRandomWeightedIndices();
            
            Debug.Log($"Total weight: {weightedIndices.TotalWeight}");
            
            for (int i = 0; i < iterations; i++)
            {
                var testWeight = (double)i / iterations * weightedIndices.TotalWeight;
                var linearSearchIndex = weightedIndices.GetIndexLinearSearch(testWeight);
                var binarySearchIndex = weightedIndices.GetIndexBinarySearch(testWeight);
                
                if (linearSearchIndex != binarySearchIndex)
                    Assert.Fail($"Binary search index does not match linear search index for weight {testWeight}! (iteration {i})");
            }

            Assert.Pass();
        }

        static VUnsafeWeightedIndices GenerateStandardRandomWeightedIndices()
        {
            var rand = new Unity.Mathematics.Random(123456);
            var weightedIndices = new VUnsafeWeightedIndices(length, Allocator.Temp);
            for (int i = 0; i < length; i++)
            {
                weightedIndices.Add(i, rand.NextFloat(weightRange));
            }
            weightedIndices.RecalculateBurst();
            return weightedIndices;
        }
    }
}