using Unity.Jobs;

namespace VLib
{
    public static class JobHandleExt
    {
        public static bool TryComplete(this JobHandle handle)
        {
            if (handle.IsCompleted)
            {
                handle.Complete();
                return true;
            }
            return false;
        }
    }
}