using FSH.Modules.AssetManagement.Domain;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.PPEIssuanceReports.GetPPEIRList;

public sealed record GetPPEIRListQuery(
    string? Keyword,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    PPEIssuanceType? IssuanceType,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedPPEIRResponse>;

public sealed record PagedPPEIRResponse(
    IReadOnlyList<PPEIRSummaryDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record PPEIRSummaryDto(
    Guid Id,
    string PPEIRNo,
    DateOnly Date,
    Guid IssuedToEmployeeId,
    string IssuanceType,
    int ItemCount);
