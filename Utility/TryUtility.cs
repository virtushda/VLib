using System;
using UnityEngine;

namespace VLib
{
    /// <summary>Utility for burying try-catch-finally boilerplate. <br/>
    /// To be used only where GC-pressure is not a concern. </summary>
    public static class Try
    {
        /// <summary>Invokes passed in action inside a try block and logs any exception on failure (by default).</summary>
        /// <param name="action">Method to execute.</param>
        /// <param name="logException">True is default, false suppresses exceptions.</param>
        /// <returns>True if code executed without failure.</returns>
        public static bool Catch(Action action, bool logException = true)
        {
            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
                if (logException)
                {
                    Debug.LogError($"Error invoking {action.Method.Name}, logging exception!");
                    Debug.LogException(e);
                }
                return false;
            }
            return true;
        }
        
        /// <summary>Invokes passed in action inside a try block and logs any exception on failure (by default).</summary>
        /// <param name="action">Method to execute.</param>
        /// <param name="logException">True is default, false suppresses exceptions.</param>
        /// <returns>True if code executed without failure.</returns>
        public static bool TryCatch(this Action action, bool logException = true) => Catch(action, logException);
    }
}