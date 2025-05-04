using System;

namespace VLib.Utility
{
    /// <summary> Dispose a resource via scope. <br/>
    /// This allows you to establish a disposable resource that is 'ref'-able, but also disposable via using statement. </summary>
    public struct ScopeDisposer<T> : IDisposable
        where T : IDisposable
    {
        T disposable;
        
        public ScopeDisposer(T disposable) => this.disposable = disposable;
        
        public void Dispose() => disposable?.Dispose();
    }
    
    public static class ScopeDisposerExt
    {
        /// <summary> Captures an IDisposable reference or value and disposes it when the <see cref="ScopeDisposer{T}"/> is disposed. <br/>
        /// Be aware that value types are COPIED. </summary>
        public static ScopeDisposer<T> DisposeWithScope<T>(this T disposable) where T : IDisposable => new(disposable);
    }
}