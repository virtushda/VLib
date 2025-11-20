namespace VLib
{
    public class Container<T>
    {
        public T Object { get; set; }
        
        public Container() { }
        
        public Container(T obj) => Object = obj;
    }
}