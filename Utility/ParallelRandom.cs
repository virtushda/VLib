using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using VLib.Systems;
using Random = Unity.Mathematics.Random;

namespace VLib
{
    /// <summary> A thread safe wrapper of Mathematics.Random. </summary>
    public class ParallelRandom
    {
        const byte MAX_LANES = 8;
        struct RandomLane
        {
            public Random random;
            public int locked;
        }

        UnsafeList<RandomLane> randomLanes;
        // Increment this and let it wrap forever
        int nextLane;
        
        readonly ValueStopwatch internalWatch;
        int timeoutMS;
        long timeoutTicks;

        public int TimeoutMS
        {
            get => timeoutMS;
            set
            {
                timeoutMS = value;
                timeoutTicks = VTime.MillisecondsToTicks(timeoutMS);
            }
        }

        public ParallelRandom(int timeoutMS = 10)
        {
            randomLanes = new UnsafeList<RandomLane>(MAX_LANES, Allocator.Persistent);
            randomLanes.Length = MAX_LANES;
            for (int i = 0; i < MAX_LANES; i++)
            {
                randomLanes[i] = new RandomLane
                {
                    random = new Random((uint)UnityEngine.Random.Range(999, int.MaxValue)),
                    locked = 0
                };
            }
            
            //Start stopwatch so it's always ready!
            internalWatch = ValueStopwatch.StartNew();
            TimeoutMS = timeoutMS;
        }

        public void Dispose() => randomLanes.DisposeRefToDefault();

        ref RandomLane AcquireExclusiveLaneAccess()
        {
            int spinBudgetBeforeTimer = 1024;
            long stopwatchStartTime = -1;
            
            var laneIndex = Interlocked.Increment(ref nextLane) % MAX_LANES;
            ref var lane = ref randomLanes.ElementAt(laneIndex);

            // Try claim lock value atomically, only proceed if valid, otherwise spin
            while (Interlocked.CompareExchange(ref lane.locked, 1, 0) != 0)
            {
                // Spin without timer for a set number of spins
                if (spinBudgetBeforeTimer > 0)
                {
                    spinBudgetBeforeTimer--;
                    continue;
                }

                if (stopwatchStartTime == -1) // After limited free spins, start timing 
                    stopwatchStartTime = internalWatch.ElapsedTicks;
                // Spin checking timer...
                if (internalWatch.ElapsedTicks > stopwatchStartTime + timeoutTicks)
                {
                    Debug.LogError("ParallelRandom: Failed to acquire lock within timeout!");
                    return ref lane;
                }
            }
            return ref lane;
        }

        void ReleaseLane(ref RandomLane lane)
        {
            if (Interlocked.Exchange(ref lane.locked, 0) == 0)
                Debug.LogError("ParallelRandom: Releasing lock that was not acquired!");
        }

        ///<summary> Min is 0, max is 1. </summary>
        public float NextFloat() => NextFloat(0, 1);
        
        ///<summary> Min is 0, define max. </summary>
        public float NextFloat(float max) => NextFloat(0, max);
        
        ///<summary> Min is inclusive, max is exclusive. </summary>
        public float NextFloat(float min, float max)
        {
            ref var lane = ref AcquireExclusiveLaneAccess();
            var randomValue = lane.random.NextFloat(min, max);
            ReleaseLane(ref lane);
            return randomValue;
        }
        
        ///<summary> Min is inclusive, max is exclusive. </summary>
        public double NextDouble(double min, double max)
        {
            ref var lane = ref AcquireExclusiveLaneAccess();
            var randomValue = lane.random.NextDouble(min, max);
            ReleaseLane(ref lane);
            return randomValue;
        }

        ///<summary> Randomly returns 0 or 1. </summary>
        public int NextInt() => NextInt(0, 2);
        
        public int NextInt(int maxExclusive) => NextInt(0, maxExclusive);
        
        ///<summary> Min is inclusive, max is exclusive. </summary>
        public int NextInt(int min, int maxExclusive)
        {
            ref var lane = ref AcquireExclusiveLaneAccess();
            var randomValue = lane.random.NextInt(min, maxExclusive);
            ReleaseLane(ref lane);
            return randomValue;
        }
    }
}