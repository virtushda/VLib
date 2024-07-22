using System;

namespace VLib
{
    [Serializable]
    public class MaskableArrayParam<T> : IArrayParam<T[]>, IParamOverridable<T[]>
    {
        T[] array;
        public bool Overriden { get; set; }
        public T[] OverridenValue { get; set; }
        public int Count { get; set; }
        public T[] Value { get => array; set => array = value; }

        public MaskableArrayParam(T[] array)
        {
            this.array = array;
        }
    } 
}