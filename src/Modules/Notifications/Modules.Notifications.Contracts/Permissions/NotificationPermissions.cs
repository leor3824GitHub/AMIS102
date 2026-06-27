namespace AMIS.Modules.Notifications.Contracts.Permissions;

/// <summary>
/// Permission name constants for the Notifications module. Names mirror <c>AmisPermission.Name</c>
/// (<c>Permissions.{Resource}.{Action}</c>) registered in <c>NotificationsModuleConstants</c>.
/// The inbox is self-service — every endpoint is scoped to the calling user — so a single
/// <c>View</c> permission (registered <c>IsBasic</c>) gates the whole module.
/// </summary>
public static class NotificationPermissions
{
    public static class Notifications
    {
        public const string View = "Permissions.Notifications.View";
    }
}
