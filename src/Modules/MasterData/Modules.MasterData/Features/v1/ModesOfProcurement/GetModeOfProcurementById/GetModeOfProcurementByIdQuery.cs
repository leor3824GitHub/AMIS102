using Mediator;

namespace AMIS.Modules.MasterData.Features.v1.ModesOfProcurement.GetModeOfProcurementById;

public sealed record GetModeOfProcurementByIdQuery(Guid Id) : IQuery<ModeOfProcurementDetailsDto>;

public sealed record ModeOfProcurementDetailsDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedOnUtc,
    string? LastModifiedBy);

