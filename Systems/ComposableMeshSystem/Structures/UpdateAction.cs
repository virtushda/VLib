/// <summary>
/// Command for enqueuing mesh updates. Processed in managed code, but could be extended for job processing.
/// </summary>
public enum UpdateAction
{
    AddInstance,
    UpdateInstance,
    RemoveInstance
}