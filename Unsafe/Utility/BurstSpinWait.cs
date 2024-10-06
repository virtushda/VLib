using Unity.Burst.Intrinsics;

namespace VLib.Libraries.VLib.Unsafe.Utility
{
    /// <summary> Burst version of SpinWait, not nearly as fancy. Not going to pretend I understand SpinWait super well, I just need something workable! </summary>
    public struct BurstSpinWait
    {
        int spinner;

        public void SpinOnce() => Spin(8);
        
        public void Spin(int iterations, int pauseIncrements = 16)
        {
            while (--iterations > 0)
            {
                ++spinner;
                if (spinner % pauseIncrements == 0)
                    Common.Pause();
            }
        }
    }
}