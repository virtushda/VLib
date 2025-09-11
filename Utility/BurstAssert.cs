using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace VLib
{
    /// <summary> Assertions that are callable from inside burst. </summary>
    public static class BurstAssert
    {
        /// <summary> Automatically reports the caller line number and name. (As separate messages) </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void TrueVerbose(bool condition, int errorCode = 0, [CallerLineNumber] int callerLineNumber = default, [CallerMemberName] string callerMemberName = default)
        {
            if (!condition)
            {
                UnityEngine.Debug.LogError($"BurstAssert.True, received false. Error code: {errorCode}. Line: {callerLineNumber} (Reporting calling member in next line due to burst restrictions)");
                UnityEngine.Debug.LogError(callerMemberName);
            }
        }
        
        /// <summary> Automatically reports the caller line number and name. (As separate messages) </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void FalseVerbose(bool condition, int errorCode = 0, [CallerLineNumber] int callerLineNumber = default, [CallerMemberName] string callerMemberName = default)
        {
            if (condition)
            {
                UnityEngine.Debug.LogError($"BurstAssert.False, received true. Error code: {errorCode}. Line: {callerLineNumber} (Reporting calling member in next line due to burst restrictions)");
                UnityEngine.Debug.LogError(callerMemberName);
            }
        }

        /// <summary> Cheaper version of true, but provides less debugging data. </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void True(bool condition, int errorCode = 0)
        {
            if (condition)
                return;
            if (errorCode != 0)
                UnityEngine.Debug.LogError($"BurstAssert.True, received false. Error code: {errorCode}");
            else 
                UnityEngine.Debug.LogError("BurstAssert.True, received false");
        }

        /// <summary> <inheritdoc cref="True"/> </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TrueThrowing(bool condition, int errorCode = 0)
        {
            if (condition)
                return;
            if (errorCode != 0)
                throw new System.Exception($"BurstAssert.TrueThrowing, received false. Error code: {errorCode}");
            else
                throw new System.Exception("BurstAssert.TrueThrowing, received false");
        }
        
        /// <summary> Cheaper version of false, but provides less debugging data. </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void False(bool condition, int errorCode = 0)
        {
            if (!condition)
                return;
            if (errorCode != 0)
                UnityEngine.Debug.LogError($"BurstAssert.False, received true. Error code: {errorCode}");
            else
                UnityEngine.Debug.LogError("BurstAssert.False, received true");
        }
        
        /// <summary> <inheritdoc cref="False"/> </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FalseThrowing(bool condition, int errorCode = 0)
        {
            if (!condition)
                return;
            if (errorCode != 0)
                throw new System.Exception($"BurstAssert.FalseThrowing, received true. Error code: {errorCode}");
            throw new System.Exception("BurstAssert.FalseThrowing, received true");
        }
        
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ApproxEquals(float value, float target, float threshold = .001f)
        {
            if (math.abs(value - target) > threshold)
                UnityEngine.Debug.LogError($"BurstAssert.ValueApprox: {value} != {target}, off by {math.abs(value - target)}");
        }
        
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ValueLessThan(int value, int threshold)
        {
            if (value >= threshold)
                UnityEngine.Debug.LogError($"BurstAssert.ValueLessThan: {value} >= {threshold}");
        }
        
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ValueGreaterThan(int value, int threshold)
        {
            if (value <= threshold)
                UnityEngine.Debug.LogError($"BurstAssert.ValueGreaterThan: {value} <= {threshold}");
        }
        
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ValueLessOrEqualTo(int value, int threshold)
        {
            if (value > threshold)
                UnityEngine.Debug.LogError($"BurstAssert.ValueLessOrEqualTo: {value} > {threshold}");
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ValueGreaterOrEqualTo(int value, int threshold)
        {
            if (value < threshold)
                UnityEngine.Debug.LogError($"BurstAssert.ValueGreaterOrEqualTo: {value} < {threshold}");
        }
    }
}