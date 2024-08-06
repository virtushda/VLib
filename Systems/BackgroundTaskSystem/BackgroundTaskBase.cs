using UnityEngine;

namespace VLib
{
    /// <summary> Inherit from this to create a work task. Prepare it however you like and then call the Submit function. </summary>
    public class BackgroundTaskBase
    {
        /// <summary> How much work is this? </summary>
        public virtual BackgroundTaskSize Size { get; } = BackgroundTaskSize.Small;
        public BackgroundTaskState State { get; internal set; }

        /// <summary> Can be called from any thread. </summary>
        public void Schedule()
        {
            if (State != BackgroundTaskState.NotInSystem)
            {
                Debug.LogError("Task already in system.");
                return;
            }
            if (BackgroundTaskSystem.instance == null)
                throw new System.Exception("BackgroundTaskSystem not initialized.");
            BackgroundTaskSystem.instance.SubmitWork(this);
        }
        
        /// <summary> Runs within a job automatically. </summary>
        internal virtual void Execute() { }

        /// <summary> Is run on the main thread after the system has determined the work is complete. </summary>
        internal void Complete()
        {
            OnComplete();

            // Reset
            //TaskID = 0;
            State = BackgroundTaskState.NotInSystem;
        }

        /// <summary> Is run on the main thread by <see cref="Complete"/> </summary>
        protected virtual void OnComplete() { }
    }
}