using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace VLib.Structures
{
    [Serializable, StructLayout(LayoutKind.Explicit)]
    public struct Sbyte8
    {
        [FieldOffset(0)] public sbyte _0;
        [FieldOffset(1)] public sbyte _1;
        [FieldOffset(2)] public sbyte _2;
        [FieldOffset(3)] public sbyte _3;
        [FieldOffset(4)] public sbyte _4;
        [FieldOffset(5)] public sbyte _5;
        [FieldOffset(6)] public sbyte _6;
        [FieldOffset(7)] public sbyte _7;
        
        public Sbyte8(sbyte _0, sbyte _1, sbyte _2, sbyte _3, sbyte _4, sbyte _5, sbyte _6, sbyte _7)
        {
            this._0 = _0;
            this._1 = _1;
            this._2 = _2;
            this._3 = _3;
            this._4 = _4;
            this._5 = _5;
            this._6 = _6;
            this._7 = _7;
        }

        public Sbyte8(sbyte value)
        {
            this._0 = value;
            this._1 = value;
            this._2 = value;
            this._3 = value;
            this._4 = value;
            this._5 = value;
            this._6 = value;
            this._7 = value;
        }

        public sbyte this[int index]
        {
            get
            {
                ConditionalCheckIndex(index);
                return index switch
                {
                    0 => _0,
                    1 => _1,
                    2 => _2,
                    3 => _3,
                    4 => _4,
                    5 => _5,
                    6 => _6,
                    7 => _7,
                    _ => throw new IndexOutOfRangeException($"Index {index} out of valid range 0-7.")
                };
            }
            set
            {
                ConditionalCheckIndex(index);
                switch (index)
                {
                    case 0:
                        _0 = value;
                        break;
                    case 1:
                        _1 = value;
                        break;
                    case 2:
                        _2 = value;
                        break;
                    case 3:
                        _3 = value;
                        break;
                    case 4:
                        _4 = value;
                        break;
                    case 5:
                        _5 = value;
                        break;
                    case 6:
                        _6 = value;
                        break;
                    case 7:
                        _7 = value;
                        break;
                    default: throw new IndexOutOfRangeException($"Index {index} out of valid range 0-7.");
                }
            }
        }
        
        // Conditional check index
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        public void ConditionalCheckIndex(int index)
        {
            if (index < 0)
                UnityEngine.Debug.LogError("Index is less than 0");
            if (index > 7)
                UnityEngine.Debug.LogError("Index is greater than 7");
        }
    }
}