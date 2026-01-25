using System.Runtime.CompilerServices;

namespace VLib
{
    public static class UnityObjectExt
    {
        /// <summary>
        /// Checks if an object is null, properly handling UnityEngine.Object instances that may have been destroyed.
        /// Unlike standard null checks, this correctly detects Unity objects that have been destroyed but still have a managed wrapper.
        /// </summary>
        /// <typeparam name="T">The type of object to check. Must be a reference type.</typeparam>
        /// <param name="obj">The object instance to check for null.</param>
        /// <returns>
        /// <c>true</c> if the object is null or is a destroyed UnityEngine.Object; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// Unity overrides the == operator for UnityEngine.Object to handle destroyed objects, but this behavior
        /// is lost when objects are cast to interfaces or System.Object. This method ensures correct null checking
        /// in all scenarios by detecting UnityEngine.Object instances at runtime and using Unity's custom equality operator.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Works correctly even if the component has been destroyed
        /// IMyInterface component = GetComponent&lt;MyComponent&gt;();
        /// if (component.IsNullAuto())
        /// {
        ///     Debug.Log("Component is null or destroyed");
        /// }
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullAuto<T>(this T obj)
            where T : class
        {
            if (obj is UnityEngine.Object unityObj)
                return unityObj == null;
            return obj == null;
        }
    }
}