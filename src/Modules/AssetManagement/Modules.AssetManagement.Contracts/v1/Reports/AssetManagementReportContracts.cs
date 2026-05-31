using Mediator;

namespace AMIS.Modules.AssetManagement.Contracts.v1.Reports;

// ── Enums ────────────────────────────────────────────────────────────────────

public enum AssetType
{
    SE  = 0,
    PPE = 1,
}

public enum ICSStatus
{
    Active            = 0,
    Renewed           = 1,
    CancelledByReturn = 2,
    Expired           = 3,
}

// ── RSPI ─────────────────────────────────────────────────────────────────────

public sealed record GetRSPIQuery(
    DateOnly? DateFrom,
    DateOnly? DateTo,
    AssetType? AssetType,
    bool ActiveOnly = true,
    int PageNumber  = 1,
    int PageSize    = 20) : IQuery<PagedRSPIResponse>;

public sealed record PagedRSPIResponse(
    IReadOnlyList<RSPISignatoryDto> Signatories,
    IReadOnlyList<RSPISectionDto>   Sections,
    IReadOnlyList<RSPIItemDto>      Items,
    int     PageLineCount,
    decimal PageAmountTotal,
    int     PageNumber,
    int     PageSize,
    int     TotalCount,
    decimal OverallAmountTotal);

public sealed record RSPISignatoryDto(int SortOrder, string Label, string Name, string Title);

public sealed record RSPISectionDto(
    Guid     ICSId,
    string   ICSNo,
    DateOnly ICSDate,
    string?  FundCluster,
    string   ICSStatus,
    int      LineCount,
    decimal  AmountTotal);

public sealed record RSPIItemDto(
    Guid    ICSId,
    string  ICSNo,
    DateOnly ICSDate,
    string  ICSStatus,
    string? FundCluster,
    Guid    ReceivedByEmployeeId,
    string  ReceivedByEmployeeName,
    string? ReceivedByEmployeePositionName,
    string? ReceivedByEmployeeOfficeName,
    Guid?   IssuedFromEmployeeId,
    string? IssuedFromEmployeeName,
    string? IssuedFromEmployeePositionName,
    string? IssuedFromEmployeeOfficeName,
    Guid    TangibleInventoryItemId,
    string  PropertyNo,
    string  ItemCode,
    string  ItemName,
    string  AssetType,
    decimal UnitCost,
    DateOnly? ExpiresOn);

// ── RegSPI ────────────────────────────────────────────────────────────────────

public sealed record GetRegSPIQuery(
    Guid       EmployeeId,
    AssetType? AssetType,
    ICSStatus? Status,
    int        PageNumber = 1,
    int        PageSize   = 20) : IQuery<PagedRegSPIResponse>;

public sealed record PagedRegSPIResponse(
    Guid    EmployeeId,
    string? EmployeeNumber,
    string  EmployeeName,
    string? EmployeeOfficeName,
    string? EmployeeDepartmentName,
    string? EmployeePositionName,
    IReadOnlyList<RegSPISignatoryDto> Signatories,
    IReadOnlyList<RegSPISectionDto>   Sections,
    IReadOnlyList<RegSPIEntryDto>     Items,
    int     PageLineCount,
    decimal PageAmountTotal,
    int     PageNumber,
    int     PageSize,
    int     TotalCount,
    decimal OverallAmountTotal);

public sealed record RegSPISignatoryDto(int SortOrder, string Label, string Name, string Title);

public sealed record RegSPISectionDto(
    Guid     ICSId,
    string   ICSNo,
    DateOnly Date,
    string?  FundCluster,
    string   ICSStatus,
    int      LineCount,
    decimal  AmountTotal);

public sealed record RegSPIEntryDto(
    Guid     ICSId,
    string   ICSNo,
    DateOnly Date,
    string?  FundCluster,
    Guid     TangibleInventoryItemId,
    string   PropertyNo,
    string   ItemCode,
    string   ItemName,
    string   AssetType,
    decimal  UnitCost,
    int?     EstimatedUsefulLifeYears,
    DateOnly? ExpiresOn,
    string   ICSStatus,
    Guid?    IssuedFromEmployeeId,
    string?  IssuedFromEmployeeName,
    string?  IssuedFromEmployeePositionName,
    string?  IssuedFromEmployeeOfficeName);

// ── PDF Commands ──────────────────────────────────────────────────────────────

public sealed record GenerateRSPIPdfCommand(
    DateOnly? DateFrom,
    DateOnly? DateTo,
    AssetType? AssetType,
    bool ActiveOnly = true,
    int PageNumber  = 1,
    int PageSize    = 1000) : ICommand<byte[]>;

public sealed record GenerateRegSPIPdfCommand(
    Guid       EmployeeId,
    AssetType? AssetType,
    ICSStatus? Status,
    int        PageNumber = 1,
    int        PageSize   = 1000) : ICommand<byte[]>;
