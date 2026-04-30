using Mediator;

namespace FSH.Modules.MasterData.Features.v1.ModesOfProcurement.DeleteModeOfProcurement;

public sealed record DeleteModeOfProcurementCommand(Guid Id) : ICommand;
