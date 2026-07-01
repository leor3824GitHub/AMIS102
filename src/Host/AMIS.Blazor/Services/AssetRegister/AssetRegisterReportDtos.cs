using AMIS.Modules.AssetRegister.Contracts.v1;

namespace AMIS.Blazor.Services.AssetRegister;

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

public sealed record IssuanceReportLineDocumentDto(
    Guid LineId,
    Guid AssetRegistryId,
    int ItemNo,
    string PropertyNo,
    string Description,
    string Unit,
    decimal UnitCost,
    decimal Amount,
    decimal? AccumulatedDepreciation,
    decimal? BookValue);

public sealed record IssuanceReportDocumentDto(
    Guid ReportId,
    string ReportNo,
    IssuanceReportType ReportType,
    IssuanceNature Nature,
    string FundCluster,
    DateOnly Date,
    Guid IssuedByEmployeeId,
    string IssuedByName,
    string? IssuedByDesignation,
    Guid ApprovedByEmployeeId,
    string ApprovedByName,
    string? ApprovedByDesignation,
    Guid IssuedToEmployeeId,
    string IssuedToName,
    string? IssuedToDesignation,
    string IssuedToOfficeAddress,
    string? Remarks,
    IReadOnlyCollection<IssuanceReportLineDocumentDto> Lines,
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

// ── RSPI (Report of Semi-Expendable Property Issued — SE, via ICS) ────────────────────────────────
public sealed record RspiRowDto(
    Guid AccountabilityId,
    string DocumentNo,
    DateOnly IssuedOn,
    AccountabilityStatus Status,
    string FundCluster,
    DateOnly? ExpiresOn,
    Guid ReceivedByEmployeeId,
    string ReceivedByName,
    string? ReceivedByDesignation,
    string? ReceivedByOfficeName,
    Guid IssuedByEmployeeId,
    string IssuedByName,
    string? IssuedByDesignation,
    string? IssuedByOfficeName,
    Guid AssetRegistryId,
    string PropertyNo,
    string Description,
    AssetType AssetType,
    string Unit,
    decimal UnitCost,
    int Quantity,
    decimal Amount);

public sealed record RspiReportDto(
    DateOnly? DateFrom,
    DateOnly? DateTo,
    AssetType? AssetType,
    bool ActiveOnly,
    IReadOnlyList<RspiRowDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    decimal OverallAmountTotal);

// ── RPI (Report on Property Issued — PPE, via PAR) ────────────────────────────────────────────────
public sealed record RpiRowDto(
    Guid AccountabilityId,
    string DocumentNo,
    DateOnly IssuedOn,
    AccountabilityStatus Status,
    string FundCluster,
    DateOnly? ExpiresOn,
    Guid ReceivedByEmployeeId,
    string ReceivedByName,
    string? ReceivedByDesignation,
    string? ReceivedByOfficeName,
    Guid IssuedByEmployeeId,
    string IssuedByName,
    string? IssuedByDesignation,
    string? IssuedByOfficeName,
    Guid AssetRegistryId,
    string PropertyNo,
    string Description,
    string Unit,
    int Quantity,
    decimal UnitCost,
    decimal Amount,
    int EstimatedUsefulLifeYears,
    DateOnly DateAcquired);

public sealed record RpiReportDto(
    DateOnly? DateFrom,
    DateOnly? DateTo,
    IReadOnlyList<RpiRowDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    decimal OverallAmountTotal);
