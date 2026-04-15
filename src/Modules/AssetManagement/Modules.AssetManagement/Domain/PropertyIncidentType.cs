namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// Nature of the incident documented in the RLSDDSP
/// (Report of Lost/Stolen/Damaged/Destroyed Semi-Expendable Property).
/// </summary>
public enum PropertyIncidentType
{
    /// <summary>Property cannot be located.</summary>
    Lost = 0,

    /// <summary>Property was taken by an unauthorized person.</summary>
    Stolen = 1,

    /// <summary>Property is physically damaged and no longer serviceable.</summary>
    Damaged = 2,

    /// <summary>Property was physically destroyed (fire, flood, calamity, etc.).</summary>
    Destroyed = 3,
}
