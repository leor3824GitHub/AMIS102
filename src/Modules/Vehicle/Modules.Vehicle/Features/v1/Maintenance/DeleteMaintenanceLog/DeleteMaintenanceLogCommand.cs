using Mediator;

namespace FSH.Modules.Vehicle.Features.v1.Maintenance.DeleteMaintenanceLog;

public sealed record DeleteMaintenanceLogCommand(Guid LogId) : ICommand<Unit>;
