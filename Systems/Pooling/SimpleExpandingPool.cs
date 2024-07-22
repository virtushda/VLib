using System;

namespace VLib
{
    ///<summary> A pool that supports a custom item creation action to auto-expand the pool if it runs out of items. </summary>
    public class SimpleExpandingPool<T> : SimplePool<T>
    {
        public Func<T> CreateNewItemDelegate { get; set; }
        
        public SimpleExpandingPool(int initPoolCapacity, Func<T> createNewItemDelegate) : base(initPoolCapacity)
        {
            CreateNewItemDelegate = createNewItemDelegate;
        }

        protected override bool TryTakeFromCollection(int suggestedIndex, out T poolable)
        {
            if (base.TryTakeFromCollection(suggestedIndex, out poolable))
                return true;
            poolable = CreateNewItemDelegate();
            IncrementTakenCount();
            return true;
        }
    }
}