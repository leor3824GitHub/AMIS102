using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.AssetRegistryQueries.GetAssetsByCustodian;

public sealed record GetAssetsByCustodianQuery(
    Guid CustodianId,
    string? Keyword,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<PagedAssetsByCustodianResponse>;

public sealed record PagedAssetsByCustodianResponse(
    IReadOnlyList<AssetRegistryListItemDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record AssetRegistryListItemDto(
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