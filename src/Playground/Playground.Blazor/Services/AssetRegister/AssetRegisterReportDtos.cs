namespace AMIS.Playground.Blazor.Services.AssetRegister;

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
    int LineStatus,
    DateOnly? ReturnedOn,
    int? ReturnedConditionAtReturn);

public sealed record AccountabilityReportDto(
    Guid AccountabilityId,
    string DocumentNo,
    int AccountabilityType,
    int Status,
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
    int AssetType,
    string Unit,
    decimal UnitCost,
    int Quantity,
    decimal Amount,
    string? ResponsibilityCenterCode);

public sealed record RegSpiReportDto(
    DateOnly AsOfDate,
    int? AssetType,
    Guid? CustodianId,
    IReadOnlyCollection<RegSpiRowDto> Rows,
    int TotalItems,
    decimal TotalAmount);

public sealed record PhysicalCountReportEntryDto(
    Guid EntryId,
    Guid? AssetRegistryId,
    string? PropertyNo,
    int? AssetType,
    string Article,
    string Unit,
    decimal UnitCost,
    int Condition,
    Guid LocationId,
    DateTimeOffset? ScannedOnUtc,
    Guid? ScannedByEmployeeId,
    string? Remarks);

public sealed record PhysicalCountReportDto(
    Guid SessionId,
    string Code,
    int Scope,
    int Status,
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
    int ReportType,
    int Status,
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

public sealed record IncidentReportItemDocumentDto(
    Guid ItemId,
    Guid AssetRegistryId,
    string PropertyNo,
    string Description,
    int AssetType,
    decimal AcquisitionCost,
    decimal CurrentReplacementCost,
    int Resolution,
    DateOnly? ResolvedOn);

public sealed record IncidentReportDocumentDto(
    Guid IncidentReportId,
    string IncidentNo,
    int IncidentType,
    int Status,
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
    int AssetType,
    DateOnly DateAcquired,
    decimal AcquisitionCost,
    decimal AccumulatedDepreciation,
    decimal AccumulatedImpairmentLosses,
    decimal CarryingAmount,
    int? DisposalMethod,
    DateOnly? DisposalRecordedOn,
    string? SaleORNo,
    decimal? SaleAmount,
    string? Remarks);

public sealed record UnserviceableReportDocumentDto(
    Guid ReportId,
    string ReportNo,
    int ReportType,
    int Status,
    DateOnly AsAt,
    string FundCluster,
    string Station,
    Guid AccountableOfficerId,
    string AccountableOfficerName,
    string AccountableOfficerDesignation,
    IReadOnlyCollection<UnserviceableReportItemDocumentDto> Items,
    decimal TotalCarryingAmount);

