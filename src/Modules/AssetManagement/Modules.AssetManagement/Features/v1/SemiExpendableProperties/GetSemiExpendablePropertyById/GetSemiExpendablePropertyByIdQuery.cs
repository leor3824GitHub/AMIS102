using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableProperties.GetSemiExpendablePropertyById;

public sealed record GetSemiExpendablePropertyByIdQuery(Guid Id) : IQuery<SemiExpendablePropertyDetailsDto>;

public sealed record SemiExpendablePropertyDetailsDto(
    Guid Id,
    string PropertyNo,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    string UnitOfMeasure,
    int? EstimatedUsefulLifeYears,
    string Category,
    string? SerialNo,
    DateOnly AcquisitionDate,
    decimal UnitCost,
    string? FundCluster,
    string Status,
    Guid? CurrentCustodianId,
    string? Remarks,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedOnUtc,
    string? LastModifiedBy);
