using System;
using Unity.Jobs;

namespace VLib
{
    public struct JobHandleSortable : IUniqueID16U, IEquatable<JobHandleSortable>, IComparable<JobHandleSortable>, IDisposable
    {
        static VIdentityProviderUInt16 identity16;
        static VIdentityProviderUInt16 Identity16 => identity16 ??= new VIdentityProviderUInt16();

        ushort id;
        public ushort UniqueID
        {
            readonly get => id;
            set => throw new NotSupportedException("Setting the ID manually is not supported.");
        }

        public JobHandle handle;

        public bool IsCompleted => handle.IsCompleted;
        public void CompleteOnly() => handle.Complete();

        public JobHandleSortable(ref JobHandle handle)
        {
            this.handle = handle;
            
            // Fetch ID threadsafe
            identity16 = Identity16;
            lock (identity16)
            {
                id = identity16.FetchID();
            }
        }

        public void CompleteAndDispose()
        {
            handle.Complete();
            Dispose();
        }

        public int CompareTo(JobHandleSortable other) => UniqueID.CompareTo(other.UniqueID);

        public int CompareTo(ushort other) => UniqueID.CompareTo(other);

        public bool Equals(JobHandleSortable other) => UniqueID.Equals(other.UniqueID);

        public void Dispose()
        {
            // Dispose ID threadsafe
            identity16 = Identity16;
            lock (identity16)
            {
                identity16.ReturnID(id);
            }
        }
    }
}