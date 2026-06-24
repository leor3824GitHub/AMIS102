using AMIS.Framework.Shared.Constants;

namespace AMIS.Modules.Chat;

public static class ChatModuleConstants
{
    public const string SchemaName = "chat";

    /// <summary>
    /// Fixed, well-known id of the single seeded NFA-wide global channel. Using a constant id makes
    /// <c>ChatDbInitializer.SeedAsync</c> idempotent and lets the membership adapters special-case the
    /// global channel without a per-user membership row.
    /// </summary>
    public static readonly Guid GlobalChannelId = new("0c8a7e10-3b9d-4f2a-9c1e-000000000001");

    public const string GlobalChannelName = "NFA Global";

    /// <summary>
    /// Permissions registered with the Identity permission catalog. Intentionally NOT marked
    /// <c>IsBasic</c> so Chat ships dark — assign these to a pilot role until the UI is ready.
    /// </summary>
    public static readonly IReadOnlyList<AmisPermission> Permissions =
    [
        new("View Chat Channels", "View", "Chat.Channels"),
        new("Create Chat Channels", "Create", "Chat.Channels"),
        new("Manage All Chat Channels", "ManageAll", "Chat.Channels"),

        new("Send Chat Messages", "Send", "Chat.Messages"),
        new("Edit Own Chat Messages", "EditOwn", "Chat.Messages"),
        new("Delete Own Chat Messages", "DeleteOwn", "Chat.Messages"),
        new("Delete Any Chat Messages", "DeleteAny", "Chat.Messages"),
    ];
}
