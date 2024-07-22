using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Unity.Collections;
using static Unity.Mathematics.math;

namespace VLib
{
    public struct NativeCurve : IDisposable
    {
        public bool IsCreated => values.IsCreated;

        int resolution;
        private NativeArray<float> values;
        private WrapMode preWrapMode;
        private WrapMode postWrapMode;

        public NativeCurve(AnimationCurve curve, int resolution)
        {
            if (curve == null)
                throw new NullReferenceException("Input curve is null!");
            
            preWrapMode = curve.preWrapMode;
            postWrapMode = curve.postWrapMode;
            this.resolution = resolution;
            values = default;
            
            InitializeValues(this.resolution);
            Update(curve, resolution);
        }

        private void InitializeValues(int count)
        {
            if (values.IsCreated)
                values.Dispose();

            values = new NativeArray<float>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        }

        public void Update(AnimationCurve curve, int newResolution)
        {
            this.resolution = newResolution;
            if (curve == null)
                throw new NullReferenceException("Animation curve is null.");

            preWrapMode = curve.preWrapMode;
            postWrapMode = curve.postWrapMode;

            if (!values.IsCreated || values.Length != resolution)
                InitializeValues(resolution);

            for (int i = 0; i < resolution; i++)
                values[i] = curve.Evaluate((float) i / (float) resolution);
        }

        public float Evaluate(float t)
        {
            var count = values.Length;

            if (count == 1)
                return values[0];

            if (t < 0f)
            {
                switch (preWrapMode)
                {
                    default:
                        return values[0];
                    case WrapMode.Loop:
                        t = 1f - (abs(t) % 1f);
                        break;
                    case WrapMode.PingPong:
                        t = pingpong(t, 1f);
                        break;
                }
            }
            else if (t > 1f)
            {
                switch (postWrapMode)
                {
                    default:
                        return values[count - 1];
                    case WrapMode.Loop:
                        t %= 1f;
                        break;
                    case WrapMode.PingPong:
                        t = pingpong(t, 1f);
                        break;
                }
            }

            var it = t * (count - 1);

            var lower = (int) it;
            var upper = lower + 1;
            if (upper >= count)
                upper = count - 1;

            return lerp(values[lower], values[upper], it - lower);
        }

        public void Dispose()
        {
            if (values.IsCreated)
                values.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float repeat(float t, float length)
        {
            return clamp(t - floor(t / length) * length, 0, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float pingpong(float t, float length)
        {
            t = repeat(t, length * 2f);
            return length - abs(t - length);
        }
    }
}