namespace VLib
{
    public static class NullableExt
    {
        public static bool TryGetValueOrDefault<T>(this T? item, out T value, T defaultValue = default) 
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
    }
}