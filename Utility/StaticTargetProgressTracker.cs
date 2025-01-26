/*using System;
using Unity.Collections;
using Unity.Mathematics;

namespace VLib
{
    /// <summary>Track progress toward a target, and build up a variable estimating whether something is stuck. WARNING: Is mutable struct, plan accordingly!</summary>
    [GenerateTestsForBurstCompatibility]
    public struct StaticTargetProgressTracker
    {
        //Data
        public float3 target;
        public float stuckness;
        public float3 lastUpdatedPosition;
        public float lastUpdatedTime;
        
        //Cache
        float lastPosDistFromTarget;

        //Params
        float2 progressStuckIncMinMax;
        public float stucknessStrength;
        public float stucknessDegradePerSecond;
        
        public float2 ProgressStuckIncMinMax
        {
            get => progressStuckIncMinMax;
            set => progressStuckIncMinMax = value;
        }

        public StaticTargetProgressTracker(float3 target) : this()
        {
            ResetData();
            ResetParams();
            
            this.target = target;
        }

        public void ResetData()
        {
            target = default;
            stuckness = 0;
            lastUpdatedPosition = default;
            lastUpdatedTime = -1;
            lastPosDistFromTarget = float.MaxValue;
        }

        public void ResetParams()
        {
            progressStuckIncMinMax = new float2(0, .6f);
            stucknessStrength = .25f;
            stucknessDegradePerSecond = .02f;
        }

        /// <summary>Track progression, if target changes, everything is reset.</summary>
        /// <param name="position"></param>
        /// <param name="target"></param>
        /// <param name="speed"></param>
        /// <param name="timeDotTime"></param>
        /// <returns>True IF tracker believe progression is 'stuck'.</returns>
        public bool UpdateTrackCheckStuck(float3 position, float3 newTarget, float speed, float timeDotTime)
        {
            // Reset if target change, this is a static target tracker
            if (math.any(target != newTarget))
            {
                ResetData();
                target = newTarget;
            }

            // First track request, nothing to compare with...
            if (lastUpdatedTime < 0)
            {
                lastUpdatedPosition = position;
                lastUpdatedTime = timeDotTime;
                lastPosDistFromTarget = math.distance(position, target);
                return false;
            }

            // Compute relevant info
            float timeDif = timeDotTime - lastUpdatedTime;

            // Compute Distances
            float predictedTravelDist = speed * timeDif;
            float actualDistFromTarget = math.distance(position, target);
            float actualTravelDistTowardsTarget = lastPosDistFromTarget - actualDistFromTarget;
            // Cache for next update
            lastPosDistFromTarget = actualDistFromTarget;

            // Compare current and previous
            float progress = actualTravelDistTowardsTarget / predictedTravelDist;

            // Lose stuckness over time
            stuckness = math.saturate(stuckness - timeDif * stucknessDegradePerSecond);

            // Increment stuckness
            if (progress < progressStuckIncMinMax.y)
            {
                //Remap progress inside increment range to 0-1, and flip so that more progress == less stuckness
                var stuckRaw = math.saturate(math.remap(progressStuckIncMinMax.x, progressStuckIncMinMax.y, 1, 0, progress));
                // Restrict time jump when close to target to stop an infinite stuckness loop
                float timeDifProcessed = timeDif;// * math.saturate(actualDistFromTarget / 4);
                stuckness += stuckRaw * timeDifProcessed * stucknessStrength;
            }

            //Update internal for next sample
            lastUpdatedPosition = position;
            lastUpdatedTime = timeDotTime;

            return stuckness >= 1;
        }
    }
}*/