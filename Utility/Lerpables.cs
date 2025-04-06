using Unity.Collections;
using Unity.Mathematics;

namespace VLib
{
    public interface ILerpable<T>
    {
        public T Value { get; set; }
        public T Lerp(T valueA, T valueB, float lerpValue);
        /// <summary> Returns Value clamped to a range. </summary>
        public T Clamp(T min, T max);
    }

    public static class LerpableExt
    {
        /*/// <summary> Computes a lerp value using the 'lerpable' as valueA, but does not alter the 'lerpable' value itself. </summary>
        public static TU LerpToward<T, TU>(this T lerpable, TU lerpTowardValue, float lerpValue)
            where T : ILerpable<TU>
        {
            return lerpable.Lerp(lerpable.Value, lerpTowardValue, lerpValue);
        }

        /// <summary> Computes a lerp value using the 'lerpable' as valueA, but does not alter the 'lerpable' value itself. </summary>
        public static TU LerpToward<T, TU>(this T lerpableA, T lerpableB, float lerpValue)
            where T : ILerpable<TU>
        {
            return lerpableA.Lerp(lerpableA.Value, lerpableB.Value, lerpValue);
        }

        /// <summary> Computes a lerp value using the 'lerpable' as valueA, modifying the lerpable </summary>
        public static TU LerpTowardValue<T, TU>(ref this T lerpableA, T lerpableB, float lerpValue)
            where T : struct, ILerpable<TU>
        {
            return lerpableA.Lerp(lerpableA.Value, lerpableB.Value, lerpValue);
        }

        /// <summary> Computes a new lerpable value using the 'lerpable' as valueA, but does not alter the 'lerpable' value itself. </summary>
        public static T LerpTowardLerpable<T, TU>(this T lerpable, TU lerpTowardValue, float lerpValue)
            where T : ILerpable<TU>, new()
        {
            var newLerpable = new T();
            newLerpable.Value = lerpable.Lerp(lerpable.Value, lerpTowardValue, lerpValue);
            return newLerpable;
        }*/

        /// <summary> Saturate extension method that uses the clamp method </summary>
        public static T Saturate<T>(this T lerpable)
            where T : ILerpable<T>
        {
            return lerpable.Clamp(lerpable.Value, lerpable.Value);
        }
    }

    /// <summary> Lerpable that lerps by component </summary>
    public interface IComponentLerpable<T> : ILerpable<T>
    {
        public T ComponentLerp(T valueA, T valueB, T lerpValue);
    }

    public interface ILerpableVector
    {
        public float4 VectorValues { get; set; }
    }

    public interface ISmoothLerpable<T> : ILerpable<T>
    {
        public T TargetValue { get; set; }
        
        public float SmoothRate { get; set; }

        public T DiffToTarget { get; }
    }
    
    public interface ISmoothComponentLerpable<T> : IComponentLerpable<T>
    {
        public T TargetValue { get; set; }
        
        public T ComponentSmoothRate { get; set; }

        public T ComputeStepSize(float stepSize);
    }

    public static class LerpableVectorExt
    {
        public static float4 AutoNormalize<T>(this T lerpable)
            where T : ILerpableVector
        {
            return lerpable.VectorValues = math.normalize(lerpable.VectorValues);
        }
    }

    public static class SmoothLerpableExt
    {
        // Extension to smoothly lerp a smooth lerpable towards its target value (single component lerp)
        public static T LerpToTarget<T, TU>(this ref T smoothLerpable, float lerpValue, out TU result)
            where T : struct, ILerpableVector, ISmoothLerpable<TU>
            where TU : unmanaged
        {
             smoothLerpable.Value = result = smoothLerpable.Lerp(smoothLerpable.Value, smoothLerpable.TargetValue, math.saturate(lerpValue));
             return smoothLerpable;
        }
        
        // Extension to smoothly lerp a smooth lerpable towards its target value (single component lerp)
        public static T ComponentLerpToTarget<T, TLerp>(this ref T smoothLerpable, TLerp lerpValue, out TLerp result)
            where T : struct, ILerpableVector, ISmoothComponentLerpable<TLerp>
            where TLerp : ILerpable<TLerp>
        {
             smoothLerpable.Value = result = smoothLerpable.ComponentLerp(smoothLerpable.Value, smoothLerpable.TargetValue, lerpValue.Saturate());
             return smoothLerpable;
        }
        
        public static T MoveTowardsTarget<T, TU>(this ref T smoothLerpable, float stepSize, out TU result)
            where T : struct, ILerpableVector, ISmoothComponentLerpable<TU>
            where TU : unmanaged, ILerpable<TU>
        {
            var lerpT = smoothLerpable.ComputeStepSize(stepSize);
            return ComponentLerpToTarget(ref smoothLerpable, lerpT, out result);
        }
    }

    public static class SmoothComponentLerpableExt
    {
        // Extension to smoothly lerp a smooth lerpable towards its target value (component lerp)
        public static T ComponentLerpToTargetUnclamped<T, TU>(this T smoothLerpable, TU lerpValue, out TU result)
            where T : struct, ILerpableVector, ISmoothComponentLerpable<TU>
            where TU : unmanaged
        {
             smoothLerpable.Value = result = smoothLerpable.ComponentLerp(smoothLerpable.Value, smoothLerpable.TargetValue, lerpValue);
             return smoothLerpable;
        }
    }

    [GenerateTestsForBurstCompatibility]
    public struct LFloat : ILerpable<float>, ILerpableVector
    {
        public LFloat(float inValue) => Value = inValue;

        public float Value { get; set; }

        public float4 VectorValues
        {
            get => Value;
            set => Value = value.x;
        }

        public readonly float Lerp(float valueA, float valueB, float lerpValue) => math.lerp(valueA, valueB, lerpValue);
        
        public readonly float Clamp(float min, float max) => math.clamp(Value, min, max);
    }

    [GenerateTestsForBurstCompatibility]
    public struct LFloat2 : ILerpable<float2>, ILerpableVector
    {
        public float2 Value { get; set; }

        public float4 VectorValues
        {
            get => new float4(Value, 0, 0);
            set => Value = value.xy;
        }

        public readonly float2 Lerp(float2 valueA, float2 valueB, float lerpValue) => math.lerp(valueA, valueB, lerpValue);
        
        public readonly float2 Clamp(float2 min, float2 max) => math.clamp(Value, min, max);
    }

    [GenerateTestsForBurstCompatibility]
    public struct LFloat3 :ILerpable<float3>, ILerpableVector
    {
        public float3 Value { get; set; }

        public float4 VectorValues
        {
            get => new float4(Value, 0);
            set => Value = value.xyz;
        }

        public readonly float3 Lerp(float3 valueA, float3 valueB, float lerpValue) => math.lerp(valueA, valueB, lerpValue);
        
        public readonly float3 Clamp(float3 min, float3 max) => math.clamp(Value, min, max);
    }
    
    [GenerateTestsForBurstCompatibility]
    public struct LFloat4 : ILerpable<float4>, ILerpableVector
    {
        public float4 Value { get; set; }

        public float4 VectorValues
        {
            get => Value;
            set => Value = value;
        }

        public readonly float4 Lerp(float4 valueA, float4 valueB, float lerpValue) => math.lerp(valueA, valueB, lerpValue);
        
        public readonly float4 Clamp(float4 min, float4 max) => math.clamp(Value, min, max);
    }

    [GenerateTestsForBurstCompatibility]
    public struct LFloat3X2 : ILerpable<float3x2>
    {
        public float3x2 Value { get; set; }

        public readonly float3x2 Lerp(float3x2 valueA, float3x2 valueB, float lerpValue)
        {
            return new float3x2(math.lerp(valueA.c0, valueB.c0, lerpValue),
                math.lerp(valueA.c1, valueB.c1, lerpValue));
        }
        
        public readonly float3x2 Clamp(float3x2 min, float3x2 max)
        {
            return new float3x2(math.clamp(Value.c0, min.c0, max.c0),
                math.clamp(Value.c1, min.c1, max.c1));
        }
    }
    
    /// <summary> A float that can be smoothed over time </summary>
    [GenerateTestsForBurstCompatibility]
    public struct LFloatSmooth : ILerpableVector, ISmoothComponentLerpable<float>
    {
        public float Value { get; set; }
        public float TargetValue { get; set; }
        
        public float SmoothRate { readonly get => ComponentSmoothRate; set => ComponentSmoothRate = value; }
        public float ComponentSmoothRate { get; set; }

        public float4 VectorValues
        {
            get => Value;
            set => Value = value.x;
        }

        public readonly float Lerp(float valueA, float valueB, float lerpValue) => math.lerp(valueA, valueB, lerpValue);
        
        public readonly float ComponentLerp(float valueA, float valueB, float lerpValue) => math.lerp(valueA, valueB, lerpValue);
        
        public readonly float DiffToTarget => math.abs(TargetValue - Value);
        
        public readonly float Clamp(float min, float max) => math.clamp(Value, min, max);

        public readonly float ComputeStepSize(float stepSize) => math.min(DiffToTarget, stepSize);
    }
    
    /// <summary> A float2 that can be smoothed over time </summary>
    [GenerateTestsForBurstCompatibility]
    public struct LFloat2Smooth : ILerpableVector, ISmoothLerpable<float2>
    {
        public float2 Value { get; set; }
        public float2 TargetValue { get; set; }
        public float SmoothRate { get; set; }

        public float4 VectorValues
        {
            readonly get => new float4(Value, 0, 0);
            set => Value = value.xy;
        }

        public readonly float2 Lerp(float2 valueA, float2 valueB, float lerpValue) => math.lerp(valueA, valueB, lerpValue);
        
        public readonly float2 ComponentLerp(float2 valueA, float2 valueB, float2 lerpValue) => math.lerp(valueA, valueB, lerpValue);
        
        public readonly float2 DiffToTarget => TargetValue - Value;
        
        public readonly float2 Clamp(float2 min, float2 max) => math.clamp(Value, min, max);
        
        public readonly float2 ComputeStepSize(float stepSize) => math.min(DiffToTarget, stepSize);
    }
    
    public struct LFloat2ComponentSmooth : ILerpableVector, ISmoothLerpable<float2>, ISmoothComponentLerpable<float2>
    {
        public float2 Value { get; set; }
        public float2 TargetValue { get; set; }
        
        public float SmoothRate { readonly get => ComponentSmoothRate.x; set => ComponentSmoothRate = value; }
        public float2 ComponentSmoothRate { get; set; }
        
        public float4 VectorValues
        {
            readonly get => new float4(Value, 0, 0);
            set => Value = value.xy;
        }

        public readonly float2 Lerp(float2 valueA, float2 valueB, float lerpValue) => math.lerp(valueA, valueB, lerpValue);
        public readonly float2 ComponentLerp(float2 valueA, float2 valueB, float2 lerpValue) => math.lerp(valueA, valueB, lerpValue);
        
        public readonly float2 DiffToTarget => math.length(TargetValue - Value);
        
        public readonly float2 Clamp(float2 min, float2 max) => math.clamp(Value, min, max);
        
        public readonly float2 ComputeStepSize(float stepSize) => math.min(DiffToTarget, stepSize);
    }
    
    /// <summary> A float3 that can be smoothed over time </summary>
    [GenerateTestsForBurstCompatibility]
    public struct LFloat3Smooth : ILerpableVector, ISmoothLerpable<float3>
    {
        public float3 Value { get; set; }
        public float3 TargetValue { get; set; }
        public float SmoothRate { get; set; }

        public float4 VectorValues
        {
            readonly get => new float4(Value, 0);
            set => Value = value.xyz;
        }

        public readonly float3 Lerp(float3 valueA, float3 valueB, float lerpValue) => math.lerp(valueA, valueB, lerpValue);
        
        public readonly float3 Clamp(float3 min, float3 max) => math.clamp(Value, min, max);
        
        public readonly float3 DiffToTarget => TargetValue - Value;
    }
    
    /// <summary> A float4 that can be smoothed over time </summary>
    [GenerateTestsForBurstCompatibility]
    public struct LFloat4Smooth : ILerpableVector, ISmoothLerpable<float4>
    {
        public float4 Value { get; set; }
        public float4 TargetValue { get; set; }
        public float SmoothRate { get; set; }

        public float4 VectorValues
        {
            readonly get => Value;
            set => Value = value;
        }

        public readonly float4 Lerp(float4 valueA, float4 valueB, float lerpValue) => math.lerp(valueA, valueB, lerpValue);
        
        public readonly float4 Clamp(float4 min, float4 max) => math.clamp(Value, min, max);
        
        public readonly float4 DiffToTarget => TargetValue - Value;
    }
}