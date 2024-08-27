using System;

namespace VLib.Systems
{
    public interface ITimeApportionee : IEquatable<ITimeApportionee>
    {
        void OnApportionedUpdate(float millisecondBudget);
    }
}