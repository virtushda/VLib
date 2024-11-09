using Unity.Burst.Intrinsics;
using Unity.Mathematics;
using VLib.Systems;

namespace VLib.Libraries.VLib.Unsafe.Utility
{
    /// <summary> Burst version of SpinWait, not nearly as fancy. Not going to pretend I understand SpinWait super well, I just need something workable! <br/>
    /// This version has a timeout! </summary>
    public struct BurstSpinWaitTimeout
    {
        int spinner;
        public readonly double startTime;
        public readonly float timeoutSeconds;
        
        public double TimeSinceStart => VTime.intraFrameTime - startTime;
        
        public BurstSpinWaitTimeout(float timeoutSeconds)
        {
            spinner = 0;
            startTime = VTime.intraFrameTime;
            this.timeoutSeconds = timeoutSeconds;
        }
        
        /// <summary> Returns false if timed out. </summary>
        public bool SpinOnce() => Spin(8);

        /// <summary> Returns false if timed out. </summary>
        public bool Spin(int iterations, int pauseIncrements = 16)
        {
            iterations = math.max(1, iterations);
            while (iterations > 0)
            {
                --iterations;
                ++spinner;
                if (spinner < 0)
                    spinner = 0;
                if (spinner % pauseIncrements == 0)
                    Common.Pause();

                // Timeout
                if (VTime.intraFrameTime - startTime > timeoutSeconds)
                    return false;
            }
            
            // If out of iterations, check for timeout
            return VTime.intraFrameTime - startTime < timeoutSeconds;
        }

        /// <summary> Spin and call pause intrinsic. </summary>
        public bool SpinPause() => Spin(1, 1);
    }
}