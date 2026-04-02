using FSH.Modules.Vehicle.Contracts.v1.Maintenance;
using Mediator;

namespace FSH.Modules.Vehicle.Features.v1.Maintenance.GetMaintenanceLog;

public sealed record GetMaintenanceLogQuery(Guid LogId) : IQuery<MaintenanceLogDto>;
