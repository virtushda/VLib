using System.Runtime.InteropServices;
using UnityEngine;

namespace VLib
{
    public static class NullableGCHandleExt
    {
        public static bool FreeAndClear(this ref GCHandle? nullableHandle)
        {
            var hasValue = nullableHandle.HasValue;
            if (hasValue)
            {
                var value = nullableHandle.Value;
                if (value.IsAllocated)
                    value.Free();
                else
                {
                    Debug.LogError("Nullable GCHandle was passed in, but GCHandle was not allocated!");
                    return false;
                }
            }

            nullableHandle = null;
            return hasValue;
        }
    }
}