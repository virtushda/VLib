using Unity.Jobs;

namespace VLib
{
    public static class NullableJobHandleExt
    {
        public static bool CompleteAndClear(this ref JobHandle? nullableHandle)
        {
            if (!nullableHandle.HasValue)
                return false;
            
            nullableHandle.Value.Complete();
            nullableHandle = null;
            return true;
        }
        
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

        public static bool ExistsCompleted(this ref JobHandle? nullableHandle) => nullableHandle?.IsCompleted ?? false;
    }
}