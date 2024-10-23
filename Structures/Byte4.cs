using System;
using UnityEngine;

namespace VLib.Structures
{
    [Serializable]
    public struct Byte4
    {
        public byte x;
        public byte y;
        public byte z;
        public byte w;

        public byte this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return x;
                    case 1: return y;
                    case 2: return z;
                    case 3: return w;
                    default: 
                        Debug.LogError($"Index {index} out of range");
                        return 0;
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        x = value;
                        break;
                    case 1:
                        y = value;
                        break;
                    case 2:
                        z = value;
                        break;
                    case 3:
                        w = value;
                        break;
                    default:
                        Debug.LogError($"Index {index} out of range");
                        break;
                }
            }
        }
        
        public Byte4(byte x, byte y, byte z, byte w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
    }
}