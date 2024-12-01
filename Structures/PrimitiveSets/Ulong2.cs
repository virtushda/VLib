using System.Diagnostics;

namespace VLib.Structures
{
    [System.Serializable]
    public struct Ulong2
    {
        public ulong x;
        public ulong y;
        
        public ulong this[int index]
        {
            get
            {
                ConditionalCheckIndex(index);
                return index == 0 ? x : y;
            }
            set
            {
                ConditionalCheckIndex(index);
                if (index == 0)
                    x = value;
                else
                    y = value;
            }
        }
        
        public Ulong2(ulong x, ulong y)
        {
            this.x = x;
            this.y = y;
        }
        
        public ulong CMin() => x < y ? x : y;
        public ulong CMax() => x > y ? x : y;
        
        public static implicit operator Ulong2(ulong value) => new(value, value);

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        public void ConditionalCheckIndex(int index)
        {
            if (index < 0)
                UnityEngine.Debug.LogError("Index is less than 0");
            if (index > 1)
                UnityEngine.Debug.LogError("Index is greater than 1");
        }
    }
}