using System;
using System.Collections.Generic;

namespace VLib
{
    /// <summary> Keyed quasi-event system based on a dictionary. </summary> 
    public class KeyedEventLists<TKey>
    {
        Dictionary<TKey, List<Action>> keyedActionLists = new();

        public Dictionary<TKey, List<Action>> KeyedActionLists
        {
            get => keyedActionLists;
            set => keyedActionLists = value;
        }

        /// <summary> WARNING: Can add duplicate values. </summary> 
        public void Add(TKey key, Action eventAction)
        {
            if (!keyedActionLists.TryGetValue(key, out var actions))
            {
                // Populate new list if needed
                actions = new List<Action>();
                keyedActionLists.Add(key, actions);
            }
            
            actions.Add(eventAction);
        }

        /// <summary> Guarded call, safe if key is not present. </summary> 
        public void Remove(TKey key, Action eventAction)
        {
            if (keyedActionLists.TryGetValue(key, out var actions))
            {
                actions.Remove(eventAction);
            }
        }

        /// <summary> Guarded call, safe if key is not present. </summary> 
        public void RemoveKey(TKey key) => keyedActionLists.Remove(key);

        public void ClearFor(TKey key)
        {
            if (keyedActionLists.TryGetValue(key, out var actions))
                actions.Clear();
        }

        public void ClearAll() => keyedActionLists.Clear();

        /// <summary> Guarded call, safe if key is not present. </summary> 
        public void Invoke(TKey key)
        {
            if (!keyedActionLists.TryGetValue(key, out var invokationList))
                return;
            foreach (var action in invokationList)
            {
                action.Invoke();
            }
        }

        /// <summary> Guarded call, safe if key is not present. </summary> 
        public void InvokeAndClear(TKey key)
        {
            Invoke(key);
            ClearFor(key);
        }
    }
}