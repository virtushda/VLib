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
                switch (index)
                {
                    case 0: return x;
                    case 1: return y;
                    default: return 0;
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
                }
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
    }
}