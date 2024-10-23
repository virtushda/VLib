using System;
using System.Diagnostics;

namespace VLib
{
    public readonly struct ValueStopwatch
    {
        static readonly double TimestampToTicks = TimeSpan.TicksPerSecond / (double) Stopwatch.Frequency;

        readonly long startTimestamp;

        public static ValueStopwatch StartNew() => new ValueStopwatch(Stopwatch.GetTimestamp());

        ValueStopwatch(long startTimestamp)
        {
            this.startTimestamp = startTimestamp;
        }

        public TimeSpan Elapsed => TimeSpan.FromTicks(ElapsedTicks);

        public bool IsInvalid => startTimestamp == 0;

        public readonly long ElapsedTicks
        {
            get
            {
                if (startTimestamp == 0)
                {
                    throw new InvalidOperationException("Detected invalid initialization(use 'default'), only to create from StartNew().");
                }

                var delta = Stopwatch.GetTimestamp() - startTimestamp;
                return (long) (delta * TimestampToTicks);
            }
        }

        public readonly float ElapsedMillisecondsF => ElapsedTicks / (float) TimeSpan.TicksPerMillisecond;
        
        public readonly long ElapsedMilliseconds => ElapsedTicks / TimeSpan.TicksPerMillisecond;
        
        public readonly float ElapsedSecondsF => ElapsedTicks / (float) TimeSpan.TicksPerSecond;
        
        public readonly long ElapsedSeconds => ElapsedTicks / TimeSpan.TicksPerSecond;
        
        public readonly float ElapsedMinutesF => ElapsedTicks / (float) TimeSpan.TicksPerMinute;
        
        public readonly long ElapsedMinutes => ElapsedTicks / TimeSpan.TicksPerMinute;
        
        public readonly float ElapsedHoursF => ElapsedTicks / (float) TimeSpan.TicksPerHour;
        
        public readonly long ElapsedHours => ElapsedTicks / TimeSpan.TicksPerHour;

        public override string ToString() => ElapsedSecondsF.AsTimeToPrint();
    }
}