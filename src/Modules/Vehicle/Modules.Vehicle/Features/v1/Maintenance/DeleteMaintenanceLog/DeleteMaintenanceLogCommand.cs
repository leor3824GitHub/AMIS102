using Mediator;

namespace AMIS.Modules.Vehicle.Features.v1.Maintenance.DeleteMaintenanceLog;

public sealed record DeleteMaintenanceLogCommand(Guid LogId) : ICommand<Unit>;

