using System.Diagnostics;
using System.Threading;
using Unity.Burst.CompilerServices;
using static Unity.Mathematics.math;

namespace OtherIterations.EntityAwarenessSystem
{
    /// <summary> A throttle that tries to smoothly move toward the average required throughput rate. <br/>
    /// Call <see cref="Increment"/> every time you do a unit of work. Then call <see cref="Turnover"/> to reset the counter and update the throttle. <br/>
    /// Don't forget to update the <see cref="elementCount"/>. <br/>
    /// Completely thread-safe. </summary>
    public struct Throttler
    {
        public int elementCount;
        public float ratePerElement;
        public float hardLimitMultiplier;
        //
        int counter;
        //
        float internalSoftLimitF;
        int softLimit;
        int hardLimit;
        //
        float maxDeltaTime;
        float normalizeMultiplier;
        float growMultiplier;
        /// <summary> Based on the element count and rate, how far over the computed target limit can the actual limit go (from growth) </summary>
        float maxOverTarget;
        //
        int internalLock;
        //
        bool errorOnUpperBoundBreach;
        bool hasErrored;

        public float SoftLimitF => internalSoftLimitF;
        public int SoftLimit => softLimit;
        public int HardLimit => hardLimit;

        public Throttler(int elementCount, float ratePerElement, float hardLimitMultiplier, float maxDeltaTime, float normalizeMultiplier, float growMultiplier, 
            int initialSoftCounter = 1, float maxOverTarget = 4f, bool errorOnUpperBoundBreach = true)
        {
            this.elementCount = elementCount;
            this.ratePerElement = ratePerElement;
            this.hardLimitMultiplier = hardLimitMultiplier;
            this.maxDeltaTime = maxDeltaTime;
            this.normalizeMultiplier = normalizeMultiplier;
            this.growMultiplier = growMultiplier;
            this.maxOverTarget = maxOverTarget;
            counter = 0;
            internalSoftLimitF = initialSoftCounter;
            softLimit = initialSoftCounter;
            hardLimit = (int) min(int.MaxValue, (ulong)initialSoftCounter * 2);
            internalLock = 0;
            
            this.errorOnUpperBoundBreach = errorOnUpperBoundBreach;
            hasErrored = false;
            
            CheckNonAbsurd();
        }

        public int SuggestedCountFor(float deltaTime)
        {
            deltaTime = min(deltaTime, maxDeltaTime);
            return (int) max(1, ceil(elementCount * ratePerElement * deltaTime));
        }

        /// <summary> Take a unit of throughput. </summary>
        public bool Increment(bool stopAtSoftLimit = false)
        {
            var incremented = Interlocked.Increment(ref counter);
            if (incremented > softLimit)
                return !stopAtSoftLimit && incremented <= hardLimit;
            return true;
        }

        /// <summary> Reset the counter and recompute throttle limits. </summary>
        public void Turnover(float deltaTime)
        {
            AdjustThrottle(deltaTime);
            Interlocked.Exchange(ref counter, 0);
        }

        void AdjustThrottle(float deltaTime)
        {
            deltaTime = min(deltaTime, maxDeltaTime);
            
            InternalLock();
            
            // Code is extremely defensive since the locking mechanism is unable to self-correct or timeout.
            try
            {
                // Normalize force
                var rateUntimed = elementCount * ratePerElement;
                var rateMax = rateUntimed * maxDeltaTime;
                var targetSoftLimit = elementCount * ratePerElement * deltaTime;
                internalSoftLimitF = lerp(internalSoftLimitF, targetSoftLimit, normalizeMultiplier);

                // Grow force
                var growthNeeded = counter / internalSoftLimitF;
                if (growthNeeded > 1)
                    internalSoftLimitF = lerp(internalSoftLimitF, internalSoftLimitF * growthNeeded, growMultiplier);
                
                // Limit to a certain range above the normalizing target (this enforces an absolute upper bound for sanity)
                var maxOverTargetValue = maxOverTarget * rateMax;
                if (maxOverTargetValue > 4 && internalSoftLimitF > maxOverTargetValue)
                {
                    internalSoftLimitF = maxOverTargetValue;
                    if (errorOnUpperBoundBreach && !hasErrored)
                    {
                        hasErrored = true;
                        UnityEngine.Debug.LogError($"Throttle soft limit has breached the upper bound! Limit: {internalSoftLimitF}, MaxAllowed: {maxOverTargetValue}");
                    }
                }

                if (internalSoftLimitF >= int.MaxValue)
                {
                    InternalUnlock();
                    UnityEngine.Debug.LogError("Throttle new soft limit is breaching int.MaxValue!");
                    return;
                }
                softLimit = max((int)(internalSoftLimitF + .99f), 1);
                
                // Update hard limit
                var newHardLimit = ceil(max(1, internalSoftLimitF) * hardLimitMultiplier);
                var breach = newHardLimit >= int.MaxValue;
                if (Hint.Unlikely(breach))
                {
                    InternalUnlock();
                    UnityEngine.Debug.LogError("Throttle new hard limit is breaching int.MaxValue!");
                    return;
                }
                hardLimit = (int) newHardLimit;
            }
            finally
            {
                InternalUnlock();
            }
        }

        void InternalLock()
        {
            while (Interlocked.CompareExchange(ref internalLock, 1, 0) != 0) { }
        }

        void InternalUnlock()
        {
            Interlocked.Exchange(ref internalLock, 0);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        void CheckNonAbsurd()
        {
            if (ratePerElement <= .0001f)
                throw new System.ArgumentOutOfRangeException($"Rate per element is too low: {ratePerElement}");
            if (hardLimitMultiplier <= 1)
                throw new System.ArgumentOutOfRangeException($"Hard limit multiplier is too low: {hardLimitMultiplier}");
            if (maxDeltaTime <= 0.0001f)
                throw new System.ArgumentOutOfRangeException($"Max delta time is too low: {maxDeltaTime}");
            if (normalizeMultiplier <= 0.0001f)
                throw new System.ArgumentOutOfRangeException($"Normalize multiplier is too low: {normalizeMultiplier}");
            if (growMultiplier <= 0.0001f)
                throw new System.ArgumentOutOfRangeException($"Grow multiplier is too low: {growMultiplier}");
        }
    }
}