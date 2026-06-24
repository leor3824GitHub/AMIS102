namespace AMIS.Framework.Web.Realtime;

/// <summary>
/// Well-known SignalR client-method names pushed over <see cref="AppHub"/>. Centralised so the server
/// (hub + feature modules broadcasting via <c>IHubContext&lt;AppHub&gt;</c>) and clients agree on the exact
/// strings — a typo here is a silently dropped message, not a compile error.
/// </summary>
public static class RealtimeEvents
{
    /// <summary>A user came online or went offline. Sent to the <c>tenant:{id}</c> group.</summary>
    public const string PresenceChanged = "PresenceChanged";

    /// <summary>A user started typing in a channel. Sent to others in the <c>channel:{id}</c> group.</summary>
    public const string ChatTypingStarted = "ChatTypingStarted";

    /// <summary>A new chat message was created. Broadcast by the Chat module to the <c>channel:{id}</c> group.</summary>
    public const string ChatMessageCreated = "ChatMessageCreated";

    /// <summary>The recipient was @mentioned. Best-effort push by the Chat module to the <c>user:{id}</c> group.</summary>
    public const string ChatMentioned = "ChatMentioned";
}
