namespace AMIS.Modules.AssetManagement.Domain;

/// <summary>
/// Event type recorded in <see cref="AssetAssignmentHistory"/>.
/// </summary>
public enum AssetAssignmentEventType
{
    Assigned = 0,
    Returned = 1,
    Transferred = 2,
    StatusChanged = 3,
}

