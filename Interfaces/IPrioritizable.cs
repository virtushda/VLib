using System;
using UnityEngine;

namespace VLib
{
    public interface IPrioritizable : IComparable<IPrioritizable>, IEquatable<IPrioritizable>
    {
        int Priority { get; }

        new bool Equals(IPrioritizable other) => this.DefaultEquals(other);

        new int CompareTo(IPrioritizable other) => this.DefaultCompareTo(other);
    }
    
    public static class IPrioritizableExt
    {
        public static int DefaultCompareTo(this IPrioritizable prioritizable, IPrioritizable other) => prioritizable.Priority.CompareTo(other.Priority);

        public static bool DefaultEquals(this IPrioritizable prioritizable, IPrioritizable other) => 
            prioritizable.Priority.Equals(other.Priority) && ReferenceEquals(prioritizable, other);
    }

    public class PrioritizableObject : IPrioritizable
    {
        public virtual int Priority { get; protected set; } = 0;
        
        public int CompareTo(IPrioritizable other) => this.DefaultCompareTo(other);

        public virtual bool Equals(IPrioritizable other) => this.DefaultEquals(other);
        
        protected bool Equals(MonoBehaviourPrioritizable other)
        {
            return base.Equals(other) && Priority == other.Priority;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((MonoBehaviourPrioritizable)obj);
        }
    }

    public class MonoBehaviourPrioritizable : MonoBehaviour, IPrioritizable
    {
        public virtual int Priority { get; protected set; } = 0;
        
        public int CompareTo(IPrioritizable other) => this.DefaultCompareTo(other);

        public virtual bool Equals(IPrioritizable other) => this.DefaultEquals(other);
        
        protected bool Equals(MonoBehaviourPrioritizable other)
        {
            return base.Equals(other) && Priority == other.Priority;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((MonoBehaviourPrioritizable)obj);
        }
    }
}