using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace VLib.Collections
{
    /// <summary> A collection optimized for selecting from a large set of weighted options. </summary>
    public struct VUnsafeWeightedIndices
    {
        VUnsafeSortedList<Option> options;
        double totalWeight;
        float minWeight;
        float maxWeight;
        
        [field: MarshalAs(UnmanagedType.U1)]
        public bool Dirty { get; [MethodImpl(MethodImplOptions.AggressiveInlining)] private set; }
        public double TotalWeight => totalWeight;
        public float MinWeight => minWeight;
        public float MaxWeight => maxWeight;
        public int Count => options.Count;
        
        public Option this[int index] => options[index];
        
        public VUnsafeWeightedIndices(int capacity, Allocator allocator)
        {
            options = new VUnsafeSortedList<Option>(capacity, allocator);
            totalWeight = 0;
            minWeight = float.MaxValue;
            maxWeight = float.MinValue;
            Dirty = false;
        }
        
        public void Dispose()
        {
            options.Dispose();
        }
        
        public void Add(int id, float weight)
        {
            BurstAssert.True(weight > 0); // Must have a weight
            options.Add(new Option { id = id, weight = weight });
            Dirty = true;
        }
        
        public void Remove(int id)
        {
            options.Remove(new Option { id = id }); // Rely on Option.Equals
            Dirty = true;
        }
        
        public void Clear()
        {
            options.Clear();
            Dirty = true;
        }

        static readonly ProfilerMarker UpdateFromMarker = new("VUnsafeWeightedIndices.UpdateFrom");
        /// <summary> Checks the internal IDs and if present in the hashmap, updates the weights. </summary>
        public void UpdateFrom(UnsafeHashMap<int, float> idWeightMap)
        {
            using var _ = UpdateFromMarker.Auto();
            bool recalculationNeeded = false;
            
            var optionCount = options.Count;
            for (int i = 0; i < optionCount; i++)
            {
                ref var option = ref options.list.ElementAt(i);
                if (!idWeightMap.TryGetValue(option.id, out var newWeight))
                    continue;
                if (option.weight.Equals(newWeight))
                    continue;
                option.weight = newWeight;
                recalculationNeeded = true;
            }

            if (recalculationNeeded)
            {
                options.Resort();
                this.RecalculateBurst();
            }
        }

        /// <summary> Burst compiled version recommended unless already in burst context: <see cref="VUnsafeWeightedIndicesBurst.GetRandomBurst"/> <br/>
        /// <inheritdoc cref="VUnsafeWeightedIndicesBurst.GetRandomBurst"/> </summary>
        public int GetRandom_DirectCall(ref Random random, bool allowRecalculate = false)
        {
            if (Dirty)
            {
                if (!allowRecalculate)
                {
                    // If you reach this error, you need to ensure that the collection is properly recalculated before calling GetRandom
                    // Disallowing recalculation is ideal when multiple threads are going to call this method, we can't allow one or multiple to start recalculating!
                    Debug.LogError("VUnsafeWeightedIndices.GetRandom: Dirty, but recalculation is not allowed! Returning first option for safety, undesired results may follow.");
                    return -1;
                }
                Recalculate_DirectCall();
            }

            BurstAssert.False(Dirty);
            BurstAssert.False(options.Count == 0);
            
            var randomWeight = random.NextDouble() * totalWeight;
            randomWeight = math.max(randomWeight, double.Epsilon);
            randomWeight = math.min(randomWeight, totalWeight - double.Epsilon);

            var index = GetIndexBinarySearch(randomWeight);
            return options[index].id;
        }

        /// <summary>Returns the index of the option in *this* collection, not the ID of the option</summary>
        public int GetIndexBinarySearch(double weight)
        {
            // Binary range search
            var min = 0;
            var max = options.Count;
            while (min + 1 < max)
            {
                var mid = (min + max) / 2;
                if (options[mid].precedingWeightSum < weight)
                    min = mid;
                else
                    max = mid;
            }
            return min;
            
            /*Classic Binary Search
            var min = 0;
            var max = options.Count - 1;
            while (min < max)
            {
                var mid = (min + max) / 2;
                if (options[mid].precedingWeightSum < weight)
                    min = mid + 1;
                else
                    max = mid;
            }
            return min;*/
        }
        
        /// <summary>Returns the index of the option in *this* collection, not the ID of the option</summary>
        public int GetIndexLinearSearch(double weight)
        {
            for (var index = options.Count - 1; index >= 0; index--)
            {
                if (options[index].precedingWeightSum < weight)
                    return index;
            }
            return 0;
        }

        static readonly ProfilerMarker RecalculateMarker = new("VUnsafeWeightedIndices.Recalculate");
        /// <summary> <inheritdoc cref="VUnsafeWeightedIndicesBurst.RecalculateBurst"/>
        /// <br/>
        /// Burst compiled version: <see cref="VUnsafeWeightedIndicesBurst.RecalculateBurst"/> </summary>
        public void Recalculate_DirectCall()
        {
            // Reset
            totalWeight = 0;
            minWeight = float.MaxValue;
            maxWeight = float.MinValue;
            
            if (options.Count == 0)
                return;
            
            using var _ = RecalculateMarker.Auto();
            
            // Accumulate weights
            for (int i = 0; i < options.Count; i++)
            {
                ref var option = ref options.list.ElementAt(i);
                option.precedingWeightSum = totalWeight;
                totalWeight += option.weight;
                
                minWeight = math.min(minWeight, option.weight);
                maxWeight = math.max(maxWeight, option.weight);
            }
            
            Dirty = false;
        }

        public struct Option : IComparable<Option>, IEquatable<Option>
        {
            public int id;
            public float weight;
            public double precedingWeightSum;

            public int CompareTo(Option other) => id.CompareTo(other.id);

            public bool Equals(Option other) => id == other.id;
            public override bool Equals(object obj) => obj is Option other && Equals(other);

            public override int GetHashCode() => id;

            public static bool operator ==(Option left, Option right) => left.Equals(right);
            public static bool operator !=(Option left, Option right) => !left.Equals(right);
        }
    }

    [BurstCompile]
    public static class VUnsafeWeightedIndicesBurst
    {
        /// <summary> Recalculates the total weight and internal jump table. It's very important for the collection to be properly calculated before sampling it. </summary>
        [BurstCompile]
        public static void RecalculateBurst(ref this VUnsafeWeightedIndices indices) => indices.Recalculate_DirectCall();

        /// <summary> Samples the indices with a weighted random. </summary>
        /// <param name="allowRecalculation"> Disallowing recalculation is needed when multiple threads are going to call this method, we can't allow one or multiple to start recalculating! <br/>
        /// This field is 'false' by default for safety purposes. </param>
        [BurstCompile]
        public static int GetRandomBurst(ref this VUnsafeWeightedIndices indices, ref Random random, bool allowRecalculation = false) => indices.GetRandom_DirectCall(ref random, allowRecalculation);
    }
}