using System;
using System.Collections.Generic;
using System.Diagnostics;
using Libraries.KeyedAccessors.Lightweight;
using Unity.Mathematics;

namespace VLib.Systems
{
    /// <summary> Allocates a time budget between various 'Apportionees' </summary>
    public class TimeApportioner<T>
        where T : ITimeApportionee, IEquatable<T>
    {
        public float MillisecondBudget { get; set; }
        float msBudgetPerApportionee;
        public float MSBudgetPerApportionee => msBudgetPerApportionee;
        
        KeyedList<T> apportionees;
        public bool UseCustomCollection { get; private set; }
        IList<T> CustomApportioneeList { get; set; }
        
        IList<T> Apportionees => CustomApportioneeList ?? apportionees.keys;

        /// <summary> Initializes with an internal collection that must be managed through methods on this object. </summary>
        public TimeApportioner(float millisecondBudget)
        {
            apportionees = new KeyedList<T>();
            UseCustomCollection = false;
            MillisecondBudget = millisecondBudget;
            RecalculateBudgets();
        }

        /// <summary> Initializes with a custom collection provided that can be externally managed. <see cref="RecalculateBudgets"/> </summary>
        public TimeApportioner(IList<T> customList, float millisecondBudget)
        {
            CustomApportioneeList = customList;
            UseCustomCollection = true;
            MillisecondBudget = millisecondBudget;
            RecalculateBudgets();
        }
        
        public void AddApportionee(T apportionee)
        {
            if (UseCustomCollection)
                CustomApportioneeList.Add(apportionee);
            else
                apportionees.Add(apportionee, out _);
            
            RecalculateBudgets();
        }

        public void RemoveApportionee(T apportionee)
        {
            if (UseCustomCollection)
                CustomApportioneeList.Remove(apportionee);
            else
                apportionees.Remove(apportionee);
            RecalculateBudgets();
        }

        public void UpdateAll()
        {
            foreach (var apportionee in Apportionees)
                apportionee.OnApportionedUpdate(msBudgetPerApportionee);
        }

        public void RecalculateBudgets() => msBudgetPerApportionee = MillisecondBudget / math.max(Apportionees.Count, 1);
        
        [Conditional("UNITY_EDITOR")]
        void AssertListIsNotCustom()
        {
            if (UseCustomCollection)
                throw new System.Exception("Cannot add or remove apportionees from a custom list.");
        }
    }
}