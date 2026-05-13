using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.AssetRegistryQueries.GetAssetsByLocation;

public sealed record GetAssetsByLocationQuery(
    Guid LocationId,
    string? Keyword,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<PagedAssetsByLocationResponse>;

public sealed record PagedAssetsByLocationResponse(
    IReadOnlyList<AssetRegistryByLocationDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record AssetRegistryByLocationDto(
    Guid RegistryId,
    Guid TangibleInventoryItemId,
    string PropertyNo,
    string AssetType,
    string LifecycleState,
    string? CurrentPropertyStatus,
    Guid? CurrentCustodianId,
    Guid? CurrentLocationId,
    string? LocationName,
    DateOnly AcquisitionDate,
    decimal UnitCost);