using AMIS.Modules.Notifications.Contracts.v1.DTOs;
using AMIS.Modules.Notifications.Domain;

namespace AMIS.Modules.Notifications.Features.v1;

internal static class NotificationMappers
{
    public static NotificationDto ToDto(this Notification n) => new(
        n.Id,
        n.Type.ToString(),
        n.Title,
        n.Body,
        n.Link,
        n.Source,
        n.MetadataJson,
        n.IsRead,
        n.CreatedOnUtc);
}
