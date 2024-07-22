using System;
using System.Runtime.InteropServices;
using Unity.Jobs;

namespace VLib
{
    public struct ActionJob : IJob
    {
        GCHandle actionHandle;

        public ActionJob(Action action)
        {
            actionHandle = GCHandle.Alloc(action);
        }

        public void Execute()
        {
            (actionHandle.Target as Action)?.Invoke();
            actionHandle.Free();
        }
    }
}