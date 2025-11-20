using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace VLib
{
    public interface ISpinLockHold
    {
        public bool Succeeded { get; }
        public bool IsCreated { get; }
    }
    
    // Generic Extension to conditionally check lock holds without boxing
    public static class ISpinLockHoldExt
    {
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConditionalCheckLockHeld<T>(this T lockHold)
            where T : ISpinLockHold
        {
            if (!lockHold.Succeeded)
                throw new InvalidOperationException("Lock must be held!");
        }
    }
}