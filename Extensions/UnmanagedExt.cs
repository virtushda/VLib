namespace VLib
{
    public static class UnmanagedExt
    {
        public static unsafe float ReadFromAsFloat<T>(this ref T value, int index)
            where T : unmanaged
        {
            return value.ReadFromValueTypeAs<T, float>(index);
        }

        public static unsafe void WriteToAsFloat<T>(this ref T value, int index, float newFloatValue)
            where T : unmanaged
        {
            value.WriteToValueTypeAs(index, newFloatValue);
        }
        
        public static unsafe TReadType ReadFromValueTypeAs<TData, TReadType>(this ref TData value, int index)
            where TData : unmanaged 
            where TReadType : unmanaged
        {
            fixed (TData* array = &value) { return ((TReadType*)array)[index]; }
        }

        public static unsafe void WriteToValueTypeAs<TData, TWriteType>(this ref TData value, int index, TWriteType newValue)
            where TData : unmanaged
            where TWriteType : unmanaged
        {
            fixed (TData* array = &value) { ((TWriteType*)array)[index] = newValue; }
        }
    }
}