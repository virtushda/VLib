using System;
using System.Threading;
using UnityEngine;

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

        /// <summary> Swaps the items and returns the new current. </summary>
        public ref T Interleave()
        {
            toggle = !toggle;
            return ref toggle ? ref valueB : ref valueA;
        }
    }
    
    public static class InterleaverExt
    {
        public static void AutoTryDispose<T>(this Interleaver<T> interleaver)
            where T : IDisposable
        {
            try
            {
                interleaver.valueA.Dispose();
                interleaver.valueB.Dispose();
            }
            catch (Exception e) 
            {
                Debug.LogException(e);
            }
        }
    }
}