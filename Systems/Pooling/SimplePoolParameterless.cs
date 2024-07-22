﻿//using System.Runtime.Remoting.Messaging;

using UnityEngine;

namespace VLib
{
    /// <summary> Automatic implementation for anything that can be created with a parameter-less constructor </summary>
    public class SimplePoolParameterless<T> : SimplePool<T>, IPooledItemCreator<T>
        where T : new()
    {
        public SimplePoolParameterless() { }
        
        public SimplePoolParameterless(int initPoolCapacity) : base(initPoolCapacity) { }

        public virtual T CreateNewItem()
        {
            IncrementTakenCount();
            return new T();
        }

        protected override bool TryTakeFromCollection(int suggestedIndex, out T poolable)
        {
            if (base.TryTakeFromCollection(suggestedIndex, out poolable))
                return true;
            
            poolable = CreateNewItem();
            return true;
        }
    }
}