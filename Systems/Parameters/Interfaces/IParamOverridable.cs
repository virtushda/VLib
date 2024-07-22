namespace VLib
{
    public interface ICountable
    {
        int Count { get; set; }
    }

    public interface IParamOverridable<T> : IParam<T>/*, ICountable*/
    {
        public bool Overriden { get; set; }
        public T OverridenValue { get; set; }
    }

    public static class IParamOverridableMethods
    {
        public static T GetCurrentValue<T>(this IParamOverridable<T> overridable)
        {
            return overridable.Overriden ? overridable.OverridenValue : overridable.Value;
        }

        public static void SetOverride<T>(this IParamOverridable<T> overridable, T overrideValue)
        {
            overridable.OverridenValue = overrideValue;
            overridable.Overriden = true;
        }

        /*public static void SetOverride<T>(this IParamOverridable<T> overridable, T overrideValue, int trueCount)
            where T : ICollection
        {
            overridable.OverridenValue = overrideValue;
            overridable.Overriden = true;
            overridable.Count = trueCount;
        }*/

        public static void ClearOverride<T>(this IParamOverridable<T> overridable)
        {
            overridable.Overriden = false;
            //overridable.Count = 0;
        }
    } 
}