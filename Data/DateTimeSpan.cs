using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;
using VLib.Attributes;

namespace VLib
{
    [Serializable]
    public struct UnityDateTime
    {
        long ticks;
    }
    
    [Serializable]
    public struct DateTimeSpan : IEquatable<DateTimeSpan>
    {
        [SerializeField] DateTimeSerializable start;
        [SerializeField] DateTimeSerializable end;
        
        public DateTime Start => start;
        public DateTime End => end;
        
        public DateTimeSpan(DateTime start, DateTime end)
        {
            if (start > end)
                throw new ArgumentOutOfRangeException(nameof(end), "End must be after start.");
            if (start.Kind is DateTimeKind.Unspecified)
                throw new ArgumentOutOfRangeException(nameof(start), "Start must be specified as UTC or Local.");
            if (end.Kind is DateTimeKind.Unspecified)
                throw new ArgumentOutOfRangeException(nameof(end), "End must be specified as UTC or Local.");
            if (start.Kind != end.Kind)
                throw new ArgumentOutOfRangeException(nameof(end), $"Start kind '{start.Kind}' and end kind '{end.Kind}' must be the same.");
            this.start = start;
            this.end = end;
        }
        
        public TimeSpan Duration => End - Start;

        /// <summary> True if dateTime is within [Start (inclusive), End (inclusive)]. </summary>
        public bool ContainsAllInclusive(DateTime dateTime) => dateTime >= Start && dateTime <= End;
        
        /// <summary> True if dateTime is within [Start (inclusive), End (exclusive)]. </summary>
        public bool ContainsInclusiveExclusive(DateTime dateTime) => dateTime >= Start && dateTime < End;

        public float GetLerpPositionClamped(DateTime dateTime)
        {
            if (dateTime <= Start)
                return 0;
            if (dateTime >= End)
                return 1;
            return (float)((dateTime - Start).TotalSeconds / Duration.TotalSeconds);
        }

        public DateTimeSpan GetRoundedToMonth()
        {
            var newStart = new DateTime(Start.Year, Start.Month, 1, 0, 0, 0, Start.Kind);
            
            // Check if already very close to the end of the month
            var endOfCurrentMonth = new DateTime(End.Year, End.Month, 1, 0, 0, 0, End.Kind).AddMonths(1);
            var delta = endOfCurrentMonth - End;
            if (delta.TotalSeconds < 5)
            {
                // Keep end, ensure it's perfect though
                return new DateTimeSpan(newStart, endOfCurrentMonth);
            }
            // Round back to the end of the previous month
            return new DateTimeSpan(newStart, endOfCurrentMonth.AddMonths(-1));
        }
        
        public DateTime Clamp(DateTime dateTime) => dateTime < Start ? Start : (dateTime > End ? End : dateTime);

        /// <summary> Returns a timespan that starts at the start of the year and ends at the end of yesterday. </summary>
        public static DateTimeSpan YearToYesterdayUTC
        {
            get
            {
                var now = DateTime.Now.ToUniversalTime();
                var start = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var end = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
                return new DateTimeSpan(start, end);
            }
        }

        public static DateTimeSpan AllTimeUTC => new(DateTimeExt.MinValueUTC, DateTimeExt.MaxValueUTC);

        /// <summary> A reasonable bounding span for anything in the modern era. </summary>
        public static DateTimeSpan Start1900End2100 => new(new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        
        public static DateTimeSpan Start1900EndNowUTC => new(new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc), DateTime.Now.ToUniversalTime());

        public override string ToString() => $"{Start} to {End} ({Start.Kind})";
        
        /*[Button]
        public void SetBy(int startYear, byte startMonth, int lastYear, byte lastMonth, bool clampToNow = true)
        {
            Start = new DateTime(startYear, startMonth, 1);
            End = new DateTime(lastYear, lastMonth, 1).AddMonths(1).AddDays(-1);
            
            if (clampToNow)
            {
                Start = Start > DateTime.Now ? DateTime.Now : Start;
                End = End > DateTime.Now ? DateTime.Now : End;
            }
        }*/

        /// <summary> Step through all months in the span. </summary>
        public IEnumerable<DateTime> EnumerateMonths()
        {
            var startFloorToMonth = new DateTime(Start.Year, Start.Month, 1);
            var endCeilToMonth = new DateTime(End.Year, End.Month, 1).AddMonths(1).AddDays(-1);
            for (var dateTime = startFloorToMonth; dateTime <= endCeilToMonth; dateTime = dateTime.AddMonths(1))
                yield return dateTime;
        }

        /// <summary> Step through all months in the span in reverse order. </summary>
        public IEnumerable<DateTime> EnumerateMonthsReverse()
        {
            var startFloorToMonth = new DateTime(Start.Year, Start.Month, 1);
            var endFloorToMonth = new DateTime(End.Year, End.Month, 1);
            for (var dateTime = endFloorToMonth; dateTime >= startFloorToMonth; dateTime = dateTime.AddMonths(-1))
                yield return dateTime;
        }
        
        /// <summary> Step through range in 3-month chunks. Stops on borders of months as much as possible. </summary>
        public IEnumerable<DateTimeSpan> Enumerate3MonthChunks()
        {
            var kind = Start.Kind;
            var start = Start;
            var end = End;
            var currentStart = start;
            while (currentStart < end)
            {
                var currentEnd = currentStart.AddMonths(3);
                
                // Round to nearest end of month
                if (currentEnd.Day < 15)
                    currentEnd = new DateTime(currentEnd.Year, currentEnd.Month, 1, 0, 0, 0, kind);
                else
                    currentEnd = new DateTime(currentEnd.Year, currentEnd.Month, 1, 0, 0, 0, kind).AddMonths(1);
                
                if (currentEnd > end)
                    currentEnd = end;
                var nextSpan = new DateTimeSpan(currentStart, currentEnd);
                Assert.IsTrue(nextSpan.Duration.Days < 120, "3 month chunk is longer than 120 days!");
                yield return new DateTimeSpan(currentStart, currentEnd);
                currentStart = currentEnd;
            }
        }
        
        /// <summary> Enumerates [Start, End] as segments of length <= segmentDuration, with a non-negative gap between segments. </summary>
        public IEnumerable<DateTimeSpan> EnumerateSegments(TimeSpan segmentDuration, TimeSpan gapDuration)
        {
            if (segmentDuration <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(segmentDuration), "segmentDuration must be greater than zero.");
            if (gapDuration < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(gapDuration), "gapDuration cannot be negative.");
            if (End <= Start)
                yield break;

            var currentStart = Start;
            var finalEnd = End;

            while (currentStart < finalEnd)
            {
                var proposedEnd = currentStart + segmentDuration;
                var currentEnd = proposedEnd <= finalEnd ? proposedEnd : finalEnd;

                yield return new DateTimeSpan(currentStart, currentEnd);

                if (currentEnd >= finalEnd)
                    yield break;

                // advance by the requested gap (can be zero for back-to-back segments)
                currentStart = currentEnd + gapDuration;
            }
        }

        public bool IsUniversalTime() => Start.Kind == DateTimeKind.Utc && End.Kind == DateTimeKind.Utc;

        /// <summary> Same moment in time? </summary>
        public bool TimeEquals(in DateTimeSpan other) => Start.Ticks == other.Start.Ticks && End.Ticks == other.End.Ticks;
        
        /// <summary> Same moment in time and same kind. </summary>
        public bool ValueEquals(in DateTimeSpan other) => TimeEquals(other) && Start.Kind == other.Start.Kind && End.Kind == other.End.Kind;

        public bool Equals(DateTimeSpan other) => ValueEquals(other);
        public override bool Equals(object obj) => obj is DateTimeSpan other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Start, End);

        public static bool operator ==(DateTimeSpan left, DateTimeSpan right) => left.Equals(right);
        public static bool operator !=(DateTimeSpan left, DateTimeSpan right) => !left.Equals(right);
    }
}