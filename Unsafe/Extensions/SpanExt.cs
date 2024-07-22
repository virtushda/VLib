using System;
using System.Collections.Generic;

namespace VLib
{
    public static class SpanExt
    {
        // Equality comparison between a Span<T> and an IList<T>
        public static bool SequenceEqual<T>(in this Span<T> span, IList<T> list)
            where T : IEquatable<T>
        {
            if (span.IsEmpty || list == null || span.Length != list.Count)
                return false;

            for (int i = 0; i < span.Length; i++)
            {
                if (!span[i].Equals(list[i]))
                    return false;
            }

            return true;
        }
    }
}