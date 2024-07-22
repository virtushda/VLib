namespace VLib
{
    public interface IArrayParam<T> : IParam<T> { }

    public static class IArrayParamMethods
    {
        public static T Array<T>(this IArrayParam<T> arrayParam)
        {
            return arrayParam.Value;
        }
    } 
}