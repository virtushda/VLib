using System;
using System.Threading;

namespace VLib
{
    /// <summary> Container that toggles between two values </summary>
    public class Interleaver<T>
    {
        public bool toggle;
        public T valueA;
        public T valueB;

        public ref T Current => ref toggle ? ref valueB : ref valueA;

        public ref T Alternate => ref toggle ? ref valueA : ref valueB;

        public Interleaver(T valueA, T valueB, bool toggle = false)
        {
            this.toggle = toggle;
            this.valueA = valueA;
            this.valueB = valueB;
        }

        //Moved to generic extension, which skips boxing
        /*public void AutoTryDispose()
        {
            Try.Catch(() =>
            {
                if (valueA is IDisposable disposableA)
                    disposableA.Dispose();
                if (valueB is IDisposable disposableB)
                    disposableB.Dispose();
            });
        }*/

        public void Interleave() => toggle = !toggle;
    }
    
    public static class InterleaverExt
    {
        public static void AutoTryDispose<T>(this Interleaver<T> interleaver)
            where T : IDisposable
        {
            Try.Catch(() =>
            {
                interleaver.valueA.Dispose();
                interleaver.valueB.Dispose();
            });
        }
    }
}