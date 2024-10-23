using System;

namespace VLib
{
    public class SortedAction : IComparable<SortedAction>
    {
        /// <summary> Higher orders are executed later </summary>
        public int Order { get; private set; }
        public Action Action { get; private set; }

        public SortedAction(Action action, int order)
        {
            Action = action;
            Order = order;
        }

        public int CompareTo(SortedAction other) => Order.CompareTo(other.Order);

        public void Invoke() => Action?.Invoke();
    }
}