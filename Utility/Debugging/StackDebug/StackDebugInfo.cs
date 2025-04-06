using System;
using Unity.Burst;

namespace VLib.Debugging
{
    /// <summary> A slice of debug information that can be manually recorded. <br/>
    /// Use with using statement. </summary>
    public struct StackDebugInfo : IDisposable
    {
        string debugInfo;

        public StackDebugInfo(string debugInfo)
        {
            this.debugInfo = debugInfo;
            StackDebug.Push(debugInfo);
        }

        [BurstDiscard]
        public void Dispose() => StackDebug.Pop();
        
        public override string ToString() => debugInfo ?? "null";
    }
}