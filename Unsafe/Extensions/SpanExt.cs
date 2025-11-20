using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

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
        
        /// <summary> Efficiently compute SHA512 hash from a span of unmanaged types. Unmanaged allows zero allocations. </summary>
        /// <param name="span">Span of unmanaged types to hash.</param>
        /// <param name="hash">Output hash as a base64 string.</param>
        /// <returns>True if hash was computed successfully, false otherwise.</returns>
        public static bool TryComputeSHA512Hash<T>(this Span<T> span, out string hash)
            where T : unmanaged
        {
            if (span.IsEmpty)
            {
                hash = string.Empty;
                return false;
            }

            using var sha512 = SHA512.Create();
            var rawBytes = MemoryMarshal.AsBytes(span);
            Span<byte> hashSpan = stackalloc byte[sha512.HashSize / 8];
            if (!sha512.TryComputeHash(rawBytes, hashSpan, out _))
            {
                hash = string.Empty;
                return false;
            }

            hash = Convert.ToBase64String(hashSpan);
            return true;
        }
    }
}