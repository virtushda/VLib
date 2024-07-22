namespace VLib
{
    public class Accessible<T> where T : IAccessible
    {
        private T value;

        public bool IsAccessible => ValidateAccessible();
        public T Value => ValidateAccessible() ? value : default;

        public Accessible(T newValue)
        {
            value = newValue;
        }

        public bool ValidateAccessible()
        {
            if (value == null) return false;
            if (value.IsAccessible) return true;
            value = default;
            return false;
        }
    }

    public interface IAccessible
    {
        bool IsAccessible { get; }
    }
}