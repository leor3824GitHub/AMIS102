namespace AMIS.Modules.Notifications.Contracts.v1.DTOs;

/// <summary>
/// A single inbox row as returned to clients. Denormalized — <see cref="Title"/>/<see cref="Body"/>/
/// <see cref="Link"/> are copied in at write time so rendering the bell never calls back into the
/// source module. <see cref="Type"/> is the <c>NotificationType</c> enum name.
/// </summary>
public sealed record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Body,
    string? Link,
    string Source,
    string? MetadataJson,
    bool IsRead,
    DateTimeOffset CreatedOnUtc);
