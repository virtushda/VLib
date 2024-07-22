using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace VLib.Utility
{
    public static class UnsafeListUtil
    {
        public unsafe struct UnsafeListPtrDisposalJob<T> : IJob
            where T : unmanaged
        {
            UnsafeList<T>* listPtr;
            
            /// <param name="listPtr"> A pointer created by UnsafeList-T-.Create </param>
            public UnsafeListPtrDisposalJob(UnsafeList<T>* listPtr)
            {
                this.listPtr = listPtr;
            }

            public void Execute() => UnsafeList<T>.Destroy(listPtr);
        }
    }
}