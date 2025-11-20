using System;

namespace VLib
{
    public static class DateTimeOffsetExt
    {
        /// <summary> Returns a new DateTimeOffset with the time set to 00:00:00. </summary>
        public static DateTimeOffset AsDate(this DateTimeOffset dateTimeOffset)
        {
            return new DateTimeOffset(dateTimeOffset.Year, dateTimeOffset.Month, dateTimeOffset.Day, 0, 0, 0, dateTimeOffset.Offset);
        }
        
        /// <summary> Returns a new DateOnly with the date set to the DateTimeOffset's date. </summary>
        public static DateOnly AsDateOnly(this DateTimeOffset dateTimeOffset) => new DateOnly(dateTimeOffset.Year, dateTimeOffset.Month, dateTimeOffset.Day);
    }
}