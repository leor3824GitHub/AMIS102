namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// Lifecycle status of a Physical Count Session.
/// </summary>
public enum PhysicalCountStatus
{
    /// <summary>Session created; inventory walk-through in progress.</summary>
    Open = 0,

    /// <summary>Session submitted; RPCPPE and ICF generated. No further entries allowed.</summary>
    Submitted = 1,
}
