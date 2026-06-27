using AMIS.Framework.Core.Domain;
using AMIS.Modules.Notifications.Contracts.v1.Enums;

namespace AMIS.Modules.Notifications.Domain;

/// <summary>
/// A single per-user inbox row. Denormalized: Title/Body/Link/MetadataJson are copied in at write time so
/// rendering the bell never calls back into the source module. Tenant-scoped (Finbuckle
/// <c>.IsMultiTenant()</c>); not soft-deletable — dismiss is a hard delete.
/// </summary>
public sealed class Notification : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;

    /// <summary>Identity user id (Guid string) of the recipient — the <c>user:{id}</c> SignalR group target.</summary>
    public string RecipientUserId { get; private set; } = default!;

    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = default!;
    public string Body { get; private set; } = default!;

    /// <summary>Relative SPA route the row deep-links to, e.g. <c>/procurement/job-orders/{id}</c>.</summary>
    public string? Link { get; private set; }

    public string? MetadataJson { get; private set; }

    /// <summary>Producing module name (e.g. "Chat", "ProcurementAcquisition").</summary>
    public string Source { get; private set; } = default!;

    /// <summary>Producer-supplied de-duplication key; unique with (RecipientUserId, Type) to make redelivery idempotent.</summary>
    public string CorrelationId { get; private set; } = default!;

    public bool IsRead { get; private set; }
    public DateTimeOffset? ReadOnUtc { get; private set; }

    // IAuditableEntity — settable so the persistence audit interceptor can populate them.
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private Notification() { }

    public static Notification Create(
        string tenantId,
        string recipientUserId,
        NotificationType type,
        string title,
        string body,
        string? link,
        string source,
        string? metadataJson,
        string correlationId)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RecipientUserId = recipientUserId,
            Type = type,
            Title = title,
            Body = body,
            Link = link,
            Source = source,
            MetadataJson = metadataJson,
            CorrelationId = correlationId,
            IsRead = false,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>Marks the row read. Idempotent — a second call is a no-op.</summary>
    public void MarkRead()
    {
        if (IsRead)
        {
            return;
        }

        IsRead = true;
        ReadOnUtc = DateTimeOffset.UtcNow;
        LastModifiedOnUtc = ReadOnUtc;
    }
}
