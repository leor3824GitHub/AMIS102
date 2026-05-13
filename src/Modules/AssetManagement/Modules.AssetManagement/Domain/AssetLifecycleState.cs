namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// Current lifecycle state of a physical asset in the registry.
/// </summary>
public enum AssetLifecycleState
{
    PendingReceipt = 0,
    Available = 1,
    Assigned = 2,
    InTransit = 3,
    UnderInvestigation = 4,
    Unserviceable = 5,
    Disposed = 6,
}
