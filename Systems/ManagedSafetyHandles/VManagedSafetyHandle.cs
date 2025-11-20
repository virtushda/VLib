using System;
using Unity.Burst;
using UnityEngine.Assertions;

namespace VLib
{
    /// <summary>Managed, pooled safety handle for robust, scope-checked usage of disposable resources.</summary>
    public class VManagedSafetyHandle
    {
        static long nextID = -long.MaxValue;

        /// <summary>Atomically increments and returns the next unique identifier.</summary>
        static ulong GetNextID() => nextID.IncrementToUlong();

        static ConcurrentVPool<VManagedSafetyHandle> pool = new(
            creationAction: () => new VManagedSafetyHandle(),
            depoolPostProcess: handle => handle.ID = GetNextID(),
            repoolPreProcess: handle => handle.ID = 0,
            disposalAction: handle => handle.ID = 0);

        /// <summary>Unique identifier for this handle instance.</summary>
        ulong ID { get; set; }

        /// <summary>Allocates and returns a scoped user token for this handle.</summary>
        public static User AllocateUser()
        {
#if ENABLE_PROFILER
            using var profileScope = ProfileScope.Auto("Allocate VManagedSafetyHandle");
#endif
            return new User(pool.Depool());
        }

        /// <summary>Releases this handle back to the pool (called by <see cref="User.Dispose"/>).</summary>
        void Release()
        {
#if ENABLE_PROFILER
            using var profileScope = ProfileScope.Auto("Release VManagedSafetyHandle");
#endif
            pool.Repool(this);
        }

        /// <summary>Scope token for a pooled <see cref="VManagedSafetyHandle"/>. Ensures concurrent-safe, single-use disposal.</summary>
        public readonly struct User : IEquatable<User>, IDisposable
        {
            readonly VManagedSafetyHandle handle;
            readonly ulong id;

            /// <summary>True if this token is valid and refers to an active handle.</summary>
            public bool IsValid => handle != null && handle.ID == id;
            public static implicit operator bool(in User user) => user.IsValid;

            /// <summary>Creates a user token for the given handle. Use <see cref="AllocateUser"/> for allocation.</summary>
            internal User(VManagedSafetyHandle handle)
            {
                this.handle = handle;
                Assert.IsTrue(handle != null, "Handle is null!");
                Assert.IsTrue(handle.ID != 0, "Handle ID is 0!");
                id = handle.ID;
            }

            /// <summary>Releases the associated handle if valid (idempotent).</summary>
            public void Dispose()
            {
                if (IsValid)
                    handle.Release();
            }

            [BurstDiscard] public bool Equals(User other) => id == other.id;
            [BurstDiscard] public override bool Equals(object obj) => obj is User other && Equals(other);
            [BurstDiscard] public override int GetHashCode() => id.GetHashCode();

            public static bool operator ==(User left, User right) => left.Equals(right);
            public static bool operator !=(User left, User right) => !left.Equals(right);
        }
    }
}