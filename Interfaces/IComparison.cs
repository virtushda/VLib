namespace VLib
{
    public interface IComparison<in T>
    {
        int CompareTo(T obj);
    }

    public struct IntCompareReverse : IComparison<int>
    {
        int value;

        public IntCompareReverse(int value) => this.value = value;

        public int CompareTo(int obj)
        {
            if (value > obj)
                return -1;
            if (value < obj)
                return 1;
            return 0;
        }
    }

    public struct FloatCompareReverse : IComparison<float>
    {
        float value;

        public FloatCompareReverse(float value) => this.value = value;

        public int CompareTo(float obj)
        {
            if (value > obj)
                return -1;
            if (value < obj)
                return 1;
            return 0;
        }
    }
}