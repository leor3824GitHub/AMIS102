using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.PPEItems.GetPPEItemById;

public sealed record GetPPEItemByIdQuery(Guid Id) : IQuery<PPEItemDetailsDto>;

public sealed record PPEItemDetailsDto(
    Guid Id,
    string PropertyCode,
    string PropertyNumber,
    string Description,
    string? SerialNumber,
    DateOnly DateAcquired,
    decimal UnitCost,
    int EstimatedUsefulLifeYears,
    string Status,
    Guid? CurrentAccountableEmployeeId,
    Guid? SourcePPERRId,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy);
