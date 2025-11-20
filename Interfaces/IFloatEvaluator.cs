using UnityEngine;

namespace VLib
{
    public interface IFloatEvaluator
    {
        float Evaluate(float input);
    }
    
    public readonly struct AnimationCurveEvaluator : IFloatEvaluator
    {
        readonly AnimationCurve curve;

        public AnimationCurveEvaluator(AnimationCurve curve)
        {
            this.curve = curve;
        }

        public float Evaluate(float input) => curve?.Evaluate(input) ?? 1f;
        
        public static implicit operator AnimationCurveEvaluator(AnimationCurve curve) => new(curve);
    }
}