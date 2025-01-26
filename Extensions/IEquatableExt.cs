using System;
using System.Collections.Generic;

namespace VLib
{
    public static class IEquatableExt
    {
        /// <summary> Changes the value if it is not equal to the new value. </summary>
        /// <typeparam name="T">The type of the value, which must implement <see cref="IEquatable{T}"/>.</typeparam>
        /// <param name="value">The reference to the value to be changed.</param>
        /// <param name="newValue">The new value to set.</param>
        /// <returns>True if the value was changed; otherwise, false.</returns>
        public static bool Change<T>(ref this T value, T newValue) 
            where T : struct, IEquatable<T>
        {
            if (value.Equals(newValue))
                return false;
            value = newValue;
            return true;
        }
        
        /// <summary> Changes the value if it is not equal to the new value. </summary>
        /// <typeparam name="T">The type of the value, which must implement <see cref="IEquatable{T}"/>.</typeparam>
        /// <param name="value">The reference to the value to be changed.</param>
        /// <param name="newValue">The new value to set.</param>
        /// <param name="previousValue">The previous value before the change.</param>
        /// <returns>True if the value was changed; otherwise, false.</returns>
        public static bool Change<T>(ref this T value, T newValue, out T previousValue) 
            where T : struct, IEquatable<T>
        {
            previousValue = value;
            return value.Change(newValue);
        }

        /// <summary> Changes the value if it is not equal to the new value, using a specified equality comparer. </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer, which must implement <see cref="IEqualityComparer{T}"/>.</typeparam>
        /// <param name="value">The reference to the value to be changed.</param>
        /// <param name="newValue">The new value to set.</param>
        /// <param name="equalityComparer">The equality comparer to use for comparison.</param>
        /// <returns>True if the value was changed; otherwise, false.</returns>
        public static bool CompareChange<T, TEqualityComparer>(ref this T value, T newValue, TEqualityComparer equalityComparer)
            where T : struct
            where TEqualityComparer : struct, IEqualityComparer<T>
        {
            if (equalityComparer.Equals(value, newValue))
                return false;
            value = newValue;
            return true;
        }

        /// <summary> Changes the value if it is not equal to the new value, using a specified equality comparer. </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <typeparam name="TEqualityComparer">The type of the equality comparer, which must implement <see cref="IEqualityComparer{T}"/>.</typeparam>
        /// <param name="value">The reference to the value to be changed.</param>
        /// <param name="newValue">The new value to set.</param>
        /// <param name="equalityComparer">The equality comparer to use for comparison.</param>
        /// <param name="previousValue">The previous value before the change.</param>
        /// <returns>True if the value was changed; otherwise, false.</returns>
        public static bool CompareChange<T, TEqualityComparer>(ref this T value, T newValue, TEqualityComparer equalityComparer, out T previousValue)
            where T : struct
            where TEqualityComparer : struct, IEqualityComparer<T>
        {
            previousValue = value;
            return value.CompareChange(newValue, equalityComparer);
        }
    }
}