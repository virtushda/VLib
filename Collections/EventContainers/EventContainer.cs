using System;
using System.Collections.Generic;

namespace VLib
{
    public class EventContainer
    {
        //private static readonly SimpleListPool<Action> listPool = new(8, 4);
        private static readonly ThreadSafePoolParameterless<List<Action>> listPool = new();
        
        public static EventContainer operator +(EventContainer eventContainer, Action action)
        {
            eventContainer.EnsureList();
            eventContainer.InvocationList.Add(action);
            return eventContainer;
        }
        
        public static EventContainer operator -(EventContainer eventContainer, Action action)
        {
            eventContainer.EnsureList();
            eventContainer.InvocationList.Remove(action);
            return eventContainer;
        }
        
        public List<Action> InvocationList;

        public void Invoke()
        {
            if (InvocationList == null)
                return;
            
            foreach (var action in InvocationList)
            {
                action?.Invoke();
            }
        }

        public bool Clear()
        {
            if (InvocationList == null)
                return false;
            
            InvocationList.Clear();
            listPool.Repool(InvocationList);
            InvocationList = null;
            return true;
        }

        public bool CopyTo(EventContainer other)
        {
            if (InvocationList == null)
                return false;
            
            other.EnsureList();
            other.InvocationList.Clear();
            foreach (var action in InvocationList)
            {
                other.InvocationList.Add(action);
            }
            return true;
        }
        public bool CopyFrom(EventContainer other)
        {
            if (other.InvocationList == null)
                return false;
            
            EnsureList();
            InvocationList.Clear();
            foreach (var action in other.InvocationList)
            {
                InvocationList.Add(action);
            }
            return true;
        }

        private void EnsureList() => InvocationList ??= listPool.Fetch();
    }
    
    public class EventContainer<T>
    {
        private static readonly ThreadSafePoolParameterless<List<Action<T>>> listPool = new();
        
        public static EventContainer<T> operator +(EventContainer<T> eventContainer, Action<T> action)
        {
            eventContainer.EnsureList();
            eventContainer.InvocationList.Add(action);
            return eventContainer;
        }
        
        public static EventContainer<T> operator -(EventContainer<T> eventContainer, Action<T> action)
        {
            eventContainer.EnsureList();
            eventContainer.InvocationList.Remove(action);
            return eventContainer;
        }
        
        public List<Action<T>> InvocationList;

        public void Invoke(T arg)
        {
            if (InvocationList == null)
                return;
            
            foreach (var action in InvocationList)
            {
                action?.Invoke(arg);
            }
        }

        public bool Clear()
        {
            if (InvocationList == null)
                return false;
            
            InvocationList.Clear();
            listPool.Repool(InvocationList);
            InvocationList = null;
            return true;
        }

        public bool CopyTo(EventContainer<T> other)
        {
            if (InvocationList == null)
                return false;

            other.EnsureList();
            other.InvocationList.Clear();
            foreach (var action in InvocationList)
            {
                other.InvocationList.Add(action);
            }
            return true;
        }
        public bool CopyFrom(EventContainer<T> other)
        {
            if (other.InvocationList == null)
                return false;

            EnsureList();
            InvocationList.Clear();
            foreach (var action in other.InvocationList)
            {
                InvocationList.Add(action);
            }
            return true;
        }

        private void EnsureList() => InvocationList ??= listPool.Fetch();
    }
}