using System;
using System.Collections.Generic;

namespace VLib
{
    public struct ComparableComparer<T> : IComparer<T>
        where T : IComparable<T>
    {
        public int Compare(T x, T y)
        {
            return x.CompareTo(y);
        }
    }
}