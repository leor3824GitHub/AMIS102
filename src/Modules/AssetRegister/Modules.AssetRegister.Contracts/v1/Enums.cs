namespace AMIS.Modules.AssetRegister.Contracts.v1;

public enum AssetType
{
    SE = 0,
    PPE = 1
}

public enum AssetCategory
{
    LowValuedSemi = 0,
    HighValuedSemi = 1,
    PPE = 2
}

public enum AssetCondition
{
    InGoodCondition = 0,
    NeedingRepair = 1,
    Unserviceable = 2
}

/// <summary>
/// Depreciation method for a PPE asset. COA GAM mandates straight-line for government;
/// the enum leaves room for future methods without reshaping the model.
/// </summary>
public enum DepreciationMethod
{
    StraightLine = 0
}

public enum LifecycleState
{
    Available = 0,
    Assigned = 1,
    UnderInvestigation = 2,
    Unserviceable = 3,
    Disposed = 4,
    /// <summary>Transferred out of this office via a posted PPEIR/SMIR. Excluded from default registry views; still resolvable by direct PropertyNo lookup for audit.</summary>
    TransferredOut = 5
}

public enum AccountabilityType
{
    SE_ICS = 0,
    PPE_PAR = 1
}

public enum AccountabilityStatus
{
    Active = 0,
    Renewed = 1,
    Returned = 2,
    Cancelled = 3,
    /// <summary>Issued but not yet accepted by the receiving employee. Editable/deletable by the
    /// issuer; assets are reserved (Assigned) but the document is not yet legally in force.
    /// Becomes <see cref="Active"/> once the accountable person accepts.</summary>
    PendingAcceptance = 4
}

public enum AccountabilityLineStatus
{
    Active = 0,
    Returned = 1,
    Lost = 2
}

public enum IssuanceReportType
{
    SMIR = 0,
    PPEIR = 1
}

/// <summary>
/// Nature of a property issuance/transfer, per the PPEIR/SMIR form field
/// "Transfer to C.O. / R.O. / P.O. / Donation / Dumping / Destruction / Sale / Others".
/// Unified across semi-expendable (SMIR) and PPE (PPEIR).
/// </summary>
public enum IssuanceNature
{
    TransferCO  = 0,
    TransferRO  = 1,
    TransferPO  = 2,
    Donation    = 3,
    Dumping     = 4,
    Destruction = 5,
    Sale        = 6,
    Others      = 7,
}

public enum PhysicalCountScope
{
    PPEOnly = 0,
    SEOnly = 1,
    Both = 2
}

public enum PhysicalCountStatus
{
    /// <summary>Ledger frozen (FrozenOnUtc set on new sessions); count recording in progress.</summary>
    Ongoing = 0,
    /// <summary>Variance review; recount-flagged entries may be re-recorded; ledger still frozen.</summary>
    Reconciled = 1,
    /// <summary>Signed off; freeze lifted.</summary>
    Closed = 2,
    /// <summary>Created with metadata (Office Order No.); recording not yet allowed, ledger not frozen.</summary>
    Draft = 3
}

public enum PhysicalCountCondition
{
    InGoodCondition = 0,
    NeedingRepair = 1,
    Unserviceable = 2,
    Missing = 3,
    FoundAtStation = 4
}

/// <summary>A movement on an asset's Property Card (COA §6.3.1.a chronological log).</summary>
public enum AssetMovementType
{
    Acquired = 0,
    Issued = 1,
    Returned = 2,
    TransferredOut = 3,
    Unserviceable = 4,
    Disposed = 5,
    Lost = 6,
    Recovered = 7
}

/// <summary>The source document a Property Card movement was derived from.</summary>
public enum MovementSource
{
    Receiving = 0,        // PPERR / SMRR
    Accountability = 1,   // PAR / ICS
    Issuance = 2,         // PPEIR / SMIR
    Unserviceable = 3,    // IIRUP / IIRUSP
    Incident = 4          // RLSDDSP
}

public enum PropertyIncidentType
{
    Lost = 0,
    Stolen = 1,
    Damaged = 2,
    Destroyed = 3
}

public enum PropertyIncidentStatus
{
    Filed = 0,
    UnderInvestigation = 1,
    Resolved = 2,
    Closed = 3
}

public enum IncidentItemResolution
{
    Pending = 0,
    Recovered = 1,
    Paid = 2,
    ReliefGranted = 3,
    Derecognized = 4
}

public enum UnserviceableReportType
{
    IIRUSP = 0,
    IIRUP = 1
}

public enum UnserviceableReportStatus
{
    Draft = 0,
    Submitted = 1,
    InspectionDone = 2,
    DisposalRecorded = 3,
    Closed = 4
}

public enum DisposalMethod
{
    Sale = 0,
    Transfer = 1,
    Destruction = 2,
    Other = 3
}

public enum ReceivingDocumentKind
{
    /// <summary>PPE Receiving Report — capital assets above the capitalization threshold.</summary>
    PPERR = 0,
    /// <summary>Supplies and Materials Receiving Report — semi-expendable.</summary>
    SMRR = 1
}

public enum ReceiptType
{
    Purchase = 0,
    Transfer = 1,
    Donation = 2,
    Other = 3
}

public enum ReturnedPropertyReceiptType
{
    /// <summary>Receipt for Returned Semi-Expendable Property — for SE_ICS accountabilities.</summary>
    RRSP = 0,
    /// <summary>Receipt for Returned Property — for PPE_PAR accountabilities.</summary>
    RRP = 1
}

public enum ReturnedPropertyReceiptStatus
{
    /// <summary>Return requested by the end-user; awaiting the property custodian to receive/accept.</summary>
    Pending = 0,
    /// <summary>Custodian accepted the return — assets flipped back to Available and the official receipt number assigned.</summary>
    Accepted = 1,
    /// <summary>Custodian rejected the return request. No asset state changed.</summary>
    Rejected = 2,
    /// <summary>Requester withdrew the pending request before the custodian acted.</summary>
    Cancelled = 3
}

