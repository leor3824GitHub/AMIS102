using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.PropertyAcknowledgementReceipts.GetPARById;

public sealed record GetPARByIdQuery(Guid Id) : IQuery<PARDetailsDto>;

public sealed record PARDetailsDto(
    Guid Id,
    string PARNo,
    DateOnly Date,
    string PARType,
    Guid ReceivedFromEmployeeId,
    Guid ReceivedByEmployeeId,
    Guid ApprovedByEmployeeId,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    IReadOnlyList<PARItemDto> Items);

public sealed record PARItemDto(
    Guid Id,
    int ItemNo,
    Guid TangibleInventoryItemId,
    string PropertyNo,
    int Quantity,
    string Unit,
    string ItemDescription,
    decimal UnitCost,
    decimal TotalCost,
    int EstimatedUsefulLifeYears,
    DateOnly DateAcquired);
