using System;

namespace VLib
{
    public interface IVIdentityProvider<T> : ILockable
        where T : unmanaged, IComparable<T>, IEquatable<T>
    {
        T FetchID();
        void ReturnID(T returnedID);
        bool TryClaimID(T id);
    }
    
    // Set of extension methods for IVIdentityProvider<T> using ILockable to wrap all interface methods in a lock
    public static class VIdentityProviderExtensions
    {
        public static T FetchIDThreadSafe<T>(this IVIdentityProvider<T> provider)
            where T : unmanaged, IComparable<T>, IEquatable<T>
        {
            lock (provider.LockObj)
            {
                return provider.FetchID();
            }
        }
        
        public static void ReturnIDThreadSafe<T>(this IVIdentityProvider<T> provider, T returnedID)
            where T : unmanaged, IComparable<T>, IEquatable<T>
        {
            lock (provider.LockObj)
            {
                provider.ReturnID(returnedID);
            }
        }
        
        public static bool TryClaimIDThreadSafe<T>(this IVIdentityProvider<T> provider, T id)
            where T : unmanaged, IComparable<T>, IEquatable<T>
        {
            lock (provider.LockObj)
            {
                return provider.TryClaimID(id);
            }
        }
    }
}