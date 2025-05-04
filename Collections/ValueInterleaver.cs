using System;
using System.Runtime.CompilerServices;

namespace VLib
{
    /// <summary> Interleave efficiently between two unmanaged values. </summary>
    public struct ValueInterleaver<T>
        where T : unmanaged
    {
        internal T valueA;
        internal T valueB;
        bool toggle;
        
        public bool Toggle => toggle;

        public ValueInterleaver(in T valueA, in T valueB, bool toggle = false)
        {
            this.valueA = valueA;
            this.valueB = valueB;
            this.toggle = toggle;
        }

        public void Interleave()
        {
            
        }
    }
    
    public static class ValueInterleaverExt
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T InterleaveFetch<T>(this ref ValueInterleaver<T> interleaver)
            where T : unmanaged
        {
            interleaver.Interleave();
            return ref interleaver.GetCurrent();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetCurrent<T>(this ref ValueInterleaver<T> interleaver)
            where T : unmanaged
        {
            return ref interleaver.Toggle ? ref interleaver.valueB : ref interleaver.valueA;
        }
        
        public static void DisposeBoth<T>(this ref ValueInterleaver<T> interleaver)
            where T : unmanaged, IDisposable
        {
            interleaver.valueA.Dispose();
            interleaver.valueB.Dispose();
        }
    }
}