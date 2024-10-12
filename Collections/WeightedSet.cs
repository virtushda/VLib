using System;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace VLib
{
    [Serializable]
    public class WeightedSet<T>
    {
/*#if ODIN_INSPECTOR
        [OdinSerialize]
        //[NonSerialized]
        [ShowInInspector] 
#endif */
        public WeightedSetRep[] weightValuePairs;

/*#if ODIN_INSPECTOR
        [OdinSerialize, ShowInInspector]
        //[NonSerialized]
        [ReadOnly] 
#else
        [SerializeField]
#endif*/
        [SerializeField]
        float totalWeight = -1;

        [Serializable]
        public class WeightedSetRep
        {
/*#if ODIN_INSPECTOR
            [OdinSerialize] 
            //[NonSerialized]
#endif*/
            public float weight;
/*#if ODIN_INSPECTOR
            [OdinSerialize] 
            //[NonSerialized]
#endif*/
            public T value;

            public WeightedSetRep(T value, float weight = 1)
            {
                this.weight = weight;
                this.value = value;
            }
            
            public void CopyFrom(WeightedSetRep other)
            {
                weight = other.weight;
                value = other.value;
            }
        }

        public float[] Weights
        {
            get
            {
                float[] weightsArray = new float[weightValuePairs.Length];
                for (int i = 0; i < weightsArray.Length; i++)
                    weightsArray[i] = weightValuePairs[i].weight;
                return weightsArray;
            }
        }

        public T[] Values
        {
            get
            {
                T[] valuesArray = new T[weightValuePairs.Length];
                for (int i = 0; i < valuesArray.Length; i++)
                    valuesArray[i] = weightValuePairs[i].value;
                return valuesArray;
            }
            set
            {
#if UNITY_EDITOR
                if (weightValuePairs == null)
                    throw new UnityException("Internal array is null, this weighted set was not generated correctly!");
#endif
                for (int i = 0; i < weightValuePairs.Length; i++)
                    weightValuePairs[i].value = value[i];
            }
        }

        public void CopyTo(WeightedSet<T> other)
        {
            other.weightValuePairs = new WeightedSetRep[weightValuePairs.Length];
            for (int i = 0; i < weightValuePairs.Length; i++)
            {
                other.weightValuePairs[i] = new WeightedSetRep(default);
                other.weightValuePairs[i].CopyFrom(weightValuePairs[i]);
            }

            other.UpdateTotalWeight();
        }
        
        public int Length => weightValuePairs.Length;

        public float TotalWeight
        {
            get
            {
                if (totalWeight <= 0.01f)
                    UpdateTotalWeight();
                return totalWeight;
            }
        }

        public WeightedSet(int length)
        {
            EnsureInitInternal(length);
        }

        public WeightedSet(T[] array)
        {
            EnsureInitInternal(array.Length);
            Values = array;
            UpdateTotalWeight();
        }

        public WeightedSetRep this[int i]
        {
            get => weightValuePairs[i];
            set => weightValuePairs[i] = value;
        }

        public void Resize(int newLength)
        {
            var oldLength = Length;
            if (oldLength != newLength)
                Array.Resize(ref weightValuePairs, newLength);
            
            // Set new defaults
            for (int i = oldLength; i < newLength; i++)
                weightValuePairs[i] = new WeightedSetRep(default);
        }

        void EnsureInitInternal(int length)
        {
            if (weightValuePairs == null)
            {
                weightValuePairs = new WeightedSetRep[length];
                // Set default values
                for (int i = 0; i < weightValuePairs.Length; i++)
                {
                    if (weightValuePairs[i] == null)
                        weightValuePairs[i] = new WeightedSetRep(default);
                }
            }
            else
            {
                Resize(length);
            }
        }

        /*public bool Contains(T value)
        {
            for (int i = 0; i < weightValuePairs.Length; i++)
            {
                if (value == weightValuePairs[i].value)
                    return true;
            }

            return false;
        }*/

        /// <summary> Main thread only </summary>
        public int GetRandomIndexWeighted()
        {
            float random = UnityEngine.Random.Range(0, TotalWeight);

            return GetIndexByWeight(random);
        }

        /// <summary> Thread-safe version </summary>
        public int GetRandomIndexWeighted(ref Unity.Mathematics.Random random)
        {
            float weight = random.NextFloat(0, TotalWeight);
            return GetIndexByWeight(weight);
        }
        
        /// <summary> Main thread only </summary>
        public T GetRandomValueWeighted() => weightValuePairs[GetRandomIndexWeighted()].value;
        
        /// <summary> Thread-safe version </summary>
        public T GetRandomValueWeighted(ref Unity.Mathematics.Random random) => weightValuePairs[GetRandomIndexWeighted(ref random)].value;

        public int GetIndexByWeight(float weight)
        {
            float cummulativeWeight = 0;
            for (int i = 0; i < Length; i++)
            {
                cummulativeWeight += weightValuePairs[i].weight;
                if (weight <= cummulativeWeight)
                    return i;
            }
            throw new UnityException($"Selection by weight failed! Weight: {weight} is outside range [0 - {totalWeight}]");
        }

#if ODIN_INSPECTOR
        [Button]
#endif
        public void UpdateTotalWeight()
        {
            totalWeight = 0;
            for (int i = 0; i < weightValuePairs.Length; i++)
            {
                if (weightValuePairs[i] != null)
                    totalWeight += weightValuePairs[i].weight;
            }
        }

        /// <summary> Update the weight and return true if it changed. </summary>
        public bool CheckUpdateTotalWeight()
        {
            var prevWeight = totalWeight;
            UpdateTotalWeight();
            return Math.Abs(prevWeight - totalWeight) > 0.01f;
        }
    }
}