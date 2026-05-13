using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.Vehicle.Domain.Events;

public sealed record VehicleDecommissionedEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid VehicleId,
    string TenantId,
    string PlateNumber,
    string? CorrelationId = null
) : DomainEvent(EventId, OccurredOnUtc, CorrelationId, TenantId)
{
    public static VehicleDecommissionedEvent Create(Guid vehicleId, string tenantId, string plateNumber, string? correlationId = null)
        => new(Guid.NewGuid(), DateTimeOffset.UtcNow, vehicleId, tenantId, plateNumber, correlationId);
}

