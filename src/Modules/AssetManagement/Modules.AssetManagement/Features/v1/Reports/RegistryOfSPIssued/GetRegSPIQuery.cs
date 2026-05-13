using AMIS.Modules.AssetManagement.Domain;
using Mediator;

namespace AMIS.Modules.AssetManagement.Features.v1.Reports.RegistryOfSPIssued;

/// <summary>
/// Generates a Registry of Semi-Expendable Property Issued (RegSPI) for a specific employee.
/// Shows all ICS records (current and historical) for that employee, with per-line property detail.
/// </summary>
public sealed record GetRegSPIQuery(
    Guid EmployeeId,
    AssetType? AssetType,
    ICSStatus? Status,
    int PageNumber = 1,
    int PageSize   = 20) : IQuery<PagedRegSPIResponse>;

public sealed record PagedRegSPIResponse(
    Guid EmployeeId,
    string? EmployeeNumber,
    string EmployeeName,
    string? EmployeeOfficeName,
    string? EmployeeDepartmentName,
    string? EmployeePositionName,
    IReadOnlyList<RegSPISignatoryDto> Signatories,
    IReadOnlyList<RegSPISectionDto> Sections,
    IReadOnlyList<RegSPIEntryDto> Items,
    int PageLineCount,
    decimal PageAmountTotal,
    int PageNumber,
    int PageSize,
    int TotalCount,
    decimal OverallAmountTotal);

public sealed record RegSPISignatoryDto(
    int SortOrder,
    string Label,
    string Name,
    string Title);

public sealed record RegSPISectionDto(
    Guid ICSId,
    string ICSNo,
    DateOnly Date,
    string? FundCluster,
    string ICSStatus,
    int LineCount,
    decimal AmountTotal);

public sealed record RegSPIEntryDto(
    Guid ICSId,
    string ICSNo,
    DateOnly Date,
    string? FundCluster,
    Guid TangibleInventoryItemId,
    string PropertyNo,
    string ItemCode,
    string ItemName,
    string AssetType,
    decimal UnitCost,
    int? EstimatedUsefulLifeYears,
    DateOnly? ExpiresOn,
    string ICSStatus,
    Guid? IssuedFromEmployeeId,
    string? IssuedFromEmployeeName,
    string? IssuedFromEmployeePositionName,
    string? IssuedFromEmployeeOfficeName);

