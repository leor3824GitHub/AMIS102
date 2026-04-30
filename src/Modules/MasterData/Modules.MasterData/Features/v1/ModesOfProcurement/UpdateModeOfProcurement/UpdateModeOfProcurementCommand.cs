using Mediator;

namespace FSH.Modules.MasterData.Features.v1.ModesOfProcurement.UpdateModeOfProcurement;

public sealed record UpdateModeOfProcurementCommand(
    Guid Id,
    string Name,
    string? Description = null,
    bool IsActive = true) : ICommand;
