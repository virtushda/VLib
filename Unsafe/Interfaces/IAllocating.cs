using System;
using System.Diagnostics;

namespace VLib
{
    public interface IAllocating : IDisposable
    {
        bool IsCreated { get; }
    }
    
    public static class IAllocatingExtensions
    {
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void ConditionalCheckIsCreated<T>(this T container)
            where T : IAllocating
        {
            if (!container.IsCreated)
                throw new InvalidOperationException("Container is not created.");
        }
        
        public static void DisposeSafe<T>(this T container)
            where T : class, IAllocating
        {
            if (container.IsCreated)
                container.Dispose();
        }

        public static void DisposeRef<T>(ref this T container)
            where T : struct, IAllocating
        {
            if (container.IsCreated)
                container.Dispose();
            container = default;
        }
    }
}