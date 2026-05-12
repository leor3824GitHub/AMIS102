using FSH.Framework.Core.Domain;
using FSH.Modules.AssetRegister.Contracts.v1;

namespace FSH.Modules.AssetRegister.Domain.Events;

/// <summary>
/// In-process domain events used to coordinate state within the AssetRegister
/// module. Dispatched on SaveChanges. Distinct from
/// <c>AssetRegisterIntegrationEvents</c> which are published on the message bus.
/// </summary>
public abstract record AssetRegisterDomainEvent(string? TenantId, string? CorrelationId = null) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record AssetRegisteredEvent(
    Guid AssetRegistryId,
    string PropertyNo,
    AssetType AssetType,
    string? TenantId,
    string? CorrelationId = null) : AssetRegisterDomainEvent(TenantId, CorrelationId);

public sealed record AssetIssuedEvent(
    Guid AssetRegistryId,
    Guid AccountabilityId,
    Guid CustodianId,
    string? TenantId,
    string? CorrelationId = null) : AssetRegisterDomainEvent(TenantId, CorrelationId);

public sealed record AssetReturnedEvent(
    Guid AssetRegistryId,
    Guid AccountabilityId,
    string? TenantId,
    string? CorrelationId = null) : AssetRegisterDomainEvent(TenantId, CorrelationId);

public sealed record AssetTransferredEvent(
    Guid AssetRegistryId,
    Guid FromAccountabilityId,
    Guid ToAccountabilityId,
    string? TenantId,
    string? CorrelationId = null) : AssetRegisterDomainEvent(TenantId, CorrelationId);

public sealed record AssetFoundAtStationEvent(
    Guid PhysicalCountSessionId,
    Guid PhysicalCountEntryId,
    string? TenantId,
    string? CorrelationId = null) : AssetRegisterDomainEvent(TenantId, CorrelationId);

public sealed record AssetReportedMissingFromCountEvent(
    Guid AssetRegistryId,
    Guid PhysicalCountSessionId,
    string? TenantId,
    string? CorrelationId = null) : AssetRegisterDomainEvent(TenantId, CorrelationId);

public sealed record AssetLostEvent(
    Guid AssetRegistryId,
    Guid PropertyIncidentReportId,
    string? TenantId,
    string? CorrelationId = null) : AssetRegisterDomainEvent(TenantId, CorrelationId);

public sealed record AssetRecoveredEvent(
    Guid AssetRegistryId,
    Guid PropertyIncidentReportId,
    string? TenantId,
    string? CorrelationId = null) : AssetRegisterDomainEvent(TenantId, CorrelationId);

public sealed record AssetUnserviceableEvent(
    Guid AssetRegistryId,
    Guid UnserviceableReportId,
    string? TenantId,
    string? CorrelationId = null) : AssetRegisterDomainEvent(TenantId, CorrelationId);

public sealed record AssetDisposedEvent(
    Guid AssetRegistryId,
    string PropertyNo,
    DisposalMethod Method,
    string? TenantId,
    string? CorrelationId = null) : AssetRegisterDomainEvent(TenantId, CorrelationId);

public sealed record AccountabilityCancelledEvent(
    Guid AccountabilityId,
    string Reason,
    string? TenantId,
    string? CorrelationId = null) : AssetRegisterDomainEvent(TenantId, CorrelationId);

public sealed record IssuanceReportPostedEvent(
    Guid ReportId,
    string ReportNo,
    IssuanceReportType ReportType,
    string? TenantId,
    string? CorrelationId = null) : AssetRegisterDomainEvent(TenantId, CorrelationId);

public sealed record PhysicalCountSessionClosedEvent(
    Guid SessionId,
    string? TenantId,
    string? CorrelationId = null) : AssetRegisterDomainEvent(TenantId, CorrelationId);

public sealed record IncidentReportFiledEvent(
    Guid IncidentReportId,
    string IncidentNo,
    PropertyIncidentType IncidentType,
    string? TenantId,
    string? CorrelationId = null) : AssetRegisterDomainEvent(TenantId, CorrelationId);

public sealed record UnserviceableReportSubmittedEvent(
    Guid ReportId,
    string ReportNo,
    UnserviceableReportType ReportType,
    string? TenantId,
    string? CorrelationId = null) : AssetRegisterDomainEvent(TenantId, CorrelationId);
