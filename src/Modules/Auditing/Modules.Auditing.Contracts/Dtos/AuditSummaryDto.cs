using System.Text.Json.Serialization;
using AMIS.Modules.Auditing.Contracts;
using AMIS.Modules.Auditing.Contracts.Serialization;

namespace AMIS.Modules.Auditing.Contracts.Dtos;

public sealed class AuditSummaryDto
{
    public Guid Id { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    [JsonConverter(typeof(JsonNumericEnumConverter<AuditEventType>))]
    public AuditEventType EventType { get; set; }

    [JsonConverter(typeof(JsonNumericEnumConverter<AuditSeverity>))]
    public AuditSeverity Severity { get; set; }

    public string? TenantId { get; set; }

    public string? UserId { get; set; }

    public string? UserName { get; set; }

    public string? TraceId { get; set; }

    public string? CorrelationId { get; set; }

    public string? RequestId { get; set; }

    public string? Source { get; set; }

    [JsonConverter(typeof(JsonNumericEnumConverter<AuditTag>))]
    public AuditTag Tags { get; set; }
}


