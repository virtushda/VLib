using System;
using System.Threading;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Profiling;
using Application = UnityEngine.Device.Application;

namespace VLib.Threading
{
    public class MainThread
    {
        private static int mainThreadId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        public static bool OnMain()
        {
            bool isMainThread = mainThreadId == Thread.CurrentThread.ManagedThreadId && !JobsUtility.IsExecutingJob;
#if UNITY_EDITOR
            // Prove it in editor by doing something cheap on the main thread.
            Profiler.BeginSample("Main thread proof");
            if (isMainThread)
            {
                try
                {
                    bool mainThreadOpResult = Application.isEditor;
                }
                catch (Exception e)
                {
                    Debug.LogError("MainThread.OnMainThread is supposed to be on the main thread, but could not prove it!");
                    Debug.LogException(e);
                    isMainThread = false;
                }
            }
            Profiler.EndSample();
#endif
            return isMainThread;
        }
    }
}