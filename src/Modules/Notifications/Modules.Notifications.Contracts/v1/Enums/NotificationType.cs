namespace AMIS.Modules.Notifications.Contracts.v1.Enums;

/// <summary>
/// Discriminator for an inbox row, used by clients for grouping/iconography. New producers add a value
/// here; the Notifications module itself stays agnostic to the meaning beyond rendering.
/// </summary>
public enum NotificationType
{
    General = 0,
    ChatMention = 1,
    InspectionRequested = 2,
}
