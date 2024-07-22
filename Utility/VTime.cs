using System;

namespace VLib
{
    public static class VTime
    {
        public static ulong SecondsToNanoSeconds(double seconds) => (ulong)(seconds * 1000000000);

        public static long SecondsToTicks(double seconds) => (long)(seconds * 10000000);

        public static long MillisecondsToTicks(double ms) => (long)(ms * 10000);

        public static double SecondsToMSFrac(double seconds) => seconds * 1000;

        public static double SecondsToMinutesFrac(double seconds) => seconds / 60f;

        public static double SecondsToHoursFrac(double seconds) => seconds / 3600f;

        public static double TicksToSeconds(long ticks) => ticks / 10000000f;
    }
}