namespace VLib
{
    public interface ICmdRefreshable
    {
        bool NeedsRefresh { get; }

        void SetNeedsRefresh(bool refresh);
        
        void RefreshBuffers();
    }
}