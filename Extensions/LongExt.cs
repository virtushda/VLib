namespace VLib
{
    public static class LongExt
    {
        public static string AsTimeToPrint(this long seconds) => ((double) seconds).AsTimeToPrint();
    }
}