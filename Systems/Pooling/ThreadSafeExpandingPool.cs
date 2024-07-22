using System;

namespace VLib
{
    ///<summary> A thread safe auto-expanding pool that takes a custom generation function. </summary>
    public class ThreadSafeExpandingPool<T> : ThreadSafePoolBase<T>
    {
        Func<T> CreateNewItemDelegate { get; set; }
        
        public ThreadSafeExpandingPool(Func<T> createNewItemDelegate)
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