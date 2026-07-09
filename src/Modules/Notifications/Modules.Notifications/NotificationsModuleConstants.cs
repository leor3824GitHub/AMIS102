using AMIS.Framework.Shared.Constants;

namespace AMIS.Modules.Notifications;

public static class NotificationsModuleConstants
{
    public const string SchemaName = "notifications";

    /// <summary>
    /// Realtime client-method name pushed over <c>AppHub</c> when a new inbox row is created. Kept local
    /// (not in <c>BuildingBlocks/Web/Realtime/RealtimeEvents</c>) so this module is self-contained; the
    /// Blazor hub client holds a matching const — a typo here is a silently dropped push, not a build error.
    /// </summary>
    public const string NotificationCreatedEvent = "NotificationCreated";

    /// <summary>
    /// Realtime client-method name pushed over <c>AppHub</c> when a workflow marks an inbox row read
    /// server-side (e.g. an ICS/PAR acceptance resolving its "issued for your acceptance" entry).
    /// Payload is the notification id. Same typo caveat as <see cref="NotificationCreatedEvent"/>.
    /// </summary>
    public const string NotificationReadEvent = "NotificationRead";

    /// <summary>
    /// Permission registered with the Identity catalog. Marked <c>IsBasic</c> so the Basic role (assigned
    /// to every user) is seeded with it — the inbox is self-service and scoped to the calling user.
    /// </summary>
    public static readonly IReadOnlyList<AmisPermission> Permissions =
    [
        new("View Notifications", "View", "Notifications", IsBasic: true),
    ];
}
