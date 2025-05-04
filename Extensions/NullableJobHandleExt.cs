using System.Runtime.CompilerServices;
using Unity.Jobs;

namespace VLib
{
    public static class NullableJobHandleExt
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CompleteAndClear(this ref JobHandle? nullableHandle)
        {
            if (!nullableHandle.HasValue)
                return false;
            
            nullableHandle.Value.Complete();
            nullableHandle = null;
            return true;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryCompleteAndClear(this ref JobHandle? nullableHandle)
        {
            if (!nullableHandle.HasValue)
                return true;
            
            if (!nullableHandle.Value.IsCompleted)
                return false;
            
            nullableHandle.Value.Complete();
            nullableHandle = null;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ExistsCompleted(this ref JobHandle? nullableHandle) => nullableHandle?.IsCompleted ?? false;
    }
}