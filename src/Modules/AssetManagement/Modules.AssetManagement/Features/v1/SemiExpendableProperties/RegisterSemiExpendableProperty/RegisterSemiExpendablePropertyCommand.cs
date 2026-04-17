using FSH.Modules.AssetManagement.Domain;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableProperties.RegisterSemiExpendableProperty;

public sealed record RegisterSemiExpendablePropertyCommand(
    string PropertyNo,
    Guid ItemId,
    AssetCategory Category,
    string? SerialNo,
    DateOnly AcquisitionDate,
    decimal UnitCost,
    string? FundCluster = null,
    string? Remarks = null) : ICommand<SemiExpendablePropertyDto>;

public sealed record SemiExpendablePropertyDto(
    Guid Id,
    string PropertyNo,
    Guid ItemId,
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
