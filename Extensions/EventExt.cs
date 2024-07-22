using System;
using UnityEngine;

namespace VLib
{
    public static class EventExt
    {
        public static void AutoInvokeEachInTryCatch(this Action @event, string eventName)
        {
            if (@event == null)
                return;
            
            foreach (var handler in @event.GetInvocationList())
            {
                try
                {
                    handler.Method.Invoke(handler.Target, null);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error invoking {eventName} on {handler.Target}, logging exception...");
                    Debug.LogException(ex);
                        
                    // Try to remove the bad delegate
                    try
                    {
                        @event -= handler as Action;
                    }
                    catch (Exception ex2)
                    {
                        Debug.LogError($"Error removing bad delegate from {eventName}, logging exception...");
                        Debug.LogException(ex2);
                    }
                }
            }
        }
    }
}