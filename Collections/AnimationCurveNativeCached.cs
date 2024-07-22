using System.Diagnostics.Contracts;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using WrapMode = UnityEngine.WrapMode;

namespace VLib
{
    /// <summary> Legacy native version that caches animation curve values into an array. </summary>
    public struct AnimationCurveNativeCached
    {
        public UnsafeList<float> curveData;
        public WrapMode wrapMode;
        
        public int Resolution => curveData.m_length;

        public AnimationCurveNativeCached(AnimationCurve curve, int resolution = 256, WrapMode wrapMode = WrapMode.Clamp, float sampleScale = 1f, Allocator allocator = Allocator.Persistent)
        {
            resolution = math.max(4, resolution);
            curveData = new UnsafeList<float>(resolution, allocator);
            curveData.Length = resolution;
            this.wrapMode = wrapMode;

            Resample(curve, sampleScale); //Removed fast parallel resample, AnimationCurve.Evaluate produces random errors!
        }

        public bool IsCreated => curveData.IsCreated;

        public void Dispose()
        {
            if (curveData.IsCreated)
                curveData.Dispose();//
        }

        [GenerateTestsForBurstCompatibility, Pure]
        public float Evaluate(float samplePoint)
        {
            samplePoint = wrapMode switch
            {
                WrapMode.Clamp => math.clamp(samplePoint, 0f, 1f),
                WrapMode.Loop => math.frac(samplePoint),
                WrapMode.PingPong => (int)samplePoint % 2 == 0 ? math.frac(samplePoint) : 1f - math.frac(samplePoint),
                _ => math.clamp(samplePoint, 0f, 1f)
            };
            
            float sampleIndexF = math.lerp(0, Resolution - 1, samplePoint);
            int minIndex = (int) sampleIndexF;
            int maxIndex = math.min(minIndex + 1, Resolution - 1);
            float tween = math.frac(sampleIndexF);
            
            //Sample and Lerp
            float sampleA = curveData[minIndex];
            float sampleB = curveData[maxIndex];

            return math.lerp(sampleA, sampleB, tween);
        }

        public void Resample(AnimationCurve sourceCurve, float sampleScale = 1f)
        {
            sourceCurve.preWrapMode = WrapMode.Clamp;
            sourceCurve.postWrapMode = WrapMode.Clamp;
            float resolutionF = curveData.m_length - 1; //We want last index to evaluate at 1 exactly
                
            for (int i = 0; i < curveData.Length; i++)
            {
                float samplePoint = i / resolutionF;
                curveData[i] = sourceCurve.Evaluate(samplePoint) / sampleScale;
            }
        }

        /*public bool BullshitPresent()
        {
            if (!curveData.IsCreated)
            {
                Debug.LogError("Curve data not created");
                return true;
            }
            if (curveData.IsEmpty)
            {
                Debug.LogError("Curve data is empty");
                return true;
            }
            
            for (int i = 0; i < curveData.Length; i++)
            {
                var v = curveData[i];
                if (math.isinf(v) || math.isnan(v))
                    return true;
            }

            return false;
        }*/
    }
}