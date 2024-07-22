using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace VLib
{
    [GenerateTestsForBurstCompatibility]
    public unsafe struct UnsafeWeightedValueSet<T>
        where T : unmanaged
    {
        public int Length => weightValuePairs->Length;

        [SerializeField] public UnsafeList<WeightedSetRep>* weightValuePairs;

        VUnsafeRef<float> totalWeightRef;
        
        public float[] Weights
        {
            get
            {
                float[] weightsArray = new float[weightValuePairs->Length];
                for (int i = 0; i < weightsArray.Length; i++)
                    weightsArray[i] = (*weightValuePairs)[i].weight;
                return weightsArray;
            }
        }

        public T[] Values
        {
            get
            {
                T[] valuesArray = new T[weightValuePairs->Length];
                for (int i = 0; i < valuesArray.Length; i++)
                    valuesArray[i] = (*weightValuePairs)[i].value;
                return valuesArray;
            }
        }

        [Serializable]
        public struct WeightedSetRep
        {
            [Range(.2f, 5)]
            public float weight;
            public T value;

            public WeightedSetRep(T value, float weight = 1)
            {
                this.weight = weight;
                this.value = value;
            }
        }

        public UnsafeWeightedValueSet(int length) : this()
        {
            totalWeightRef = new VUnsafeRef<float>(-1, Allocator.Persistent);
            EnsureInitInternal(length);
        }

        public void Dispose()
        {
            totalWeightRef.Dispose();
            weightValuePairs->DisposeSafe();
        }

        public WeightedSetRep this[int i]
        {
            get => (*weightValuePairs)[i];
            set => (*weightValuePairs)[i] = value;
        }

        public void Resize(int newLength)
        {
            if (Length != newLength)
                weightValuePairs->Resize(newLength);
        }

        void EnsureInitInternal(int length)
        {
            if (weightValuePairs == null)
            {
                weightValuePairs = UnsafeList<WeightedSetRep>.Create(length, Allocator.Persistent);
                // Set default values
                for (int i = 0; i < weightValuePairs->Length; i++)
                {
                    (*weightValuePairs)[i] = new WeightedSetRep(default);
                }
            }
            else
            {
                Resize(length);
            }
        }

        public int GetRandomIndexWeighted()
        {
            if (totalWeightRef.Value <= 0.01f)
                UpdateTotalWeight();

            float random = UnityEngine.Random.Range(0, totalWeightRef.Value);

            return GetIndexByWeight(random);
        }

        public int GetRandomIndexWeighted(in Unity.Mathematics.Random random)
        {
            if (totalWeightRef.Value <= 0.01f)
                UpdateTotalWeight();

            float weight = random.NextFloat(0, totalWeightRef.Value);

            return GetIndexByWeight(weight);
        }

        public int GetIndexByWeight(float weight)
        {
            float cummulativeWeight = 0;
            for (int i = 0; i < Length; i++)
            {
                cummulativeWeight += (*weightValuePairs)[i].weight;
                if (weight <= cummulativeWeight)
                    return i;
            }
            throw new UnityException($"Selection by weight failed! Weight: {weight} is outside range [0 - {totalWeightRef.Value}]");
        }

        public void UpdateTotalWeight()
        {
            totalWeightRef.Value = 0;
            for (int i = 0; i < weightValuePairs->Length; i++)
            {
                var val = totalWeightRef.Value;
                val += (*weightValuePairs)[i].weight;
                totalWeightRef.Value = val;
            }
        }
    }
}