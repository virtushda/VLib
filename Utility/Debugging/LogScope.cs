/*using System;
using UnityEngine;

namespace VLib.Debugging
{
    /// <summary> Log upon entering and exiting a scope. </summary>
    public struct LogScope : IDisposable
    {
        string scopeName;
        
        public static LogScope Enter(string scopeName)
        {
            Debug.Log($"Entering scope: {scopeName}");
            return new LogScope(scopeName);
        }
        
        LogScope(string scopeName)
        {
            this.scopeName = scopeName;
        }
        
        public void Dispose()
        {
            Debug.Log($"Leaving scope: {scopeName}");
        }
    }
}*/