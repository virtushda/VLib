using System;
using System.Globalization;

namespace VLib
{
    public static class DateTimeExt
    {
        public static readonly DateTime MinValueUTC = new DateTime(0, DateTimeKind.Utc);
        public static readonly DateTime MaxValueUTC = new DateTime(DateTime.MaxValue.Ticks, DateTimeKind.Utc);
        
        public static DateOnly AsDateOnly(this DateTime dateTime) => new DateOnly(dateTime.Year, dateTime.Month, dateTime.Day);
        public static TimeOnly AsTimeOnly(this DateTime dateTime) => new TimeOnly(dateTime.Hour, dateTime.Minute, dateTime.Second);
        
        /// <summary> Converts a DateTime to RFC 3339 format. </summary>
        public static string ToRfc3339(in this DateTime dateTime)
        {
            var asUTC = dateTime.ToUniversalTime();
            return asUTC.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture);
        }
        
        /// <summary> Converts a RFC 3339 string to a DateTime. </summary>
        public static DateTime FromRfc3339(this string rfc3339)
        {
            return DateTime.Parse(rfc3339, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
        }
        
        public static int MinutesIntoCurrentDay(this DateTime dateTime) => dateTime.Hour * 60 + dateTime.Minute;

        public static void CheckIsUTC(this DateTime dateTime)
        {
            if (dateTime.Kind != DateTimeKind.Utc)
                throw new ArgumentException("DateTime must be UTC.", nameof(dateTime));
        }

        public static void CheckIsLocal(this DateTime dateTime)
        {
            if (dateTime.Kind != DateTimeKind.Local)
                throw new ArgumentException("DateTime must be local.", nameof(dateTime));
        }
    }
}