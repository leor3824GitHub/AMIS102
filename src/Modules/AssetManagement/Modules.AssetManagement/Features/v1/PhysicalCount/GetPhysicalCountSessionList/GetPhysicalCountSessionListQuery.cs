using FSH.Modules.AssetManagement.Domain;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.PhysicalCount.GetPhysicalCountSessionList;

public sealed record GetPhysicalCountSessionListQuery(
    string? Keyword,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    PhysicalCountStatus? Status,
    PhysicalCountScope? Scope,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedPhysicalCountSessionResponse>;

public sealed record PagedPhysicalCountSessionResponse(
    IReadOnlyList<PhysicalCountSessionSummaryDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record PhysicalCountSessionSummaryDto(
    Guid Id,
    string SessionNo,
    DateOnly CountDate,
    string StationOffice,
    string Scope,
    string Status,
    int TotalEntries,
    int Found,
    int NotFound,
    int FoundAtStation,
    int Pending,
    DateTimeOffset CreatedOnUtc);
