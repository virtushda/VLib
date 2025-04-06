using System;

namespace VLib.Debugging
{
    /// <summary> If you throw one of these, it will automatically include the relevant stack debug info. </summary>
    public class StackDebugException : Exception
    {
        internal StackDebugException(string message, Exception innerException) : base(message, innerException) { }
    }
    
    public static class StackDebugExceptionExtensions
    {
        /// <summary> Wrap the exception in a StackDebugException, which will include the relevant stack debug info, if any. </summary>
        public static void RethrowWithStackDebug<T>(this T e)
            where T : Exception =>
            throw new StackDebugException("StackDebug Output:\n" + StackDebug.GetStackInfoForCurrentThread(), e);
    }
}