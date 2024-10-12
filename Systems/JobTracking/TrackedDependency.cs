namespace VLib
{
    public readonly struct TrackedDependency
    {
        public readonly ulong id;
        public readonly bool writeAccess;

        public TrackedDependency(ulong id, bool writeAccess)
        {
            this.id = id;
            this.writeAccess = writeAccess;
        }
    }
}