using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace VLib
{
    /// <summary> Assertions that are callable from inside burst. </summary>
    public static class BurstAssert
    {
        /// <summary> Automatically reports the caller line number and name. (As separate messages) </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void True(bool condition, [CallerLineNumber] int callerLineNumber = default, [CallerMemberName] string callerMemberName = default)
        {
            if (!condition)
            {
                UnityEngine.Debug.LogError($"BurstAssert.True, received false. Line: {callerLineNumber} (Reporting calling member in next line due to burst restrictions)");
                UnityEngine.Debug.LogError(callerMemberName);
            }
        }
        
        /// <summary> Automatically reports the caller line number and name. (As separate messages) </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void False(bool condition, [CallerLineNumber] int callerLineNumber = default, [CallerMemberName] string callerMemberName = default)
        {
            if (condition)
            {
                UnityEngine.Debug.LogError($"BurstAssert.False, received true. Line: {callerLineNumber} (Reporting calling member in next line due to burst restrictions)");
                UnityEngine.Debug.LogError(callerMemberName);
            }
        }

        /// <summary> Cheaper version of true, but provides less debugging data. </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void TrueCheap(bool condition)
        {
            if (!condition)
                UnityEngine.Debug.LogError("BurstAssert.True, received false");
        }
        
        /// <summary> Cheaper version of false, but provides less debugging data. </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void FalseCheap(bool condition)
        {
            if (condition)
                UnityEngine.Debug.LogError("BurstAssert.False, received true");
        }
    }
}