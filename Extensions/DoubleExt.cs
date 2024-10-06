using VLib.Systems;

namespace VLib
{
    public static class DoubleExt
    {
        public static string AsTimeToPrint(this double seconds)
        {
            if (seconds < .0001f)
                return $"{VTime.SecondsToTicks(seconds)}ticks";
            if (seconds < .1f)
                return $"{VTime.SecondsToMSFrac(seconds):F}ms";
            if (seconds < 60)
                return $"{seconds:F}secs";
            if (seconds < 3600)
                return $"{VTime.SecondsToMinutesFrac(seconds):F} minutes";
            if (seconds < 3600 * 24)
                return $"{VTime.SecondsToHoursFrac(seconds):F} hours";

            return $"{(VTime.SecondsToHoursFrac(seconds) / 24):F} days";
        }
    }
}