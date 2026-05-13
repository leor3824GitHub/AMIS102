namespace AMIS.Modules.AssetManagement.Domain;

/// <summary>
/// The verification outcome of a single asset during a Physical Count Session.
/// </summary>
public enum PhysicalCountEntryResult
{
    /// <summary>Asset was physically located at the station.</summary>
    Found = 0,

    /// <summary>Asset was not located during the inventory walk-through.</summary>
    NotFound = 1,

    /// <summary>
    /// Asset was found at this station but is not assigned/registered here —
    /// it may belong to another office or may be unrecorded in the books.
    /// </summary>
    FoundAtStation = 2,
}

