﻿#if UNITY_EDITOR
#define PROVE_MAIN_THREAD
#endif

using System;
using System.Diagnostics;
using System.Threading;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Profiling;
using Application = UnityEngine.Device.Application;
using Debug = UnityEngine.Debug;

namespace VLib.Threading
{
    public class MainThread
    {
        private static int mainThreadId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize() => mainThreadId = Thread.CurrentThread.ManagedThreadId;

        /// <summary> Checks the thread ID against the known main thread ID. </summary>
        /// <param name="allowExecutingInJob"> By default, false is returned if this is called from within a job. Pass true to allow a job running on the main thread to return true. </param>
        public static bool OnMain(bool allowExecutingInJob = false)
        {
            Profiler.BeginSample("Check OnMain");
            bool isMainThread = mainThreadId == Thread.CurrentThread.ManagedThreadId && (!JobsUtility.IsExecutingJob || allowExecutingInJob);
#if PROVE_MAIN_THREAD
            // Prove it in editor by doing something cheap on the main thread.
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
#endif
            Profiler.EndSample();
            return isMainThread;
        }

        [Conditional("UNITY_EDITOR")]
        public static void AssertMainThreadConditional()
        {
            if (!OnMain())
                throw new InvalidOperationException("This operation must be done on the main thread.");
        }
    }
}