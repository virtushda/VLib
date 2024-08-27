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

        /*[GenerateTestsForBurstCompatibility]
        public static class CrossPlatform
        {
            // UNIX IMPL UNTESTED

            /// <summary> System time in nanoseconds since 1970, should work for 580 years from then </summary>
            [GenerateTestsForBurstCompatibility]
            public static ulong GetSystemNanoseconds()
            {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                return GetSystemNanosecondsWindows();
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
                return GetSystemNanosecondsUnix();
#else
                throw new PlatformNotSupportedException("Unsupported platform");
#endif
            }
            
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            // Windows implementation
            private static ulong GetSystemNanosecondsWindows()
            {
                GetSystemTimeAsFileTime(out long fileTime);
                return (ulong)fileTime * 100; // Convert 100-nanosecond intervals to nanoseconds
            }

            [DllImport("Kernel32.dll")]
            private static extern void GetSystemTimeAsFileTime(out long lpSystemTimeAsFileTime);
#endif

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
            // Unix implementation
            private static ulong GetSystemNanosecondsUnix()
            {
                timespec time;
                clock_gettime(CLOCK_REALTIME, out time);
                return (ulong)time.tv_sec * 1_000_000_000 + (ulong)time.tv_nsec;
            }

            private const int CLOCK_REALTIME = 0;

            [StructLayout(LayoutKind.Sequential)]
            private struct timespec
            {
                public long tv_sec;  // seconds
                public long tv_nsec; // nanoseconds
            }

            [DllImport("libc")]
            private static extern int clock_gettime(int clk_id, out timespec tp);
#endif
        }*/
    }
}