namespace AMIS.Maui.Services;

// Local mirrors of the Chat module JSON shapes. MAUI must not reference Modules.* — extra JSON
// properties (members, mentions, reactions) are ignored on deserialization for v1 mobile chat.

public sealed record ChatChannelDto(
    Guid Id,
    string? Name,
    string? Topic,
    string Type,
    string Scope,
    Guid? LastMessageId,
    DateTimeOffset? LastMessageAtUtc,
    DateTimeOffset CreatedOnUtc)
{
    public string DisplayName =>
        !string.IsNullOrWhiteSpace(Name) ? Name!
        : string.Equals(Type, "Direct", StringComparison.OrdinalIgnoreCase) ? "Direct message"
        : string.Equals(Type, "GroupDm", StringComparison.OrdinalIgnoreCase) ? "Group"
        : "Channel";

    public bool IsGlobal => string.Equals(Scope, "Global", StringComparison.OrdinalIgnoreCase);

    public string ScopeLabel => IsGlobal ? "NFA-wide" : "Office";
}

public sealed record ChatMessageDto(
    Guid Id,
    Guid ChannelId,
    string SenderId,
    string? SenderName,
    string? Content,
    Guid? ParentMessageId,
    int ReplyCount,
    bool IsPinned,
    bool IsDeleted,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? EditedOnUtc);

public sealed record ChatMessagePageDto(
    List<ChatMessageDto> Items,
    Guid? NextCursor,
    bool HasMore);
