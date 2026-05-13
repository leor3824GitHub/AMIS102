using Mediator;

namespace AMIS.Modules.MasterData.Features.v1.ModesOfProcurement.CreateModeOfProcurement;

public sealed record CreateModeOfProcurementCommand(
    string Name,
    string? Description = null) : ICommand<ModeOfProcurementDto>;

public sealed record ModeOfProcurementDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive);

