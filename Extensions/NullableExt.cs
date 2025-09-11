using System;

namespace VLib
{
    public static class NullableExt
    {
        public static bool TryGetValueOrDefault<T>(this T? item, out T value, in T defaultValue = default) 
            where T : struct
        {
            if (item.HasValue)
            {
                value = item.Value;
                return true;
            }
            value = defaultValue;
            return false;
        }
        
        public static bool TryDisposeToDefault<T>(ref this T? item, T defaultValue = default) 
            where T : struct, IDisposable
        {
            if (item.TryGetValueOrDefault(out var value))
            {
                value.Dispose();
                item = null;
                return true;
            }
            return false;
        }
    }
}