namespace VLib
{
    public class NullableObj
    {
        public static implicit operator bool(NullableObj n) => n != null;
    }
}