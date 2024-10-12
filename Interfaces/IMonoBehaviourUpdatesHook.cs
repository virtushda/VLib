namespace VLib
{
    public interface IMonoBehaviourUpdatesHook
    {
        public void RegisterUpdateAction(System.Action action, int order);
        public void RegisterLateUpdateAction(System.Action action, int order);
        public void RegisterFixedUpdateAction(System.Action action, int order);
        
        public void DeregisterUpdateAction(System.Action action, int order);
        public void DeregisterLateUpdateAction(System.Action action, int order);
        public void DeregisterFixedUpdateAction(System.Action action, int order);
    }
}