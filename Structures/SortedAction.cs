using System;
using System.Collections.Generic;
using UnityEngine;

namespace VLib
{
    public class SortedAction : IComparable<SortedAction>
    {
        public static implicit operator SortedAction(Action action) => new SortedAction(action, 0);
        public static implicit operator Action(SortedAction action) => action.Action;
        
        /// <summary> Higher orders are executed later </summary>
        public int Order { get; private set; }
        public Action Action { get; private set; }

        public SortedAction(Action action, int order)
        {
            Action = action;
            Order = order;
        }

        public int CompareTo(SortedAction other) => Order.CompareTo(other.Order);

        public void Invoke()
        {
            try
            {
                Action?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError("Error invoking action, logging next.");
                Debug.LogException(e);
            }
        }
    }

    public static class SortedActionExt
    {
        public static void InvokeAll(this IReadOnlyList<SortedAction> actions)
        {
            for (int i = 0; i < actions.Count; i++)
                actions[i].Invoke();
        }
    }
}