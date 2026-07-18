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

    /// <summary>An ICS/PAR was issued to the accountable person and is awaiting their acceptance.</summary>
    AccountabilityIssued = 3,

    /// <summary>Weekly digest: one or more of the recipient's Active ICS/PAR are due for renewal soon (or overdue).</summary>
    AccountabilityRenewalDue = 4,

    /// <summary>Another agency has offered property to this one; it is waiting in Incoming Transfers.</summary>
    TransferOfferReceived = 5,

    /// <summary>The receiving agency accepted or rejected a transfer this agency offered.</summary>
    TransferOfferAnswered = 6,
}
