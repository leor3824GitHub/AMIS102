using Mediator;

namespace AMIS.Modules.AssetRegister.Contracts.v1.Reports;

public sealed record AccountabilityReportLineDto(
    Guid LineId,
    Guid AssetRegistryId,
    string PropertyNo,
    string Description,
    string Unit,
    decimal UnitCost,
    string ItemNo,
    string? ResponsibilityCenterCode,
    int IssuedQty,
    int ReturnedQty,
    AccountabilityLineStatus LineStatus,
    DateOnly? ReturnedOn,
    AssetCondition? ReturnedConditionAtReturn);

public sealed record AccountabilityReportDto(
    Guid AccountabilityId,
    string DocumentNo,
    AccountabilityType AccountabilityType,
    AccountabilityStatus Status,
    string FundCluster,
    DateOnly IssuedOn,
    DateOnly? ExpiresOn,
    Guid IssuedByEmployeeId,
    string IssuedByName,
    string? IssuedByDesignation,
    Guid ReceivedByEmployeeId,
    string ReceivedByName,
    string? ReceivedByDesignation,
    IReadOnlyCollection<AccountabilityReportLineDto> Lines,
    int TotalIssuedQty,
    int TotalReturnedQty,
    decimal TotalAmount);

public sealed record IssuanceReportLineDocumentDto(
    Guid LineId,
    Guid AccountabilityId,
    Guid AccountabilityLineId,
    Guid AssetRegistryId,
    string PropertyNo,
    string Description,
    string Unit,
    string? ResponsibilityCenterCode,
    int QuantityIssued,
    decimal UnitCost,
    decimal Amount);

public sealed record IssuanceReportDocumentDto(
    Guid ReportId,
    string ReportNo,
    IssuanceReportType ReportType,
    IssuanceReportStatus Status,
    string FundCluster,
    DateOnly PeriodFromDate,
    DateOnly PeriodToDate,
    Guid PreparedByEmployeeId,
    string PreparedByName,
    string? PreparedByDesignation,
    Guid? CertifiedByEmployeeId,
    string? CertifiedByName,
    string? CertifiedByDesignation,
    Guid? PostedByEmployeeId,
    string? PostedByName,
    string? PostedByDesignation,
    DateOnly? PostedOn,
    IReadOnlyCollection<IssuanceReportLineDocumentDto> Lines,
    decimal TotalAmount);

public sealed record PhysicalCountReportEntryDto(
    Guid EntryId,
    Guid? AssetRegistryId,
    string? PropertyNo,
    AssetType? AssetType,
    string Article,
    string Unit,
    decimal UnitCost,
    PhysicalCountCondition Condition,
    Guid LocationId,
    DateTimeOffset? ScannedOnUtc,
    Guid? ScannedByEmployeeId,
    string? Remarks);

public sealed record PhysicalCountReportDto(
    Guid SessionId,
    string Code,
    PhysicalCountScope Scope,
    PhysicalCountStatus Status,
    string FundCluster,
    DateOnly AsAt,
    DateOnly StartedOn,
    DateOnly? ClosedOn,
    IReadOnlyCollection<PhysicalCountReportEntryDto> Entries,
    int TotalEntries,
    int TotalMissing,
    int TotalUnserviceable,
    int TotalFoundAtStation,
    decimal TotalBookValue);

public sealed record RegSpiRowDto(
    Guid AccountabilityId,
    string DocumentNo,
    DateOnly IssuedOn,
    Guid CustodianId,
    string CustodianName,
    string CustodianDesignation,
    Guid AccountabilityLineId,
    Guid AssetRegistryId,
    string PropertyNo,
    string Description,
    AssetType AssetType,
    string Unit,
    decimal UnitCost,
    int Quantity,
    decimal Amount,
    string? ResponsibilityCenterCode);

public sealed record RegSpiReportDto(
    DateOnly AsOfDate,
    AssetType? AssetType,
    Guid? CustodianId,
    IReadOnlyCollection<RegSpiRowDto> Rows,
    int TotalItems,
    decimal TotalAmount);

public sealed record IncidentReportItemDocumentDto(
    Guid ItemId,
    Guid AssetRegistryId,
    string PropertyNo,
    string Description,
    AssetType AssetType,
    decimal AcquisitionCost,
    decimal CurrentReplacementCost,
    IncidentItemResolution Resolution,
    DateOnly? ResolvedOn);

public sealed record IncidentReportDocumentDto(
    Guid IncidentReportId,
    string IncidentNo,
    PropertyIncidentType IncidentType,
    PropertyIncidentStatus Status,
    DateOnly IncidentDate,
    string FundCluster,
    string DepartmentOffice,
    string Circumstances,
    Guid AccountableOfficerId,
    string AccountableOfficerName,
    string AccountableOfficerDesignation,
    IReadOnlyCollection<IncidentReportItemDocumentDto> Items,
    decimal TotalAcquisitionCost,
    decimal TotalCurrentReplacementCost);

public sealed record UnserviceableReportItemDocumentDto(
    Guid ItemId,
    Guid AssetRegistryId,
    string PropertyNo,
    string Description,
    AssetType AssetType,
    DateOnly DateAcquired,
    decimal AcquisitionCost,
    decimal AccumulatedDepreciation,
    decimal AccumulatedImpairmentLosses,
    decimal CarryingAmount,
    DisposalMethod? DisposalMethod,
    DateOnly? DisposalRecordedOn,
    string? SaleORNo,
    decimal? SaleAmount,
    string? Remarks);

public sealed record UnserviceableReportDocumentDto(
    Guid ReportId,
    string ReportNo,
    UnserviceableReportType ReportType,
    UnserviceableReportStatus Status,
    DateOnly AsAt,
    string FundCluster,
    string Station,
    Guid AccountableOfficerId,
    string AccountableOfficerName,
    string AccountableOfficerDesignation,
    IReadOnlyCollection<UnserviceableReportItemDocumentDto> Items,
    decimal TotalCarryingAmount);

public sealed record GetAccountabilityReportQuery(Guid AccountabilityId) : IQuery<AccountabilityReportDto?>;

public sealed record GetIssuanceReportDocumentQuery(Guid ReportId) : IQuery<IssuanceReportDocumentDto?>;

public sealed record GetPhysicalCountReportQuery(Guid SessionId, AssetType? AssetType = null) : IQuery<PhysicalCountReportDto?>;

public sealed record GetRegSpiReportQuery(
    DateOnly? AsOfDate = null,
    AssetType? AssetType = null,
    Guid? CustodianId = null) : IQuery<RegSpiReportDto>;

public sealed record GetIncidentReportDocumentQuery(Guid IncidentReportId) : IQuery<IncidentReportDocumentDto?>;

public sealed record GetUnserviceableReportDocumentQuery(Guid ReportId) : IQuery<UnserviceableReportDocumentDto?>;
