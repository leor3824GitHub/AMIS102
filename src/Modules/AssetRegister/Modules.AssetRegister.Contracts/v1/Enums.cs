namespace FSH.Modules.AssetRegister.Contracts.v1;

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

public enum LifecycleState
{
    Available = 0,
    Assigned = 1,
    UnderInvestigation = 2,
    Unserviceable = 3,
    Disposed = 4
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
    Cancelled = 3
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

public enum IssuanceReportStatus
{
    Draft = 0,
    Posted = 1
}

public enum PhysicalCountScope
{
    PPEOnly = 0,
    SEOnly = 1,
    Both = 2
}

public enum PhysicalCountStatus
{
    Ongoing = 0,
    Reconciled = 1,
    Closed = 2
}

public enum PhysicalCountCondition
{
    InGoodCondition = 0,
    NeedingRepair = 1,
    Unserviceable = 2,
    Missing = 3,
    FoundAtStation = 4
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
