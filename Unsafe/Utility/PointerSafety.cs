using System.Threading;
using UnityEngine.Assertions;

namespace VLib.Utility
{
    public static class PointerSafetyUtil
    {
        /// <summary> In order to get the native pointer, one must increment a counter. Decrementing it is required and the holder is expected to null their pointer references. </summary>
        public static void IncrementNativeUserCounter(ref int counter)
        {
            Interlocked.Increment(ref counter);
            Assert.IsTrue(counter < 4096, "Ptr safety counter went above 4096!");
        }
        
        /// <summary> This must be called to manually release a hold of the native ptr. This is how basic safety is enforced. </summary>
        public static void DecrementNativeUserCounter(ref int counter)
        {
            Interlocked.Decrement(ref counter);
            Assert.IsTrue(counter >= 0, "Ptr safety counter went below 0!");
        }
    }
}