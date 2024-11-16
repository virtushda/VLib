namespace VLib.Structures
{
    public struct Byte2
    {
        public byte x;
        public byte y;
        
        public Byte2(byte x, byte y)
        {
            this.x = x;
            this.y = y;
        }

        public bool IsValidSbyte2(out Sbyte2 sbyte2)
        {
            if (x <= sbyte.MaxValue && y <= sbyte.MaxValue)
            {
                sbyte2 = new Sbyte2((sbyte) x, (sbyte) y);
                return true;
            }
            sbyte2 = new Sbyte2();
            return false;
        }
    }
}