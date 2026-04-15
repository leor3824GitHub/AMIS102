using FSH.Modules.AssetManagement.Domain;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableProperties.GetSemiExpendableProperties;

public sealed record GetSemiExpendablePropertiesQuery(
    string? Keyword = null,
    Guid? SemiExpendableItemId = null,
    AssetCategory? Category = null,
    PropertyStatus? Status = null,
    Guid? CurrentCustodianId = null,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedSemiExpendablePropertiesResponse>;

public sealed record PagedSemiExpendablePropertiesResponse(
    ICollection<SemiExpendablePropertySummaryDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record SemiExpendablePropertySummaryDto(
    Guid Id,
    string PropertyNo,
    Guid SemiExpendableItemId,
    string ItemCode,
    string ItemName,
    string Category,
    string? SerialNo,
    DateOnly AcquisitionDate,
    decimal UnitCost,
    string? FundCluster,
    string Status,
    Guid? CurrentCustodianId,
    string? Remarks);
