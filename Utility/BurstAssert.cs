using System.Diagnostics;

namespace VLib
{
    /// <summary> Assertions that are callable from inside burst. </summary>
    public static class BurstAssert
    {
        [Conditional("UNITY_EDITOR")]
        public static void True(bool condition)
        {
            if (!condition)
                throw new System.Exception("BurstAssert.True, received false");
        }
        
        [Conditional("UNITY_EDITOR")]
        public static void False(bool condition)
        {
            if (condition)
                throw new System.Exception("BurstAssert.False, received true");
        }
    }
}