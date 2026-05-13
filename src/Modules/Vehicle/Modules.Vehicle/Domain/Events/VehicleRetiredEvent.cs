using FSH.Framework.Core.Domain;

namespace FSH.Modules.Vehicle.Domain.Events;

public sealed record VehicleRetiredEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid VehicleId,
    string TenantId,
    string PlateNumber,
    string? CorrelationId = null
) : DomainEvent(EventId, OccurredOnUtc, CorrelationId, TenantId)
{
    public static VehicleRetiredEvent Create(Guid vehicleId, string tenantId, string plateNumber, string? correlationId = null)
        => new(Guid.NewGuid(), DateTimeOffset.UtcNow, vehicleId, tenantId, plateNumber, correlationId);
}
