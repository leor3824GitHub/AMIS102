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

/// <summary>
/// One dated movement on the RegSPI registry (COA Annex A.4). Exactly one of the form's column
/// groups is filled per row, selected by <see cref="TransactionType"/>. <see cref="Balance"/> is the
/// running quantity still in the custody of end-users within the sheet after this transaction:
/// +issue, +re-issue, −return; a disposal deducts only when the asset was still in custody when
/// disposed (a disposal of an already-returned asset is recorded without deducting again).
/// </summary>
public sealed record RegSpiLedgerRowDto(
    DateOnly Date,
    string ReferenceNo,
    Guid AssetRegistryId,
    string PropertyNo,
    string Description,
    int EstimatedUsefulLifeYears,
    RegSpiTransactionType TransactionType,
    int Qty,
    string? OfficeOfficer,
    int Balance,
    decimal Amount,
    string? Remarks);

/// <summary>One Annex A.4 sheet — the transaction ledger for one SE classification within a fund
/// cluster, with movement totals and the closing balance (quantity + value still with end-users).</summary>
public sealed record RegSpiClassificationGroupDto(
    string? PropertyClass,
    string ClassificationName,
    int SheetNo,
    IReadOnlyCollection<RegSpiLedgerRowDto> Rows,
    int IssuedQty,
    int ReturnedQty,
    int ReissuedQty,
    int DisposedQty,
    int BalanceQty,
    decimal BalanceAmount);

/// <summary>One fund cluster's sheets, matching the COA Annex A.4 per-sheet scoping (Fund Cluster × SE classification).</summary>
public sealed record RegSpiFundClusterGroupDto(
    string FundCluster,
    IReadOnlyCollection<RegSpiClassificationGroupDto> Classifications,
    int BalanceQty,
    decimal BalanceAmount);

public sealed record RegSpiReportDto(
    DateOnly AsOfDate,
    Guid? CustodianId,
    string? FundCluster,
    string? PropertyClass,
    IReadOnlyCollection<RegSpiFundClusterGroupDto> Groups,
    int TotalTransactions,
    int BalanceQty,
    decimal BalanceAmount);

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
    Guid? CustodianId = null,
    string? FundCluster = null,
    string? PropertyClass = null) : IQuery<RegSpiReportDto>;

/// <summary>Distinct fund clusters present on active SE-ICS accountabilities — populates the RegSPI filter dropdown.</summary>
public sealed record GetRegSpiFundClustersQuery() : IQuery<IReadOnlyList<string>>;

public sealed record GetIncidentReportDocumentQuery(Guid IncidentReportId) : IQuery<IncidentReportDocumentDto?>;

public sealed record GetUnserviceableReportDocumentQuery(Guid ReportId) : IQuery<UnserviceableReportDocumentDto?>;

// ── RSPI (Report of Semi-Expendable Property Issued — sourced from SE ICS accountabilities) ──────
//
// COA periodic listing of semi-expendable property issued to accountable officers via ICS.
// Sourced from PropertyAccountability where AccountabilityType == SE_ICS. Line/item detail and
// employee printed names come from the frozen snapshots on each line; office/department are resolved
// from MasterData at query time (not carried on the snapshot).

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

/// <summary>
/// Report of Semi-Expendable Property Issued (RSPI). Lists SE assets currently issued via ICS
/// accountabilities. <paramref name="ActiveOnly"/> (default) keeps only Active accountabilities;
/// when false it also includes Renewed. Returned/Cancelled/PendingAcceptance are always excluded.
/// </summary>
public sealed record GetRspiReportQuery(
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    AssetType? AssetType = null,
    bool ActiveOnly = true,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<RspiReportDto>;

// ── RPI (Report on Property Issued — PPE, sourced from PAR accountabilities) ─────────────────────
//
// PPE counterpart of the RSPI. Sourced from PropertyAccountability where AccountabilityType == PPE_PAR.

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

/// <summary>
/// Report on Property Issued (RPI). Lists PPE assets currently issued via PAR accountabilities
/// (Active + Renewed; Returned/Cancelled/PendingAcceptance excluded).
/// </summary>
public sealed record GetRpiReportQuery(
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<RpiReportDto>;
