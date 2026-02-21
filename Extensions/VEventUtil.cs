using System;
using UnityEngine;

namespace VLib
{
    public static class VEventUtil
    {
        /// <summary> Invokes each handler in the event in a try-catch block. <br/>
        /// If a handler throws an exception, it is removed from the event. <br/>
        /// If removeEventsAfterInvocation is true, all 'invoked' handlers are removed from the event after invocation. <br/>
        /// This method is not thread-safe. </summary>
        public static void AutoInvokeEachInTryCatch(ref Action evt, string eventName, bool removeEventsAfterInvocation)
        {
            var snapshot = evt;
            if (snapshot == null) return;

            foreach (Action handler in snapshot.GetInvocationList())
            {
                try
                {
                    handler();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error invoking {eventName} on {handler.Target}");
                    Debug.LogException(ex);

                    // remove failing handler from the real delegate
                    evt -= handler;
                }
            }

            if (removeEventsAfterInvocation)
            {
                foreach (Action handler in snapshot.GetInvocationList())
                    evt -= handler;
            }
        }
    }
}