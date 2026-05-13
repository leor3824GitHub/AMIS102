using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.Vehicle.Domain.Events;

public sealed record VehicleCreatedEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid VehicleId,
    string TenantId,
    string PlateNumber,
    string Make,
    string Model,
    int Year,
    string? CorrelationId = null
) : DomainEvent(EventId, OccurredOnUtc, CorrelationId, TenantId)
{
    public static VehicleCreatedEvent Create(Guid vehicleId, string tenantId, string plateNumber, string make, string model, int year, string? correlationId = null)
        => new(Guid.NewGuid(), DateTimeOffset.UtcNow, vehicleId, tenantId, plateNumber, make, model, year, correlationId);
}

