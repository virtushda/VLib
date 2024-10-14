namespace VLib.Structures
{
    public struct Dirtyable<T>
    {
        /// <summary> This is exposed to allow for ref behaviour. Avoid setting this directly unless you know what you are doing. Use <see cref="Value"/> by default. </summary>
        public T internalValue;
        bool dirty;
        
        public bool IsDirty => dirty;
        public void Dirty() => dirty = true;
        public void Clean() => dirty = false;
        
        public T Value
        {
            get => internalValue;
            set
            {
                this.internalValue = value;
                dirty = true;
            }
        }
    }
}