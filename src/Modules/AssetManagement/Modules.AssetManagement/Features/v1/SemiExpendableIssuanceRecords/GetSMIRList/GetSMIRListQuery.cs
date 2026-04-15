using FSH.Modules.AssetManagement.Domain;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableIssuanceRecords.GetSMIRList;

public sealed record GetSMIRListQuery(
    string? Keyword,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    SMIRIssuanceType? IssuanceType,
    string? TransferredToTenantId,
    int PageNumber = 1,
    int PageSize   = 10) : IQuery<PagedSMIRListResponse>;

public sealed record PagedSMIRListResponse(
    IReadOnlyList<SMIRSummaryDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record SMIRSummaryDto(
    Guid Id,
    string SMIRNo,
    DateOnly Date,
    string IssuanceType,
    string? TransferredToTenantId,
    string? TransferredToOfficerName,
    Guid? IssuedByEmployeeId,
    int ItemCount,
    DateTimeOffset CreatedOnUtc);
