using FSH.Modules.AssetManagement.Domain;
using FSH.Modules.AssetManagement.Features.v1.Locations;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.Locations.GetLocations;

public sealed record GetLocationsQuery(
    string? Keyword,
    LocationType? Type,
    Guid? ParentLocationId,
    int PageNumber = 1,
    int PageSize = 50) : IQuery<PagedLocationsResponse>;

public sealed record PagedLocationsResponse(
    IReadOnlyList<LocationDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);