using FSH.Framework.Eventing.Abstractions;

namespace FSH.Modules.AssetRegister.Contracts.v1.IntegrationEvents;

/// <summary>
/// Integration events published by the AssetRegister module for consumption by
/// other modules (Finance, Auditing, MasterData). Phase 1 declares the types;
/// publication wiring lands in Phase 3.
/// </summary>
public static class AssetRegisterIntegrationEvents
{
    public sealed record AssetRegistered(
        Guid AssetRegistryId,
        string PropertyNo,
        AssetType AssetType,
        decimal UnitCost,
        DateOnly AcquisitionDate,
        string? TenantId,
        string CorrelationId = "") : IIntegrationEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
        public string Source { get; } = "AssetRegister";
    }

    public sealed record AssetIssued(
        Guid AssetRegistryId,
        Guid AccountabilityId,
        Guid CustodianId,
        string? TenantId,
        string CorrelationId = "") : IIntegrationEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
        public string Source { get; } = "AssetRegister";
    }

    public sealed record AssetDisposed(
        Guid AssetRegistryId,
        string PropertyNo,
        string? TenantId,
        string CorrelationId = "") : IIntegrationEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
        public string Source { get; } = "AssetRegister";
    }

    public sealed record IssuanceReportPosted(
        Guid ReportId,
        string ReportNo,
        IssuanceReportType ReportType,
        DateOnly PeriodFromDate,
        DateOnly PeriodToDate,
        string? TenantId,
        string CorrelationId = "") : IIntegrationEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
        public string Source { get; } = "AssetRegister";
    }

    public sealed record IncidentReportFiled(
        Guid IncidentReportId,
        string IncidentNo,
        PropertyIncidentType IncidentType,
        string? TenantId,
        string CorrelationId = "") : IIntegrationEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
        public string Source { get; } = "AssetRegister";
    }

    public sealed record UnserviceableReportClosed(
        Guid ReportId,
        string ReportNo,
        UnserviceableReportType ReportType,
        string? TenantId,
        string CorrelationId = "") : IIntegrationEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
        public string Source { get; } = "AssetRegister";
    }
}
