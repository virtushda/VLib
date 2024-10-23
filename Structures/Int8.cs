using System;
using Unity.Collections;
using Unity.Mathematics;

namespace VLib.Structures
{
    [Serializable]
    public struct Int8
    {
        public int4x2 matrix;
        
        public int this [int index]
        {
            get => index < 4 ? matrix.c0[index] : matrix.c1[index - 4];
            set
            {
                if (index < 4)
                    matrix.c0[index] = value;
                else
                    matrix.c1[index - 4] = value;
            }
        }
        
        public Int8(int value) => matrix = new int4x2(value);
        
        public Int8(int4x2 value) => matrix = value;
        
        public Int8(int x0, int y0, int z0, int w0, int x1, int y1, int z1, int w1)
        {
            matrix = new int4x2(new int4(x0, y0, z0, w0), new int4(x1, y1, z1, w1));
        }

        /// <summary> Takes the first 8 values from the collection. </summary>
        public void WriteValuesFrom<T>(T valueCollection)
            where T : IIndexable<int>
        {
            var count = math.min(valueCollection.Length, 8);
            // First 4
            ref var c0 = ref matrix.c0;
            for (var i = 0; i < count; i++)
                c0[i] = valueCollection.ElementAt(i);
            // Last 4
            ref var c1 = ref matrix.c1;
            for (var i = 4; i < count; i++)
                c1[i - 4] = valueCollection.ElementAt(i);
        }
    }
}