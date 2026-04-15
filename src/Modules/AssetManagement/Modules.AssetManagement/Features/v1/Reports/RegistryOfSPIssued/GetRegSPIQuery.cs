using FSH.Modules.AssetManagement.Domain;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.Reports.RegistryOfSPIssued;

/// <summary>
/// Generates a Registry of Semi-Expendable Property Issued (RegSPI) for a specific employee.
/// Shows all ICS records (current and historical) for that employee,
/// with per-line property detail.
/// </summary>
public sealed record GetRegSPIQuery(
    Guid EmployeeId,
    AssetCategory? Category,
    ICSStatus? Status,
    int PageNumber = 1,
    int PageSize   = 20) : IQuery<PagedRegSPIResponse>;

public sealed record PagedRegSPIResponse(
    Guid EmployeeId,
    IReadOnlyList<RegSPIEntryDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record RegSPIEntryDto(
    Guid ICSId,
    string ICSNo,
    DateOnly Date,
    string? FundCluster,
    Guid PropertyId,
    string PropertyNo,
    string ItemCode,
    string ItemName,
    string Category,
    decimal UnitCost,
    int? EstimatedUsefulLifeYears,
    DateOnly? ExpiresOn,
    string ICSStatus);
