using System.Collections;

namespace VLib
{
    public interface IParam<T>
    {
        T Value { get; set; }
    }

    public static class IParamExt
    {
        public static T Array<T>(this IParam<T> param)
            where T : ICollection
        {
            return param.Value;
        }
    } 
}