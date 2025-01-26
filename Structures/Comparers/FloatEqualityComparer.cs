using System.Collections.Generic;
using Unity.Mathematics;

namespace VLib.Libraries.VLib.Structures.Comparers
{
    public struct FloatEqualityComparer : IEqualityComparer<float>
    {
        float epsilon;
        
        public FloatEqualityComparer(float epsilon) => this.epsilon = epsilon;

        public bool Equals(float x, float y) => math.abs(x - y) <= epsilon;

        public int GetHashCode(float obj) => obj.GetHashCode();
    }
}