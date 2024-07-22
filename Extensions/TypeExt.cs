using System;

namespace VLib
{
    public static class TypeExt
    {
        public static object GetDefaultValue<T>(this T t)
            where T : Type
        {
            if (t.IsValueType)
                return Activator.CreateInstance(t);

            return null;
        }
    }
}