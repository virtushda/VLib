using System;
using System.Collections.Generic;

namespace VLib
{
    public class EventContainer
    {
        static readonly ConcurrentVPoolParameterless<List<Action>> ListPool = new();
        
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
            ListPool.Repool(InvocationList);
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

        void EnsureList() => InvocationList ??= ListPool.Depool();
    }
    
    public class EventContainer<TArgs>
    {
        static readonly ConcurrentVPoolParameterless<List<(object, Action<object, TArgs>)>> ListPool = new();
        
        /*public static EventContainer<TArgs> operator +(EventContainer<TArgs> eventContainer, Action<TArgs> action)
        {
            eventContainer.EnsureList();
            eventContainer.InvocationList.Add(action);
            return eventContainer;
        }
        
        public static EventContainer<TArgs> operator -(EventContainer<TArgs> eventContainer, Action<TArgs> action)
        {
            eventContainer.EnsureList();
            eventContainer.InvocationList.Remove(action);
            return eventContainer;
        }*/
        
        public void Subscribe(object context, Action<object, TArgs> action)
        {
            EnsureList();
            InvocationList.Add((context, action));
        }
        
        public void Unsubscribe(object context, Action<object, TArgs> action = null)
        {
            EnsureList();
            for (int i = InvocationList.Count - 1; i >= 0; i--)
            {
                var invocation = InvocationList[i];
                if(!context.Equals(invocation.context))
                    continue;
                if(action != null && !action.Equals(invocation.action))
                    continue;
                
                InvocationList.RemoveAt(i);
            }
        }
        
        public List<(object context, Action<object, TArgs> action)> InvocationList;

        public void Invoke(TArgs args)
        {
            if (InvocationList == null)
                return;
            
            foreach (var invocation in InvocationList)
            {
                invocation.action?.Invoke(invocation.context, args);
            }
        }

        public bool Clear()
        {
            if (InvocationList == null)
                return false;
            
            InvocationList.Clear();
            ListPool.Repool(InvocationList);
            InvocationList = null;
            return true;
        }

        public bool CopyTo(EventContainer<TArgs> other)
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
        public bool CopyFrom(EventContainer<TArgs> other)
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

        void EnsureList() => InvocationList ??= ListPool.Depool();
    }
}