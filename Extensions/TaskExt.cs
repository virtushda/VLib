using System;
using System.Collections;
using System.Threading.Tasks;
using Sirenix.Utilities;
using UnityEngine;
using VLib.Debugging;

namespace VLib
{
    public static class TaskExt
    {
        public static IEnumerator WaitCoroutine<T>(this T task) where T : Task => TaskWaiter(task);
        
        public static IEnumerator WaitCoroutineTimed<T>(this T task, string timerName = "") where T : Task
        {
            float timeStart = Time.time;
            return TaskWaiterThen(task, () =>
            {
                float timeDiff = Time.time - timeStart;
                string name = timerName.IsNullOrWhitespace() ? "Unknown" : timerName;
                
                Debug.Log($"'{name}' task took {timeDiff.AsTimeToPrint()}");
            });
        }

        public static IEnumerator WaitCoroutineThen<T>(this T task, Action action) where T : Task => TaskWaiterThen(task, action);

        public static IEnumerator WaitCoroutineThenUseResult<T>(this Task<T> task, Action<T> action) => TaskWaiterThen(task, action);

        static IEnumerator TaskWaiter<T>(T task, float timeout = 240)
            where T : Task
        {
            if (task == null || task.IsCompleted)
                yield break;
            
            float time = 0;
            while (time < timeout && !task.IsCompleted)
            {
                time += Time.deltaTime;
                yield return null;
            }
        }

        static IEnumerator TaskWaiterThen<T>(T task, Action action, float timeout = 240)
            where T : Task
        {
            if (task == null)
                yield break;
            yield return TaskWaiter(task, timeout);
            action?.Invoke();
        }

        static IEnumerator TaskWaiterThen<TTask, T>(TTask task, Action<T> action, float timeout = 240)
            where TTask : Task<T>
        {
            if (task == null)
                yield break;
            yield return TaskWaiter(task, timeout);
            action?.Invoke(task.Result);
        }
    }
}