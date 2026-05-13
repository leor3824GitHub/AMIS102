using AMIS.Modules.Auditing.Contracts;
using AMIS.Modules.Auditing.Contracts.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AMIS.Modules.Auditing.Contracts.Dtos;

public sealed class AuditDetailDto
{
    public Guid Id { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public DateTime ReceivedAtUtc { get; set; }

    [JsonConverter(typeof(JsonNumericEnumConverter<AuditEventType>))]
    public AuditEventType EventType { get; set; }

    [JsonConverter(typeof(JsonNumericEnumConverter<AuditSeverity>))]
    public AuditSeverity Severity { get; set; }

    public string? TenantId { get; set; }

    public string? UserId { get; set; }

    public string? UserName { get; set; }

    public string? TraceId { get; set; }

    public string? SpanId { get; set; }

    public string? CorrelationId { get; set; }

    public string? RequestId { get; set; }

    public string? Source { get; set; }

    [JsonConverter(typeof(JsonNumericEnumConverter<AuditTag>))]
    public AuditTag Tags { get; set; }

    /// <summary>
    /// Masked, deserialized payload. Serialized back to JSON for clients.
    /// </summary>
    public JsonElement Payload { get; set; }
}


