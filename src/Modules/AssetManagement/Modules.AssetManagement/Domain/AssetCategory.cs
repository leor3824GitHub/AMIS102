namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// Classification of semi-expendable property per COA Circular 2022-004 Section 4.8.
/// The threshold boundaries are governed by the active capitalization threshold configured in Master Data.
/// This value is frozen on the property at the time of receipt and only changes
/// via an explicit ReclassifyPropertiesCommand with a ReclassificationRecord audit trail.
/// </summary>
public enum AssetCategory
{
    /// <summary>
    /// Unit cost is at or below the low-value threshold (e.g., ≤ ₱5,000).
    /// Issued via ICS with SPLV-YYYY-MM-NNNN numbering.
    /// Accountability extinguished upon expiry of estimated useful life.
    /// </summary>
    LowValuedSemi = 0,

    /// <summary>
    /// Unit cost is above the low-value threshold but below the capitalization threshold
    /// (e.g., > ₱5,000 and &lt; ₱50,000).
    /// Issued via ICS with SPHV-YYYY-MM-NNNN numbering.
    /// Accountability only extinguished upon physical return (RRSP), transfer (ITR),
    /// loss/damage (RLSDDSP), or disposal (IIRUSP).
    /// </summary>
    HighValuedSemi = 1,

    /// <summary>
    /// Unit cost meets or exceeds the capitalization threshold (e.g., ≥ ₱50,000).
    /// Recorded as Property, Plant and Equipment per COA rules.
    /// Issued via PAR and tracked on the PPE ledger card.
    /// </summary>
    PPE = 2,
}
